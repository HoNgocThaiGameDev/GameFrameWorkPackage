using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NebulaSoft
{
    [InitializeOnLoad]
    public static class AutoInitializerLoader
    {
        static AutoInitializerLoader()
        {
            EditorSceneManager.activeSceneChangedInEditMode += OnSceneChanged;

            RewriteStartScene();
        }

        private static void RewriteStartScene()
        {
            if (CoreEditor.AutoLoadInitializer)
            {
                Scene currentScene = SceneManager.GetActiveScene();
                if (currentScene != null)
                {
                    string path1 = currentScene.path;
                    string path2 = CoreEditor.FOLDER_SCENES;

                    if (!string.IsNullOrEmpty(path1) && !string.IsNullOrEmpty(path2))
                    {
                        path1 = path1.Replace('\\', '/');
                        path2 = path2.Replace('\\', '/');

                        // Scene is located in the default Core Scenes folder
                        // This check is important to ignore all scenes that are outside the default folder
                        // and to allow running them without launching the game from the Init scene
                        if (path1.StartsWith(path2))
                        {
                            if (currentScene.name != CoreEditor.InitSceneName)
                            {
#if UNITY_6000_0_OR_NEWER
                                Initializer initializer = Object.FindFirstObjectByType<Initializer>();
#else
                                Initializer initializer = Object.FindObjectOfType<Initializer>();
#endif

                                if (initializer == null)
                                {
                                    SceneAsset gameScene = EditorUtils.GetAsset<SceneAsset>("Init");
                                    if (gameScene != null)
                                    {
                                        GameLoading.LoadingSceneBuildIndex = currentScene.buildIndex;
                                        EditorSceneManager.playModeStartScene = gameScene;

                                        return;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            GameLoading.LoadingSceneBuildIndex = -1;
            EditorSceneManager.playModeStartScene = null;
        }

        private static void OnSceneChanged(Scene oldScene, Scene newScene)
        {
            RewriteStartScene();
        }
    }
}