using System.IO;
using UnityEditor;
using UnityEngine;

namespace NebulaSoft
{
    [InitializeOnLoad]
    public static class CoreEditor
    {
        // Folders
        public static string FOLDER_CORE { get; private set; }

        public static string FOLDER_CORE_MODULES => Path.Combine(FOLDER_CORE, "Modules");

        public static string FOLDER_DATA;
        public static string FOLDER_SCENES;

        // Editor values
        public static bool UseCustomInspector { get; private set; } = true;
        public static bool UseHierarchyIcons { get; private set; } = true;

        public static bool AutoLoadInitializer { get; private set; } = true;
        public static string InitSceneName { get; private set; } = "Init";

        public static Color AdsDummyBackgroundColor { get; private set; } = new Color(0.2f, 0.2f, 0.3f);
        public static Color AdsDummyMainColor { get; private set; } = new Color(0.2f, 0.3f, 0.7f);

        public static bool ShowNebulaSoftPromotions { get; private set; } = false;

        static CoreEditor()
        {
            Init();
        }

        private static void Init()
        {
            CoreSettings coreSettings = EditorUtils.GetAsset<CoreSettings>();
            if (coreSettings == null)
            {
                if (EditorApplication.isUpdating || EditorApplication.isCompiling)
                {
                    EditorApplication.delayCall += Init;

                    return;
                }

                Debug.LogWarning("[NebulaSoft Core]: Core Settings asset cannot be found in the project. This asset is required for the proper functionality of the modules.");

                coreSettings = ScriptableObject.CreateInstance<CoreSettings>();

                FOLDER_CORE = Path.Combine("Assets", "NebulaSoft Core");

                if (!AssetDatabase.IsValidFolder(FOLDER_CORE))
                {
                    AssetDatabase.CreateFolder("Assets/", "NebulaSoft Core");
                }

                AssetDatabase.CreateAsset(coreSettings, Path.Combine("Assets", "NebulaSoft Core", "Core Settings.asset"));
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            else
            {
                FOLDER_CORE = AssetDatabase.GetAssetPath(coreSettings).Replace(coreSettings.name + ".asset", "");
            }

            ApplySettings(coreSettings);
        }

        public static void ApplySettings(CoreSettings settings)
        {
            // Folders
            FOLDER_DATA = settings.DataFolder;
            FOLDER_SCENES = settings.ScenesFolder;

            // Init
            InitSceneName = settings.InitSceneName;
            AutoLoadInitializer = settings.AutoLoadInitializer;

            // Editor
            UseCustomInspector = settings.UseCustomInspector;
            UseHierarchyIcons = settings.UseHierarchyIcons;

            // Ads
            AdsDummyBackgroundColor = settings.AdsDummyBackgroundColor;
            AdsDummyMainColor = settings.AdsDummyMainColor;

            // Other
            ShowNebulaSoftPromotions = settings.ShowNebulaSoftPromotions;
        }

        public static string FormatPath(string path)
        {
            return path.Replace("{CORE_MODULES}", FOLDER_CORE_MODULES)
                       .Replace("{CORE_DATA}", FOLDER_DATA)
                       .Replace("{CORE}", FOLDER_CORE);
        }

        public static string GetCoreVersion()
        {
            string coreVersion = null;
            TextAsset coreChangelogText = EditorUtils.GetAsset<TextAsset>("Core Changelog");
            if (coreChangelogText != null && !string.IsNullOrEmpty(coreChangelogText.text))
            {
                string[] lines = coreChangelogText.text.Split('\n');
                if (lines.Length > 0)
                {
                    coreVersion = lines[0];
                }
            }

            return coreVersion;
        }

        public static string GetTemplateVersion()
        {
            string projectVersion = null;
            TextAsset templateChangelogText = EditorUtils.GetAsset<TextAsset>("Template Changelog");
            if (templateChangelogText != null && !string.IsNullOrEmpty(templateChangelogText.text))
            {
                string[] lines = templateChangelogText.text.Split('\n');
                if (lines.Length > 0)
                {
                    projectVersion = lines[0];
                }
            }

            return projectVersion;
        }

        public static string GetDocumentationURL()
        {
            string documentationURL = null;

            TextAsset documentationText = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Project Files/DOCUMENTATION.txt");

            if (documentationText != null && !string.IsNullOrEmpty(documentationText.text))
            {
                string[] lines = documentationText.text.Split('\n');
                if (lines.Length > 0)
                {
                    documentationURL = lines[^1];
                }
            }

            return documentationURL;
        }
    }
}
