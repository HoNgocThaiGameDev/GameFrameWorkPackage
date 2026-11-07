using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

namespace NebulaSoft
{
    public static class SceneLoadingActionsMenu
    {
        [MenuItem("Actions/Game Scene", priority = 100)]
        private static void GameScene()
        {
            EditorSceneManager.OpenScene(Path.Combine(CoreEditor.FOLDER_SCENES, "Game.unity"));
        }

        [MenuItem("Actions/Game Scene", true)]
        private static bool GameSceneValidation()
        {
            return !Application.isPlaying;
        }

        [MenuItem("Actions/Menu Scene", priority = 100)]
        private static void MenuScene()
        {
            EditorSceneManager.OpenScene(Path.Combine(CoreEditor.FOLDER_SCENES, "Menu.unity"));
        }

        [MenuItem("Actions/Menu Scene", true)]
        private static bool MenuSceneValidation()
        {
            return !Application.isPlaying;
        }
    }
}