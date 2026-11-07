using UnityEngine;
using UnityEditor;
using System.IO;

namespace NebulaSoft
{
    [CustomEditor(typeof(DevPanelInitModule))]
    public class DevPanelInitModuleEditor : InitModuleEditor
    {
        public override void OnCreated()
        {
            DevPanelSettings settings = EditorUtils.GetAsset<DevPanelSettings>();
            if (settings == null)
            {
                settings = (DevPanelSettings)ScriptableObject.CreateInstance<DevPanelSettings>();
                settings.name = "Dev Panel Settings";

                string referencePath = AssetDatabase.GetAssetPath(target);
                string directoryPath = Path.GetDirectoryName(referencePath);

                // Create a unique file path for the ScriptableObject
                string assetPath = Path.Combine(directoryPath, settings.name + ".asset");
                assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);

                // Save the ScriptableObject to the determined path
                AssetDatabase.CreateAsset(settings, assetPath);
                AssetDatabase.SaveAssets();

                EditorUtility.SetDirty(target);
            }

            serializedObject.Update();
            serializedObject.FindProperty("settings").objectReferenceValue = settings;
            serializedObject.ApplyModifiedProperties();
        }
    }
}