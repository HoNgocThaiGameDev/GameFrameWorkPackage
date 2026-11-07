using UnityEditor;
using UnityEngine;

namespace NebulaSoft
{
    public static class CorePreferences
    {
        // Create the SettingsProvider and register it to the Preferences window
        [SettingsProvider]
        public static SettingsProvider CustomPreferencesMenu()
        {
            CoreSettings coreSettings = EditorUtils.GetAsset<CoreSettings>();

            Editor editor = null;
            if(coreSettings != null)
            {
                editor = Editor.CreateEditor(coreSettings);
            }

            // Create a new SettingsProvider with a path in the Preferences window
            SettingsProvider provider = new SettingsProvider("Preferences/NebulaSoft Core", SettingsScope.User)
            {
                // Label of the preferences page
                label = "NebulaSoft Core",

                // This method is called to draw the GUI
                guiHandler = (searchContext) =>
                {
                    EditorGUILayout.BeginHorizontal();

                    GUILayout.Space(10);
                    EditorGUILayout.BeginVertical();

                    if (editor != null)
                    {
                        editor.OnInspectorGUI();
                    }
                    else
                    {
                        EditorGUILayout.LabelField("Core Settings file can't be found!");
                    }

                    EditorGUILayout.EndVertical();
                    EditorGUILayout.EndHorizontal();
                },

                // Define the keywords to be used in the search bar
                keywords = new string[] { "Custom", "Preferences", "NebulaSoft Core", "Core" }
            };

            return provider;
        }
    }
}