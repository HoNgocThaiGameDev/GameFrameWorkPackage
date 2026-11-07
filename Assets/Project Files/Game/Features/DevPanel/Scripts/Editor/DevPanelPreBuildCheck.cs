using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace NebulaSoft
{
    public class DevPanelPreBuildCheck : IPreprocessBuildWithReport
    {
        public int callbackOrder => int.MaxValue;

        public void OnPreprocessBuild(BuildReport report)
        {
            if(Application.isBatchMode)
                return;

            DevPanelSettings settings = EditorUtils.GetAsset<DevPanelSettings>();
            if (settings == null)
                return;

            if (settings.IsEnabled)
            {
                int option = EditorUtility.DisplayDialogComplex(
                    "Dev Panel Enabled",
                    "The Dev Panel is enabled. Do you want to leave it enabled, disable it, or cancel the build?",
                    "Leave",
                    "Disable",
                    "Cancel"
                );

                switch (option)
                {
                    case 0: // Leave
                        break;
                    case 1: // Disable
                        SerializedObject serializedObject = new SerializedObject(settings);

                        serializedObject.Update();
                        SerializedProperty activeProperty = serializedObject.FindProperty("isEnabled");
                        activeProperty.boolValue = false;
                        serializedObject.ApplyModifiedProperties();

                        EditorUtility.SetDirty(settings);

                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();
                        break;
                    case 2: // Cancel
                        throw new BuildFailedException("Build canceled by user due to Dev Panel being enabled.");
                }
            }
        }
    }
}