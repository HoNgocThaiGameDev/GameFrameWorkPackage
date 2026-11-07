using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NebulaSoft
{
    public static class EditorUtils
    {
        public readonly static string projectFolderPath = Application.dataPath.Replace("/Assets", "/");

        private static readonly Regex arrayRegex = new Regex("Array.data");

        public static GenericMenu GetSubTypeMenu(Type parentType, Action<Type> selectAction, Type selectedType = null, bool showAbstract = false)
        {
            GenericMenu menu = new GenericMenu();

            if (!parentType.IsAbstract)
                menu.AddItem(new GUIContent(parentType.ToString()), parentType == selectedType, delegate { selectAction(parentType); });

            Type[] assemblyTypes = Assembly.GetAssembly(parentType).GetTypes();
            Type[] itemTypes = assemblyTypes.Where(type => type.IsSubclassOf(parentType) || type.Equals(parentType)).ToArray();
            Type[] baseItemTypes = itemTypes.Where(type => type.BaseType == parentType).ToArray();
            foreach (Type baseType in baseItemTypes)
            {
                SubType(ref menu, itemTypes, baseType, selectAction, "", selectedType, showAbstract);
            }

            return menu;
        }

        private static void SubType(ref GenericMenu menu, Type[] itemTypes, Type baseType, Action<Type> selectAction, string defaultPath = "", Type selectedType = null, bool showAbstract = false)
        {
            Type[] subItemTypes = itemTypes.Where(type => type.BaseType == baseType).ToArray();

            if (subItemTypes.Length > 0)
            {
                if (showAbstract || !baseType.IsAbstract)
                    menu.AddItem(new GUIContent(defaultPath + baseType.ToString() + "/" + baseType.ToString()), baseType == selectedType, delegate { selectAction(baseType); });

                foreach (Type subType in subItemTypes)
                {
                    SubType(ref menu, itemTypes, subType, selectAction, defaultPath + baseType.ToString() + "/", selectedType);
                }
            }
            else
            {
                if (showAbstract || !baseType.IsAbstract)
                    menu.AddItem(new GUIContent(defaultPath + baseType.ToString()), baseType == selectedType, delegate { selectAction(baseType); });
            }
        }

        public static void SelectAsset(SerializedProperty serializedProperty)
        {
            Object objectReference = serializedProperty.objectReferenceValue;

            if (objectReference != null)
            {
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = objectReference;
            }
        }

        public static void SelectAsset(Object objectReference)
        {
            if (objectReference != null)
            {
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = objectReference;
            }
        }

        /// <summary>
        /// Get asset in project
        /// </summary>
        public static Object GetAsset(Type type)
        {
            string[] assets = AssetDatabase.FindAssets("t:" + type.Name);
            if (assets.Length > 0)
            {
                return AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(assets[0]), type);
            }

            return null;
        }

        /// <summary>
        /// Get asset in project
        /// </summary>
        public static T GetAsset<T>(string name = "") where T : Object
        {
            string[] assets = AssetDatabase.FindAssets((string.IsNullOrEmpty(name) ? "" : name + " ") + "t:" + typeof(T).Name);
            if (assets.Length > 0)
            {
                return (T)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(assets[0]), typeof(T));
            }

            return null;
        }

        /// <summary>
        /// Get asset in project
        /// </summary>
        public static T GetAssetByName<T>(string name = "") where T : Object
        {
            string[] assets = AssetDatabase.FindAssets((string.IsNullOrEmpty(name) ? "" : name + " ") + "t:" + typeof(T).Name);
            if (assets.Length > 0)
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

            return null;
        }

        /// <summary>
        /// Get assets in project
        /// </summary>
        public static T[] GetAssets<T>(string name = "") where T : Object
        {
            string[] assetsPath = AssetDatabase.FindAssets((string.IsNullOrEmpty(name) ? "" : name + " ") + "t:" + typeof(T).Name);
            if (assetsPath.Length > 0)
            {
                T[] assets = new T[assetsPath.Length];

                for (int i = 0; i < assets.Length; i++)
                {
                    assets[i] = (T)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(assetsPath[i]), typeof(T));
                }

                return assets;
            }

            return null;
        }

        /// <summary>
        /// Check if project contains asset
        /// </summary>
        public static bool HasAsset<T>(string name = "") where T : Object
        {
            string[] assets = AssetDatabase.FindAssets((string.IsNullOrEmpty(name) ? "" : name + " ") + "t:" + typeof(T).Name);
            if (assets.Length > 0)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Create ScriptableObject at path
        /// </summary>
        public static T CreateAsset<T>(System.Type type, string path, bool refresh = false) where T : ScriptableObject
        {
            T scriptableObject = (T)ScriptableObject.CreateInstance(type);

            string itemPath = path + ".asset";

            AssetDatabase.CreateAsset(scriptableObject, itemPath);

            AssetDatabase.SaveAssets();

            if (refresh)
                AssetDatabase.Refresh();

            return scriptableObject;
        }

        /// <summary>
        /// Create ScriptableObject at path
        /// </summary>
        public static T CreateAsset<T>(string path, bool refresh = false) where T : ScriptableObject
        {
            T scriptableObject = (T)ScriptableObject.CreateInstance(typeof(T));

            string itemPath = path + ".asset";

            AssetDatabase.CreateAsset(scriptableObject, itemPath);

            AssetDatabase.SaveAssets();

            if (refresh)
                AssetDatabase.Refresh();

            return scriptableObject;
        }

        public static T CreateScriptableObject<T>(string assetName) where T : ScriptableObject
        {
            T scriptableObject = ScriptableObject.CreateInstance<T>();

            string selectionPath = AssetDatabase.GetAssetPath(Selection.activeObject);

            selectionPath = Directory.Exists(selectionPath) ? selectionPath : Path.GetDirectoryName(selectionPath).Replace("\\", "/");

            if (string.IsNullOrEmpty(selectionPath))
                selectionPath = "Assets";

            string assetExtension = ".asset";
            string resultPath = selectionPath + "/" + assetName + assetExtension;

            for (int i = 1; File.Exists(resultPath); i++)
                resultPath = selectionPath + "/" + assetName + " " + i + assetExtension;

            AssetDatabase.CreateAsset(scriptableObject, resultPath);

            Selection.activeObject = scriptableObject;

            return scriptableObject;
        }

        public static string FindFolderPath(string folderName)
        {
            string resultFolder = FindSubfolder(folderName, Application.dataPath);

            if (string.IsNullOrEmpty(resultFolder))
            {
                Debug.LogWarning("Folder " + folderName + " couldn't be found!");
            }

            return resultFolder;
        }

        private static string FindSubfolder(string folderName, string rootPath)
        {
            string[] subdirectoryEntries = Directory.GetDirectories(rootPath);

            string result = "";

            foreach (string subdirectory in subdirectoryEntries)
            {
                if (string.Compare(Path.GetFileName(subdirectory), folderName) == 0)
                    return subdirectory;

                result = FindSubfolder(folderName, subdirectory);

                if (!string.IsNullOrEmpty(result))
                    break;
            }

            return result;
        }

        public static bool IsArray(SerializedProperty property)
        {
            return IsArray(property.propertyPath);
        }

        public static bool IsArray(string propertyPath)
        {
            return arrayRegex.IsMatch(propertyPath);
        }

        public static string ConvertToRelativePath(string absolutePath)
        {
            if (string.IsNullOrEmpty(absolutePath)) return null;

            Uri projectUri = new Uri(Application.dataPath);
            Uri absoluteUri = new Uri(absolutePath);

            string relativePath = Uri.UnescapeDataString(projectUri.MakeRelativeUri(absoluteUri).ToString());

            if (string.IsNullOrEmpty(relativePath))
                relativePath = "Assets";

            return relativePath.Replace("\\", "/");
        }
    }
}
