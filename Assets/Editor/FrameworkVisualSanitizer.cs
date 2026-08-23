using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NebulaSoft.EditorTools
{
    /// <summary>
    /// Replaces artwork with a shared swatch while retaining the existing
    /// hierarchy and layout. Each sprite slot receives a stable palette color so
    /// the team can identify and replace individual art regions quickly.
    /// </summary>
    public static class FrameworkVisualSanitizer
    {
        private const string AppliedPreference = "PicturePuzzle.Framework.ColorPlaceholdersApplied.v1";
        private const string WhiteSpritePath = "Assets/Framework/WhiteSprite.svg";
        private static readonly Color BackgroundGray = new Color(0.45f, 0.45f, 0.45f, 1f);
        private static readonly Color[] PlaceholderPalette =
        {
            new Color32(255, 183,  77, 255), // orange
            new Color32( 79, 195, 247, 255), // blue
            new Color32(129, 199, 132, 255), // green
            new Color32(244, 143, 177, 255), // pink
            new Color32(186, 104, 200, 255), // purple
            new Color32(255, 241, 118, 255), // yellow
            new Color32(128, 203, 196, 255), // teal
            new Color32(255, 138, 101, 255), // coral
            new Color32(144, 164, 174, 255), // slate
            new Color32(174, 213, 129, 255)  // lime
        };

        [InitializeOnLoadMethod]
        private static void ApplyColorPlaceholdersAfterReload()
        {
            if (EditorPrefs.GetBool(AppliedPreference, false))
                return;

            // Delay until the asset database and serialized prefab contents are ready.
            EditorApplication.delayCall += () =>
            {
                if (EditorPrefs.GetBool(AppliedPreference, false))
                    return;

                EditorPrefs.SetBool(AppliedPreference, true);
                ApplyToProject();
            };
        }

        [MenuItem("Framework/Replace Skeleton")]
        public static void ApplyToProject()
        {
            Sprite whiteSprite = EnsureWhiteSprite();
            if (whiteSprite == null)
            {
                Debug.LogError("[FrameworkVisualSanitizer] Could not create the shared white Sprite.");
                return;
            }

            int prefabCount = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                    continue;

                GameObject root = PrefabUtility.LoadPrefabContents(path);
                bool changed = SanitizeHierarchy(root, whiteSprite);
                if (changed)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    prefabCount++;
                }

                PrefabUtility.UnloadPrefabContents(root);
            }

            int sceneCount = 0;
            string activeScenePath = SceneManager.GetActiveScene().path;
            foreach (string guid in AssetDatabase.FindAssets("t:Scene"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                bool changed = false;
                foreach (GameObject root in scene.GetRootGameObjects())
                    changed |= SanitizeHierarchy(root, whiteSprite);

                if (changed)
                {
                    EditorSceneManager.SaveScene(scene);
                    sceneCount++;
                }
            }

            if (!string.IsNullOrEmpty(activeScenePath) && FileExists(activeScenePath))
                EditorSceneManager.OpenScene(activeScenePath, OpenSceneMode.Single);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[FrameworkVisualSanitizer] Replaced artwork in " + prefabCount + " prefabs and " + sceneCount + " scenes.");
        }

        private static Sprite EnsureWhiteSprite()
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(WhiteSpritePath);
            if (sprite != null)
                return sprite;

            const string pngPath = "Assets/Framework/WhiteSprite.png";
            string absolutePath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), pngPath);
            Texture2D texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[32 * 32];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.white;

            texture.SetPixels(pixels);
            texture.Apply();
            System.IO.File.WriteAllBytes(absolutePath, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(pngPath, ImportAssetOptions.ForceSynchronousImport);

            TextureImporter importer = AssetImporter.GetAtPath(pngPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(pngPath);
        }

        private static bool SanitizeHierarchy(GameObject root, Sprite whiteSprite)
        {
            bool changed = false;
            foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
            {
                bool isBackground = IsBackground(transform);
                SpriteRenderer spriteRenderer = transform.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null)
                {
                    if (spriteRenderer.sprite != whiteSprite)
                    {
                        spriteRenderer.sprite = whiteSprite;
                        changed = true;
                    }

                    changed |= SetColor(spriteRenderer, GetPlaceholderColor(transform, isBackground));
                }

                Image image = transform.GetComponent<Image>();
                if (image != null)
                {
                    if (image.sprite != whiteSprite)
                    {
                        image.sprite = whiteSprite;
                        changed = true;
                    }

                    changed |= SetColor(image, GetPlaceholderColor(transform, isBackground));
                }

                RawImage rawImage = transform.GetComponent<RawImage>();
                if (rawImage != null)
                    changed |= SetColor(rawImage, GetPlaceholderColor(transform, isBackground));

                Camera camera = transform.GetComponent<Camera>();
                if (camera != null && camera.clearFlags != CameraClearFlags.SolidColor)
                {
                    camera.clearFlags = CameraClearFlags.SolidColor;
                    changed = true;
                }

                if (camera != null && camera.backgroundColor != BackgroundGray)
                {
                    camera.backgroundColor = BackgroundGray;
                    changed = true;
                }
            }

            return changed;
        }

        private static bool SetColor(Graphic graphic, Color target)
        {
            Color color = graphic.color;
            target.a = color.a;
            if (color == target)
                return false;

            graphic.color = target;
            return true;
        }

        private static bool SetColor(SpriteRenderer renderer, Color target)
        {
            Color color = renderer.color;
            target.a = color.a;
            if (color == target)
                return false;

            renderer.color = target;
            return true;
        }

        private static bool IsBackground(Transform transform)
        {
            for (Transform current = transform; current != null; current = current.parent)
            {
                string name = current.name.Replace("_", string.Empty).Replace("-", string.Empty).ToLowerInvariant();
                if (name.Contains("background") || name == "bg" || name.Contains("backdrop"))
                    return true;
            }

            return false;
        }

        private static Color GetPlaceholderColor(Transform transform, bool isBackground)
        {
            if (isBackground)
                return BackgroundGray;

            // A stable path-based index keeps the same slot color between runs
            // without storing any extra metadata in the scene or prefab.
            string path = transform.name;
            for (Transform current = transform.parent; current != null; current = current.parent)
                path = current.name + "/" + path;

            unchecked
            {
                int hash = 17;
                for (int i = 0; i < path.Length; i++)
                    hash = hash * 31 + path[i];

                int index = Mathf.Abs(hash % PlaceholderPalette.Length);
                return PlaceholderPalette[index];
            }
        }

        private static bool FileExists(string path)
        {
            return !string.IsNullOrEmpty(path) && System.IO.File.Exists(path);
        }
    }
}
