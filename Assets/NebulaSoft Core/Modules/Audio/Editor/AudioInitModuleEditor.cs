using UnityEngine;
using UnityEditor;
using System.IO;

namespace NebulaSoft
{
    [CustomEditor(typeof(AudioInitModule))]
    public class AudioInitModuleEditor : InitModuleEditor
    {
        public override void OnCreated()
        {
            AudioClips audioClips = EditorUtils.GetAsset<AudioClips>();
            if(audioClips == null)
            {
                audioClips = (AudioClips)ScriptableObject.CreateInstance<AudioClips>();
                audioClips.name = "Audio Clips";

                string referencePath = AssetDatabase.GetAssetPath(target);
                string directoryPath = Path.GetDirectoryName(referencePath);

                // Create a unique file path for the ScriptableObject
                string assetPath = Path.Combine(directoryPath, audioClips.name + ".asset");
                assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);

                // Save the ScriptableObject to the determined path
                AssetDatabase.CreateAsset(audioClips, assetPath);
                AssetDatabase.SaveAssets();

                EditorUtility.SetDirty(target);
            }

            serializedObject.Update();
            serializedObject.FindProperty("audioSettings").objectReferenceValue = audioClips;
            serializedObject.ApplyModifiedProperties();
        }
    }
}
