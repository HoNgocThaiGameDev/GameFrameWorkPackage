using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.TextCore;
using UnityEngine.U2D;

#if MODULE_TMP
using TMPro;
#endif

namespace NebulaSoft
{
    [CustomEditor(typeof(CurrencyDatabase))]
    public class CurrencyDatabaseEditor : CustomInspector
    {
        private CurrencyDatabase currencyDatabase;

        protected override void OnEnable()
        {
            base.OnEnable();

            currencyDatabase = (CurrencyDatabase)target;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

#if MODULE_TMP
            if (GUILayout.Button("Create Sprite Atlas"))
            {
                CreateAtlas();
            }
#endif
        }

#if MODULE_TMP
        private void CreateAtlas()
        {
            if (currencyDatabase == null) return;

            List<TMPAtlasGenerator.SpriteData> atlasElements = new List<TMPAtlasGenerator.SpriteData>();

            Currency[] currencies = currencyDatabase.Currencies;

            //Set Full Rect Type manualy
            for (int i = 0; i < currencies.Length; i++)
            {
                if (currencies[i].Icon != null)
                {
                    TextureImporter textureImporter = (TextureImporter)TextureImporter.GetAtPath(AssetDatabase.GetAssetPath(currencies[i].Icon));

                    TextureImporterSettings settings = new TextureImporterSettings();
                    textureImporter.ReadTextureSettings(settings);
                    settings.spriteMeshType = SpriteMeshType.FullRect;
                    textureImporter.SetTextureSettings(settings);
                    textureImporter.SaveAndReimport();
                }
            }

            AssetDatabase.Refresh();

            for (int i = 0; i < currencies.Length; i++)
            {
                if (currencies[i].Icon != null)
                {
                    atlasElements.Add(new TMPAtlasGenerator.SpriteData(currencies[i].Icon, currencies[i].ID));
                }
            }

            TMPAtlasGenerator.Create(atlasElements, "");
        }

        public class TMPAtlasGenerator
        {
            private const string FILE_PATH_SAVE = "atlas_generator_file_path";
            private List<SpriteData> elements;

            private string filePath;

            public TMPAtlasGenerator(string path)
            {
                this.filePath = path;

                elements = new List<SpriteData>();
            }

            public void Add(SpriteData element)
            {
                elements.Add(element);
            }

            public void Save()
            {
                SpriteAtlasAsset spriteAtlasAsset = new SpriteAtlasAsset();
                spriteAtlasAsset.Add(elements.Select(x => (Object)x.Sprite).ToArray());

                SpriteAtlasAsset.Save(spriteAtlasAsset, filePath);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                SpriteAtlasImporter spriteAtlasImporter = (SpriteAtlasImporter)SpriteAtlasImporter.GetAtPath(filePath);
                spriteAtlasImporter.packingSettings = new SpriteAtlasPackingSettings()
                {
                    enableTightPacking = false,
                    enableAlphaDilation = true
                };

                spriteAtlasImporter.SaveAndReimport();

                AssetDatabase.Refresh();
            }

            public IEnumerator<SpriteAtlas> GetSpriteAtlas()
            {
                SpriteAtlas spriteAtlas = null;

                do
                {
                    yield return null;

                    spriteAtlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(filePath);
                }
                while (spriteAtlas == null);

                yield return spriteAtlas;
            }

            public bool IsEmpty()
            {
                return elements.IsNullOrEmpty();
            }

