using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NebulaSoft
{
    public static class CorePreparationTool
    {
        private const string RECOMMENDED_BASE_FOLDER = "Assets/Project Files";
        private static readonly string[] RECOMMENDED_FOLDERS = new string[]
        {
            "/Data",
            "/Game",
            "/Game/Audio",
            "/Game/Animations",
            "/Game/Fonts",
            "/Game/Images",
            "/Game/Materials",
            "/Game/Models",
            "/Game/Prefabs",
            "/Game/Scenes",
            "/Game/Scripts",
            "/Game/Features",
            "/Game/Integration",
            "/Game/UI",
            "/Game/Shaders",
            "/Game/Textures"
        };

        [MenuItem("Window/NebulaSoft Core/Prepare Project", priority = 900)]
        private static void CreateStructure()
        {
            try
            {
                int choice = EditorUtility.DisplayDialogComplex("Core Preparetion", "Choose how you want to set up the project structure:", "Recommended", "Custom", "Cancel");

                if (choice == 0)
                {
                    RunRecommendedSetup();
                }
                else if (choice == 1)
                {
                    RunCustomSetup();
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"An error occurred while preparing the project structure: {ex.Message}");
            }
        }

        private static void RunRecommendedSetup()
        {
            CreateRecommendedFolders();

            ProjectInitSettings projectInitSettings = CreateInitSettings(Path.Combine(RECOMMENDED_BASE_FOLDER, "Data"));

            GameObject initializerPrefab = CreateInitializerPrefab(projectInitSettings, Path.Combine(RECOMMENDED_BASE_FOLDER, "Game", "Prefabs"));

            CreateInitScene(initializerPrefab, Path.Combine(RECOMMENDED_BASE_FOLDER, "Game", "Scenes"));

            CoreSettings coreSettings = EditorUtils.GetAsset<CoreSettings>();
            if(coreSettings != null)
            {
                SerializedObject settingsSerializedObject = new SerializedObject(coreSettings);
                settingsSerializedObject.Update();
                settingsSerializedObject.FindProperty("dataFolder").stringValue = Path.Combine("Assets", "Project Files", "Data");
                settingsSerializedObject.FindProperty("scenesFolder").stringValue = Path.Combine("Assets", "Project Files", "Game", "Scenes");
                settingsSerializedObject.ApplyModifiedProperties();

                CoreEditor.ApplySettings(coreSettings);

                AssetDatabase.Refresh();
            }
        }

        private static void RunCustomSetup()
        {
            // Prompt user for each custom folder path
            string initSettingsFolder = EditorUtils.ConvertToRelativePath(EditorUtility.OpenFolderPanel("Select Settings Folder", "Assets", ""));
            string prefabFolder = EditorUtils.ConvertToRelativePath(EditorUtility.OpenFolderPanel("Select Prefabs Folder", "Assets", ""));
            string scenesFolder = EditorUtils.ConvertToRelativePath(EditorUtility.OpenFolderPanel("Select Scenes Folder", "Assets", ""));

            if (string.IsNullOrEmpty(initSettingsFolder) || string.IsNullOrEmpty(prefabFolder) || string.IsNullOrEmpty(scenesFolder))
            {
                Debug.LogWarning("Setup canceled or one or more folders were not selected.");

                return;
            }

            ProjectInitSettings projectInitSettings = CreateInitSettings(initSettingsFolder);

            GameObject initializerPrefab = CreateInitializerPrefab(projectInitSettings, prefabFolder);

            CreateInitScene(initializerPrefab, scenesFolder); 
            
            CoreSettings coreSettings = EditorUtils.GetAsset<CoreSettings>();
            if (coreSettings != null)
            {
                SerializedObject settingsSerializedObject = new SerializedObject(coreSettings);
                settingsSerializedObject.Update();
                settingsSerializedObject.FindProperty("dataFolder").stringValue = initSettingsFolder;
                settingsSerializedObject.FindProperty("scenesFolder").stringValue = scenesFolder;
                settingsSerializedObject.ApplyModifiedProperties();

                CoreEditor.ApplySettings(coreSettings);

                AssetDatabase.Refresh();
            }
        }

        private static void CreateInitScene(GameObject initializerPrefab, string path)
        {
            // Ensure scene folder exists
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);

            // Create "Init" scene
            string initScenePath = Path.Combine(path, "Init.unity");
            if (!File.Exists(initScenePath))
            {
                Scene initScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                initScene.name = "Init";

                PrefabUtility.InstantiatePrefab(initializerPrefab);
                CreateLoadingGraphicsObject();

                EditorSceneManager.SaveScene(initScene, initScenePath);

                AddSceneToBuildSettings(initScenePath);
            }
            else
            {
                Debug.LogWarning("Init scene already exists at the specified path.");
            }

            // Create "Game" scene
            string gameScenePath = Path.Combine(path, "Game.unity");
            if (!File.Exists(gameScenePath))
            {
                Scene gameScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

                AudioListener audioListener = Camera.main.GetComponent<AudioListener>();
                GameObject.DestroyImmediate(audioListener);

                EditorSceneManager.SaveScene(gameScene, gameScenePath);

                AddSceneToBuildSettings(gameScenePath);
            }
            else
            {
                Debug.LogWarning("Game scene already exists at the specified path.");
            }
        }

        private static GameObject CreateInitializerPrefab(ProjectInitSettings projectInitSettings, string folderPath)
        {
            string path = Path.Combine(folderPath, "Initializer.prefab");

            // Check if prefab already exists
            if (File.Exists(path))
            {
                Debug.Log("Initializer prefab already exists. Skipping creation.");

                return AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }

            GameObject initializerObject = new GameObject("Initializer");

            GameObject eventSystemObject = new GameObject("Event System");
            eventSystemObject.transform.SetParent(initializerObject.transform);
            eventSystemObject.transform.ResetLocal();

            EventSystem eventSystem = eventSystemObject.AddComponent<EventSystem>();

            Initializer initializer = initializerObject.AddComponent<Initializer>();

            SerializedObject serializedObject = new SerializedObject(initializer);
            serializedObject.Update();
            serializedObject.FindProperty("initSettings").objectReferenceValue = projectInitSettings;
            serializedObject.FindProperty("eventSystem").objectReferenceValue = eventSystem;
            serializedObject.ApplyModifiedProperties();

            GameObject prefabObject = PrefabUtility.SaveAsPrefabAsset(initializerObject, path);

            Object.DestroyImmediate(initializerObject);

            return prefabObject;
        }

        private static ProjectInitSettings CreateInitSettings(string folderPath)
        {
            return ProjectInitSettingsEditor.CreateAsset(folderPath, false);
        }

        private static void CreateRecommendedFolders()
        {
            // Create each folder if it doesn't already exist
            foreach (string folder in RECOMMENDED_FOLDERS)
            {
                string folderPath = RECOMMENDED_BASE_FOLDER + folder;
                if (!AssetDatabase.IsValidFolder(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }
            }

            AssetDatabase.Refresh();
        }

        private static void AddSceneToBuildSettings(string scenePath)
        {
            // Check if the scene is already in Build Settings
            bool sceneExists = false;
            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (scene.path == scenePath)
                {
                    sceneExists = true;
                    break;
                }
            }

            // If the scene isn't already in Build Settings, add it
            if (!sceneExists)
            {
                var newScene = new EditorBuildSettingsScene(scenePath, true);
                var buildScenes = new EditorBuildSettingsScene[EditorBuildSettings.scenes.Length + 1];
                EditorBuildSettings.scenes.CopyTo(buildScenes, 0);
                buildScenes[buildScenes.Length - 1] = newScene;
                EditorBuildSettings.scenes = buildScenes;
            }
        }

        private static GameObject CreateLoadingGraphicsObject()
        {
            GameObject go = new GameObject("Loading Graphics");

            GameObject cameraObject = new GameObject("Loading Camera");
            cameraObject.transform.SetParent(go.transform);

            Camera camera = cameraObject.AddComponent<Camera>();

            GameObject canvasObject = new GameObject("Canvas");
            canvasObject.transform.SetParent(go.transform);

            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            CanvasScaler canvasScaler = canvasObject.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1080, 1920);
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;

            GameObject backgroundObject = new GameObject("Background");

            RectTransform backgroundRectTransform = backgroundObject.AddComponent<RectTransform>();
            backgroundRectTransform.SetParent(canvasObject.transform);
            backgroundRectTransform.anchorMin = new Vector2(0, 0);
            backgroundRectTransform.anchorMax = new Vector2(1, 1);
            backgroundRectTransform.pivot = new Vector2(0.5f, 0.5f);
            backgroundRectTransform.anchoredPosition = new Vector2(0, 0);
            backgroundRectTransform.sizeDelta = Vector2.zero;

            Image backgroundImage = backgroundObject.AddComponent<Image>();
            backgroundImage.color = new Color(0.5f, 0.6f, 1.0f, 1.0f);
            backgroundImage.raycastTarget = false;

            GameObject textObject = new GameObject("Text");

            RectTransform textRectTransform = textObject.AddComponent<RectTransform>();
            textRectTransform.SetParent(canvasObject.transform);
            textRectTransform.anchorMin = new Vector2(0, 0);
            textRectTransform.anchorMax = new Vector2(1, 0);
            textRectTransform.pivot = new Vector2(0.5f, 0);
            textRectTransform.anchoredPosition = new Vector2(0, 0);
            textRectTransform.sizeDelta = new Vector2(0, 350);

            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            text.alignment = TextAlignmentOptions.Converted;
            text.horizontalAlignment = HorizontalAlignmentOptions.Center;
            text.verticalAlignment = VerticalAlignmentOptions.Middle;
            text.overflowMode = TextOverflowModes.Overflow;
            text.richText = true;
            text.fontSize = 90;
            text.text = "Loading..";
            text.raycastTarget = false;

            LoadingGraphics loadingGraphics = go.AddComponent<LoadingGraphics>();

            SerializedObject serializedObject = new SerializedObject(loadingGraphics);
            serializedObject.Update();
            serializedObject.FindProperty("loadingText").objectReferenceValue = text;
            serializedObject.FindProperty("backgroundImage").objectReferenceValue = backgroundImage;
            serializedObject.FindProperty("canvasScaler").objectReferenceValue = canvasScaler;
            serializedObject.FindProperty("loadingCamera").objectReferenceValue = camera;
            serializedObject.ApplyModifiedProperties();

            return go;
        }
    }
}
