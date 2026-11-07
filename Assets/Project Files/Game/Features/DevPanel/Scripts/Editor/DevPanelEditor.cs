using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NebulaSoft
{
    [InitializeOnLoad]
    public static class DevPanelEditor
    {
        private const string MenuName = "Actions/Show Dev Panel";

        static DevPanelEditor()
        {
            // Subscribe to play mode state change event
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            // Subscribe to scene opened event
            EditorSceneManager.sceneOpened += OnSceneOpened;

            // Set the initial state of the menu item based on the settings
            EditorApplication.delayCall += () =>
            {
                try
                {
                    DevPanelSettings settings = EditorUtils.GetAsset<DevPanelSettings>();

                    if (settings == null) return;

                    DevPanelEnabler.LinkSettings(settings);

                    UpdateDevPanelState();
                }
                catch
                {
                    // Ignore any exceptions that may occur during the initial setup
                }
            };
        }

        [MenuItem(MenuName, priority = 201)]
        private static void ToggleAction()
        {
            DevPanelSettings settings = EditorUtils.GetAsset<DevPanelSettings>();

            if (settings == null) return;

            SerializedObject serializedObject = new SerializedObject(settings);
            serializedObject.Update();

            SerializedProperty activeProperty = serializedObject.FindProperty("isEnabled");
            activeProperty.boolValue = !activeProperty.boolValue;

            serializedObject.ApplyModifiedProperties();

            EditorUtility.SetDirty(settings);

            DevPanelEnabler.UpdateState();

            UpdateDevPanelState();
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            DevPanelSettings settings = EditorUtils.GetAsset<DevPanelSettings>();

            if (settings != null) return;

            DevPanelEnabler.LinkSettings(settings);

            UpdateDevPanelState();
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            UpdateDevPanelState();
        }

        public static void UpdateDevPanelState()
        {
            DevPanelSettings settings = EditorUtils.GetAsset<DevPanelSettings>();

            if (settings == null) return;

            UnityEditor.Menu.SetChecked(MenuName, settings.IsEnabled);

            DevPanel[] devPanels = GameObject.FindObjectsByType<DevPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (!devPanels.IsNullOrEmpty())
            {
                foreach (DevPanel panel in devPanels)
                {
                    panel.gameObject.SetActive(settings.IsEnabled);
                }
            }
        }
    }
}