            public void CreateSpriteAsset(SpriteAtlas spriteAtlas)
            {
                // Get the path to the selected asset.
                string fileNameWithExtension = Path.GetFileName(this.filePath);
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(this.filePath);
                string filePath = this.filePath.Replace(fileNameWithExtension, "");

                // Create new Sprite Asset
                TMP_SpriteAsset spriteAsset = ScriptableObject.CreateInstance<TMP_SpriteAsset>();
                AssetDatabase.CreateAsset(spriteAsset, filePath + fileNameWithoutExtension + ".asset");

                ReflectionUtils.InjectInstanceComponent(spriteAsset, "m_Version", "1.1.0");

                // Compute the hash code for the sprite asset.
                spriteAsset.hashCode = TMP_TextUtilities.GetSimpleHashCode(spriteAsset.name);

                List<TMP_SpriteGlyph> spriteGlyphTable = new List<TMP_SpriteGlyph>();
                List<TMP_SpriteCharacter> spriteCharacterTable = new List<TMP_SpriteCharacter>();

                // Get number of sprites contained in the sprite atlas.
                int spriteCount = spriteAtlas.spriteCount;
                Sprite[] sprites = new Sprite[spriteCount];

                // Get all the sprites
                spriteAtlas.GetSprites(sprites);

                for (int i = 0; i < sprites.Length; i++)
                {
                    Sprite sprite = sprites[i];
                    string realName = sprite.name.Substring(0, sprite.name.Length - 7);
                    SpriteData linkedElement = GetAtlasElement(realName);

                    TMP_SpriteGlyph spriteGlyph = new TMP_SpriteGlyph();
                    spriteGlyph.index = (uint)i;

                    if ((linkedElement != null) && (linkedElement.OverrideDataSet))
                    {
                        spriteGlyph.metrics = linkedElement.GlyphMetrics;
                        spriteGlyph.scale = linkedElement.Scale;
                    }
                    else
                    {
                        spriteGlyph.metrics = new GlyphMetrics(sprite.rect.width, sprite.rect.height, -sprite.pivot.x, sprite.rect.height - sprite.pivot.y, sprite.rect.width);
                        spriteGlyph.scale = 1.0f;
                    }

                    spriteGlyph.glyphRect = new GlyphRect(sprite.textureRect);
                    spriteGlyph.sprite = sprite;
                    spriteGlyphTable.Add(spriteGlyph);

                    TMP_SpriteCharacter spriteCharacter = new TMP_SpriteCharacter(0xFFFE, spriteGlyph);

                    if (linkedElement != null)
                    {
                        spriteCharacter.name = linkedElement.Name;
                    }
                    else
                    {
                        spriteCharacter.name = realName;
                    }

                    spriteCharacter.scale = 1.0f;

                    spriteCharacterTable.Add(spriteCharacter);
                }

                ReflectionUtils.InjectInstanceComponent(spriteAsset, "m_SpriteCharacterTable", spriteCharacterTable);
                ReflectionUtils.InjectInstanceComponent(spriteAsset, "m_GlyphTable", spriteGlyphTable);

                spriteAsset.spriteSheet = spriteGlyphTable[0].sprite.texture;

                // Add new default material for sprite asset.
                AddDefaultMaterial(spriteAsset);

                // Update Lookup tables.
                spriteAsset.UpdateLookupTables();

                // Get the Sprites contained in the Sprite Sheet
                EditorUtility.SetDirty(spriteAsset);

                AssetDatabase.SaveAssets();

                AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(spriteAsset));
            }

            private void AddDefaultMaterial(TMP_SpriteAsset spriteAsset)
            {
                UnityEngine.Shader shader = UnityEngine.Shader.Find("TextMeshPro/Sprite");
                Material material = new Material(shader);
                material.SetTexture(ShaderUtilities.ID_MainTex, spriteAsset.spriteSheet);

                spriteAsset.material = material;
                material.hideFlags = HideFlags.HideInHierarchy;
                AssetDatabase.AddObjectToAsset(material, spriteAsset);
            }

            private SpriteData GetAtlasElement(string spriteName)
            {
                foreach (SpriteData element in elements)
                {
                    if (element.Sprite.name == spriteName)
                        return element;
                }

                return null;
            }

            public static TMPAtlasGenerator Create(List<SpriteData> atlasElements, string path)
            {
                TMPAtlasGenerator atlasGenerator = new TMPAtlasGenerator(path);

                EditorCoroutines.Execute(atlasGenerator.AtlasCoroutine(atlasElements));

                return atlasGenerator;
            }

