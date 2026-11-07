using System.IO;
using UnityEngine;
using System;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

namespace NebulaSoft
{
    public static class RuntimeEditorUtils
    {        
        /// <summary>
        /// Get asset in project
        /// </summary>
        public static T GetAsset<T>() where T : ScriptableObject
        {
#if UNITY_EDITOR
            Type type = typeof(T);

            string[] assets = AssetDatabase.FindAssets("t:" + type.Name);
            if (assets.Length > 0)
            {
                return (T)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(assets[0]), type);
            }
#endif

            return null;
        }

        public static T GetAssetByName<T>(string name = "") where T : Object
        {
#if UNITY_EDITOR
            string[] assets = AssetDatabase.FindAssets((string.IsNullOrEmpty(name) ? "" : name + " ") + "t:" + typeof(T).Name);
            if (assets.Length > 0)
            {
                if (string.IsNullOrEmpty(name))
                {
                    return (T)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(assets[0]), typeof(T));
                }
                else
                {
                    string assetPath;
                    for (int i = 0; i < assets.Length; i++)
                    {
                        assetPath = AssetDatabase.GUIDToAssetPath(assets[i]);
                        if (Path.GetFileNameWithoutExtension(assetPath) == name)
                        {
                            return (T)AssetDatabase.LoadAssetAtPath(assetPath, typeof(T));
                        }
                    }
                }
            }
#endif

            return null;
        }

        public static void Undo(Object obj, string name)
        {
#if UNITY_EDITOR
            UnityEditor.Undo.RecordObject(obj, name);
#endif
        }

        public static void SetDirty(Object obj)
        {
#if UNITY_EDITOR
            EditorUtility.SetDirty(obj);
#endif
        }

        public static void MoveComponentUp(this Component component)
        {
#if UNITY_EDITOR
            ComponentUtility.MoveComponentUp(component);
#endif
        }

        public static void MoveComponentDown(this Component component)
        {
#if UNITY_EDITOR
            ComponentUtility.MoveComponentDown(component);
#endif
        }

        public static void ToggleVisibility(this GameObject gameObject, bool state)
        {
#if UNITY_EDITOR
            SceneVisibilityManager.instance.ToggleVisibility(gameObject, state);
#endif
        }

        public static void TogglePicking(this GameObject gameObject, bool state)
        {
#if UNITY_EDITOR
            
            SceneVisibilityManager.instance.TogglePicking(gameObject, state);
#endif
        }
    }
}
