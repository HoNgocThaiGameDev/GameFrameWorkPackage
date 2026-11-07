using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEditor.Overlays;
using UnityEditor.Toolbars;
using System.IO;

namespace NebulaSoft
{
    [Overlay(typeof(SceneView), "Scenes Toolbar")]
    [Icon("Assets/Project Files/NebulaSoft Core/Extended Editor/Icons/icon_settings.png")]
    public class EditorScenesToolbar : ToolbarOverlay
    {
        EditorScenesToolbar() : base(ScenesDropwdown.id)
        {

        }

        [EditorToolbarElement(id, typeof(SceneView))]
        class ScenesDropwdown : EditorToolbarDropdown
        {
            public const string id = "ScenesToolbar/Dropdown";

            private static string targetScenePath;

            public ScenesDropwdown()
            {
                text = "Scenes";
                clicked += ShowDropdown;
            }

            private void ShowDropdown()
            {
                GenericMenu menu = new GenericMenu();

                int sceneCount = EditorSceneManager.sceneCountInBuildSettings;
                for (int i = 0; i < sceneCount; i++)
                {
                    string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
                    string sceneName = Path.GetFileNameWithoutExtension(scenePath);
                    Scene scene = SceneManager.GetSceneByPath(scenePath);

                    menu.AddItem(new GUIContent(sceneName), scene.isLoaded, scene.isLoaded ? () => UnloadScene(scene) : () => OpenScene(scenePath));
                }

                menu.ShowAsContext();
            }

            private void UnloadScene(Scene scene)
            {
                if (SceneManager.sceneCount > 1)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }

            private void OpenScene(string scenePath)
            {
                targetScenePath = scenePath;
            }

            [UnityEditor.Callbacks.DidReloadScripts]
            private static void ScriptsHasBeenReloaded()
            {
                SceneView.duringSceneGui += DuringSceneGui;
            }

            private static void DuringSceneGui(SceneView view)
            {
                Event e = Event.current;

                if (!string.IsNullOrEmpty(targetScenePath))
                {
                    if(!Application.isPlaying)
                    {
                        EditorSceneManager.OpenScene(targetScenePath, e.shift ? OpenSceneMode.Additive : OpenSceneMode.Single);
                    }
                    else
                    {
                        SceneManager.LoadScene(SceneUtility.GetBuildIndexByScenePath(targetScenePath), e.shift ? LoadSceneMode.Additive : LoadSceneMode.Single);
                    }

                    targetScenePath = string.Empty;
                }

            }
        }
    }

}