            public IEnumerator AtlasCoroutine(List<SpriteData> atlasElements)
            {
                if (string.IsNullOrEmpty(filePath))
                {
                    string savedPath = EditorPrefs.GetString(FILE_PATH_SAVE);
                    if (!string.IsNullOrEmpty(savedPath))
                    {
                        savedPath = Path.GetDirectoryName(savedPath);
                    }
                    else
                    {
                        savedPath = "Assets";
                    }

                    filePath = EditorUtility.SaveFilePanelInProject("Generated Atlas", "GeneratedAtlas", "spriteatlasv2", "Select atlas path", savedPath);

                    if (string.IsNullOrEmpty(filePath))
                    {
                        Debug.LogError("[Atlas Generator]: Path can't be empty!");

                        yield break;
                    }

                    EditorPrefs.SetString(FILE_PATH_SAVE, Path.GetDirectoryName(filePath));
                }

                if (atlasElements.IsNullOrEmpty())
                {
                    Debug.LogError("[Atlas Generator]: Sprites list is empty!");

                    yield break;
                }

                for (int i = 0; i < atlasElements.Count; i++)
                {
                    elements.Add(atlasElements[i]);
                }

                //Handle possible override
                if (!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(filePath, AssetPathToGUIDOptions.OnlyExistingAssets)))
                {
                    string TMP_AssetFilePath = filePath.Replace(".spriteatlasv2", ".asset");
                    TMP_SpriteAsset TMP_AssetFile = AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>(TMP_AssetFilePath);

                    if ((TMP_AssetFile != null) && (EditorUtility.DisplayDialog("Asset override", "Do you want to copy sprite GlyphMetrics and scale from overriden asset?", "Yes", "No")))
                    {
                        for (int i = 0; i < TMP_AssetFile.spriteCharacterTable.Count; i++)
                        {
                            for (int j = 0; j < elements.Count; j++)
                            {
                                if (!elements[j].OverrideDataSet)
                                {
                                    if (elements[j].Name.Equals(TMP_AssetFile.spriteCharacterTable[i].name))
                                    {
                                        elements[j].GlyphMetrics = TMP_AssetFile.spriteCharacterTable[i].glyph.metrics;
                                        elements[j].Scale = TMP_AssetFile.spriteCharacterTable[i].glyph.scale;
                                        elements[j].OverrideDataSet = true;
                                    }
                                }
                            }
                        }
                    }
                }

                Save();

                IEnumerator<SpriteAtlas> spriteAtlasEnumerator = GetSpriteAtlas();
                while (spriteAtlasEnumerator.MoveNext())
                {
                    yield return null;
                }

                SpriteAtlas spriteAtlas = spriteAtlasEnumerator.Current;
                if (spriteAtlas == null)
                {
                    Debug.LogError("[Currencies]: Failed to create Sprite Atlas!");

                    yield break;
                }

                EditorUtility.SetDirty(spriteAtlas);

                yield return null;

                CreateSpriteAsset(spriteAtlas);

                //Applying Ref to TMP settings
                TMP_Settings settings = EditorUtils.GetAsset<TMP_Settings>();

                if (settings != null)
                {
                    if (EditorUtility.DisplayDialog("Linking asset to TMP Settings", "Do you want to add created \"Sprite Atlas\" to \"TMP Settings\"?", "Yes", "Cancel"))
                    {
                        SerializedObject settingsSerializedObject = new SerializedObject(settings);
                        SerializedProperty defaultAssetProperty = settingsSerializedObject.FindProperty("m_defaultSpriteAsset");
                        string TMP_AssetFilePath = filePath.Replace(".spriteatlasv2", ".asset");
                        TMP_SpriteAsset TMP_AssetFile = AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>(TMP_AssetFilePath);

                        if ((defaultAssetProperty.objectReferenceValue == null) || EditorUtility.DisplayDialog("Linking asset to TMP Settings", "Do you want to override \"Default Sprite Asset\" reference with created \"Sprite Atlas\" or add reference to fallback list of current \"Default Sprite Asset\" ?", "Override", "Add to fallback list"))
                        {
                            defaultAssetProperty.objectReferenceValue = TMP_AssetFile;
                            settingsSerializedObject.ApplyModifiedProperties();
                        }
                        else
                        {
                            SerializedObject spriteAssetSerializedObject = new SerializedObject(defaultAssetProperty.objectReferenceValue);
                            SerializedProperty fallbackAssetsProperty = spriteAssetSerializedObject.FindProperty("fallbackSpriteAssets");
                            fallbackAssetsProperty.arraySize++;
                            fallbackAssetsProperty.GetArrayElementAtIndex(fallbackAssetsProperty.arraySize - 1).objectReferenceValue = TMP_AssetFile;
                            spriteAssetSerializedObject.ApplyModifiedProperties();
                        }
                    }
                }
            }

            public class SpriteData
            {
                private Sprite sprite;
                private string name;
                private bool overrideDataSet;
                private GlyphMetrics glyphMetrics;
                private float scale;

                public Sprite Sprite => sprite;
                public string Name => name;

                public bool OverrideDataSet { get => overrideDataSet; set => overrideDataSet = value; }
                public GlyphMetrics GlyphMetrics { get => glyphMetrics; set => glyphMetrics = value; }
                public float Scale { get => scale; set => scale = value; }

                public SpriteData(Sprite sprite, string name)
                {
                    this.sprite = sprite;
                    this.name = name;
                    overrideDataSet = false;
                }
            }
        }
#endif
    }
}
