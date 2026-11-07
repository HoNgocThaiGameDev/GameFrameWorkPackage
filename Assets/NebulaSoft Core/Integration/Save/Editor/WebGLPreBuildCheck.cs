using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace NebulaSoft
{
    public class WebGLPreBuildCheck : IPreprocessBuildWithReport
    {
        public int callbackOrder => int.MaxValue;

        public void OnPreprocessBuild(BuildReport report)
        {
#if UNITY_WEBGL
            ProjectInitSettings projectInitSettings = EditorUtils.GetAsset<ProjectInitSettings>();
            if (projectInitSettings == null)
                return;

            SaveInitModule saveInitModule = projectInitSettings.GetModule<SaveInitModule>();
            if (saveInitModule == null)
                return;

            SerializedObject serializedObject = new SerializedObject(saveInitModule);
            SerializedProperty prefixProperty = serializedObject.FindProperty("webGLPrefix");

            if (!string.IsNullOrEmpty(prefixProperty.stringValue))
                return;

            // Generate a unique prefix based on the current time
            string uniquePrefix = $"build_{DateTime.UtcNow:yyyyMMdd_HHmmss}";

            serializedObject.Update();
            prefixProperty.stringValue = uniquePrefix;
            serializedObject.ApplyModifiedProperties(); 
            
            EditorUtility.SetDirty(saveInitModule);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

#endif
        }
    }
}