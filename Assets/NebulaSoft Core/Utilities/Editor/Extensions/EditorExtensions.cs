using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NebulaSoft
{
    public static class EditorExtensions
    {
        /// <summary>
        /// Create SciptableObject from code
        /// </summary>
        public static T CreateItem<T>(Type type, string fullPath) where T : ScriptableObject
        {
            T item = (T)ScriptableObject.CreateInstance(type);

            string objectPath = fullPath + ".asset";

            if (AssetDatabase.LoadAssetAtPath<T>(objectPath) != null)
            {
                return AssetDatabase.LoadAssetAtPath<T>(objectPath);
            }

            AssetDatabase.CreateAsset(item, objectPath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return item;
        }

        /// <summary>
        /// Add element to SerializedProperty array
        /// </summary>
        public static void AddToObjectArray<T>(this SerializedProperty arrayProperty, T elementToAdd) where T : Object
        {
            // If the SerializedProperty this is being called from is not an array, throw an exception.
            if (!arrayProperty.isArray)
                throw new UnityException("SerializedProperty " + arrayProperty.name + " is not an array.");

            // Pull all the information from the target of the serializedObject.
            arrayProperty.serializedObject.Update();

            // Add a null array element to the end of the array then populate it with the object parameter.
            arrayProperty.InsertArrayElementAtIndex(arrayProperty.arraySize);
            arrayProperty.GetArrayElementAtIndex(arrayProperty.arraySize - 1).objectReferenceValue = elementToAdd;

            // Push all the information on the serializedObject back to the target.
            arrayProperty.serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Remove element from SerializedProperty array
        /// </summary>
        public static void RemoveFromObjectArrayAt(this SerializedProperty arrayProperty, int index)
        {
            // If the index is not appropriate or the serializedProperty this is being called from is not an array, throw an exception.
            if (index < 0)
                throw new UnityException("SerializedProperty " + arrayProperty.name + " cannot have negative elements removed.");

            if (!arrayProperty.isArray)
                throw new UnityException("SerializedProperty " + arrayProperty.name + " is not an array.");

            if (index > arrayProperty.arraySize - 1)
                throw new UnityException("SerializedProperty " + arrayProperty.name + " has only " + arrayProperty.arraySize + " elements so element " + index + " cannot be removed.");

            // Pull all the information from the target of the serializedObject.
            arrayProperty.serializedObject.Update();

            // If there is a non-null element at the index, null it.
            if (arrayProperty.GetArrayElementAtIndex(index).objectReferenceValue != null)
                arrayProperty.DeleteArrayElementAtIndex(index);

            // Delete the null element from the array at the index.
            arrayProperty.DeleteArrayElementAtIndex(index);

            // Push all the information on the serializedObject back to the target.
            arrayProperty.serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Use this to remove an object from an object array represented by a SerializedProperty.
        /// </summary>
        /// <typeparam name="T">Type of object to be removed.</typeparam>
        /// <param name="arrayProperty">Property that contains array.</param>
        /// <param name="elementToRemove">Element to be removed.</param>
        public static void RemoveFromObjectArray<T>(this SerializedProperty arrayProperty, T elementToRemove) where T : Object
        {
            // If either the serializedProperty doesn't represent an array or the element is null, throw an exception.
            if (!arrayProperty.isArray)
                throw new UnityException("SerializedProperty " + arrayProperty.name + " is not an array.");

            if (!elementToRemove)
                throw new UnityException("Removing a null element is not supported using this method.");

            // Pull all the information from the target of the serializedObject.
            arrayProperty.serializedObject.Update();

            // Go through all the elements in the serializedProperty's array...
            for (int i = 0; i < arrayProperty.arraySize; i++)
            {
                SerializedProperty elementProperty = arrayProperty.GetArrayElementAtIndex(i);

                // ... until the element matches the parameter...
                if (elementProperty.objectReferenceValue == elementToRemove)
                {
                    // ... then remove it.
                    arrayProperty.RemoveFromObjectArrayAt(i);
                    return;
                }
            }

            throw new UnityException("Element " + elementToRemove.name + "was not found in property " + arrayProperty.name);
        }

        /// <summary>
        /// Use this to remove the object at an index from an object array represented by a SerializedProperty.
        /// </summary>
        public static void RemoveFromVariableArrayAt(this SerializedProperty arrayProperty, int index)
        {
            // If the index is not appropriate or the serializedProperty this is being called from is not an array, throw an exception.
            if (index < 0)
                throw new UnityException("SerializedProperty " + arrayProperty.name + " cannot have negative elements removed.");

            if (!arrayProperty.isArray)
                throw new UnityException("SerializedProperty " + arrayProperty.name + " is not an array.");

            if (index > arrayProperty.arraySize - 1)
                throw new UnityException("SerializedProperty " + arrayProperty.name + " has only " + arrayProperty.arraySize + " elements so element " + index + " cannot be removed.");

            // Pull all the information from the target of the serializedObject.
            arrayProperty.serializedObject.Update();

            // Delete the null element from the array at the index.
            arrayProperty.DeleteArrayElementAtIndex(index);

            // Push all the information on the serializedObject back to the target.
            arrayProperty.serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Get object from serializedProperty
        /// </summary>
        public static object GetPropertyObject(SerializedProperty property)
        {
            string[] path = property.propertyPath.Split('.');
            object baseObject = property.serializedObject.targetObject;
            Type baseType = baseObject.GetType();

            for (int i = 0; i < path.Length; i++)
            {
                FieldInfo fieldInfo = baseType.GetField(path[i], BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

                baseType = fieldInfo.FieldType;

                baseObject = fieldInfo.GetValue(baseObject);
            }

            return baseObject;
        }

        /// <summary>
        /// Get full path to serializedProperty
        /// </summary>
        public static string GetPropertyPath(this SerializedProperty property)
        {
            return property.propertyPath.Split(new string[] { ".Array" }, StringSplitOptions.None)[0];
        }

        /// <summary>
        /// Get serializedProperty id from path
        /// </summary>
        public static int GetPropertyArrayIndex(this SerializedProperty property)
        {
            int index1 = property.propertyPath.LastIndexOf('[');
            int index2 = property.propertyPath.LastIndexOf(']');
            return int.Parse(property.propertyPath.Substring(index1 + 1, index2 - index1 - 1));
        }

        public static bool AddObject<T>(this SerializedProperty property, T addedObject) where T : ScriptableObject
        {
            if (property.isArray)
            {
                if (addedObject != null)
                {
                    property.serializedObject.Update();

                    int index = property.arraySize;

                    property.arraySize++;
                    property.GetArrayElementAtIndex(index).objectReferenceValue = addedObject;

                    property.serializedObject.ApplyModifiedProperties();

                    return true;
                }
            }

            return false;
        }

        public static bool RemoveObject(this SerializedProperty property, int index, string title = "This object will be removed!", string content = "Are you sure?")
        {
            if (EditorUtility.DisplayDialog(title, content, "Remove", "Cancel"))
            {
                if (property.isArray)
                {
                    string assetPath = AssetDatabase.GetAssetPath(property.GetArrayElementAtIndex(index).objectReferenceValue);

                    property.RemoveFromObjectArrayAt(index);

                    if (File.Exists(EditorUtils.projectFolderPath + assetPath))
                    {
                        AssetDatabase.DeleteAsset(assetPath);
                    }

                    return true;
                }
            }

            return false;
        }

        public static void SelectSourceObject(this SerializedProperty property)
        {
            if (property.objectReferenceValue != null)
            {
                EditorUtility.FocusProjectWindow();

                EditorGUIUtility.PingObject(property.objectReferenceValue);
            }
        }

        public static void ClearProperty(this SerializedProperty property)
        {
            SerializedProperty iterator = property.Copy();

            while (iterator.NextVisible(true) && iterator.propertyPath.Contains(property.propertyPath))
            {
                ClearValueProperty(iterator);
            }
        }

        private static void ClearValueProperty(SerializedProperty property)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Generic:
                    break;
                case SerializedPropertyType.Integer:
                    property.intValue = 0;
                    break;
                case SerializedPropertyType.Boolean:
                    property.boolValue = false;
                    break;
                case SerializedPropertyType.Float:
                    property.floatValue = 0;
                    break;
                case SerializedPropertyType.String:
                    property.stringValue = string.Empty;
                    break;
                case SerializedPropertyType.Color:
                    property.colorValue = Color.white;
                    break;
                case SerializedPropertyType.ObjectReference:
                    property.objectReferenceValue = null;
                    break;
                case SerializedPropertyType.LayerMask:
                    break;
                case SerializedPropertyType.Enum:
                    property.enumValueIndex = 0;
                    break;
                case SerializedPropertyType.Vector2:
                    property.vector2Value = Vector2.zero;
                    break;
                case SerializedPropertyType.Vector3:
                    property.vector3Value = Vector3.zero;
                    break;
                case SerializedPropertyType.Vector4:
                    property.vector4Value = Vector4.zero;
                    break;
                case SerializedPropertyType.Rect:
                    property.rectValue = Rect.zero;
                    break;
                case SerializedPropertyType.ArraySize:
                    property.arraySize = 0;
                    break;
                case SerializedPropertyType.Character:
                    break;
                case SerializedPropertyType.AnimationCurve:
                    property.animationCurveValue = AnimationCurve.Constant(0, 0, 0);
                    break;
                case SerializedPropertyType.Bounds:
                    property.boundsValue = new Bounds(Vector3.zero, Vector3.zero);
                    break;
                case SerializedPropertyType.Gradient:
                    property.gradientValue = new Gradient();
                    break;
                case SerializedPropertyType.Quaternion:
                    property.quaternionValue = Quaternion.identity;
                    break;
                case SerializedPropertyType.ExposedReference:
                    property.exposedReferenceValue = null;
                    break;
                case SerializedPropertyType.FixedBufferSize:
                    break;
                case SerializedPropertyType.Vector2Int:
                    property.vector2IntValue = Vector2Int.zero;
                    break;
                case SerializedPropertyType.Vector3Int:
                    property.vector3IntValue = Vector3Int.zero;
                    break;
                case SerializedPropertyType.RectInt:
                    property.rectIntValue = new RectInt();
                    break;
                case SerializedPropertyType.BoundsInt:
                    property.boundsIntValue = new BoundsInt();
                    break;
                case SerializedPropertyType.ManagedReference:
                    property.managedReferenceValue = null;
                    break;
                case SerializedPropertyType.Hash128:
                    property.hash128Value = new Hash128();
                    break;
            }
        }

        public static IEnumerable<SerializedProperty> GetChildren(this SerializedProperty property)
        {
            property = property.Copy();
            var nextElement = property.Copy();
            bool hasNextElement = nextElement.NextVisible(false);
            if (!hasNextElement)
            {
                nextElement = null;
            }

            property.NextVisible(true);
            while (true)
            {
                if ((SerializedProperty.EqualContents(property, nextElement)))
                {
                    yield break;
                }

                yield return property;

                bool hasNext = property.NextVisible(false);
                if (!hasNext)
                {
                    break;
                }
            }
        }
    }
}