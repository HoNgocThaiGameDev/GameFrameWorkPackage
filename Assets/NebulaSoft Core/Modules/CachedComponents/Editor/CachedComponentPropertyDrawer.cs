using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NebulaSoft
{
    [CustomPropertyDrawer(typeof(ICachedComponent<>), true)]
    public class CachedComponentPropertyDrawer : PropertyDrawer
    {
        private static Dictionary<string, PropertyData> propertyData = new Dictionary<string, PropertyData>();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            float x = position.x;
            float y = position.y;
            float width = position.width;
            float height = EditorGUIUtility.singleLineHeight;

            PropertyData data = GetPropertyData(property);

            Rect rect = new Rect(x, y, width, height);
            property.isExpanded = EditorGUI.Foldout(new Rect(x, y, width, height), property.isExpanded, label, true);

            if (property.isExpanded)
            {
                rect.y += height + 2; // Add vertical offset

                EditorGUI.indentLevel++;

                foreach (string propertyName in data.CachedProperties)
                {
                    SerializedProperty subProperty = property.FindPropertyRelative(propertyName);

                    EditorGUI.PropertyField(rect, subProperty);

                    rect.y += EditorGUI.GetPropertyHeight(subProperty, true) + 2; // Add vertical offset
                }

                rect.x += 12;
                rect.width -= 12;

                if (GUI.Button(rect, "Cache"))
                {
                    Component component = data.GetTargetComponent();
                    if (component != null)
                    {
                        Component targetComponent = component.GetComponent(data.ComponentType);

                        if (targetComponent != null)
                        {
                            Undo.RecordObject(data.TargetObject, "Cache Component");

                            data.InvokeCacheMethod(targetComponent);

                            EditorUtility.SetDirty(component);
                        }
                    }
                }

                rect.y += height + 2; // Add vertical offset

                if (GUI.Button(rect, "Apply"))
                {
                    Component component = data.GetTargetComponent();
                    if (component != null)
                    {
                        Component targetComponent = component.GetComponent(data.ComponentType);

                        if (targetComponent != null)
                        {
                            Undo.RecordObject(targetComponent, "Apply Component");

                            data.InvokeApplyMethod(targetComponent);

                            EditorUtility.SetDirty(component);
                        }
                    }
                }

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUI.GetPropertyHeight(property, label, false);

            if (property.isExpanded)
            {
                PropertyData data = GetPropertyData(property);

                // Add properties height
                foreach (var propertyName in data.CachedProperties)
                {
                    height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative(propertyName), true) + 2;
                }

                // Add buttons height
                height += EditorGUIUtility.singleLineHeight * 2 + 4;

                return height;
            }

            return height;
        }

        private PropertyData GetPropertyData(SerializedProperty property)
        {
            string propertyPath = property.propertyPath;
            if (!propertyData.TryGetValue(propertyPath, out PropertyData data))
            {
                data = new PropertyData(property);
                propertyData.Add(propertyPath, data);
            }

            return data;
        }

        private class PropertyData
        {
            public string[] CachedProperties { get; private set; }

            public Type ComponentType { get; private set; }

            public Object TargetObject { get; private set; }

            public object variableObject;
            private FieldInfo variableFieldInfo;

            private FieldInfo customTargetFieldInfo;

            public MethodInfo ApplyMethodInfo { get; private set; }
            public MethodInfo CacheMethodInfo { get; private set; }

            public PropertyData(SerializedProperty property)
            {
                CachedProperties = CacheProperties(property);

                // Get the target object
                TargetObject = property.serializedObject.targetObject;

                // Get the type of the target object
                Type targetType = TargetObject.GetType();

                // Get the field info using the property path
                variableFieldInfo = targetType.GetField(property.propertyPath, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                variableObject = variableFieldInfo.GetValue(TargetObject);

                // Cache methods
                ApplyMethodInfo = variableFieldInfo.FieldType.GetMethod("Apply", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                CacheMethodInfo = variableFieldInfo.FieldType.GetMethod("Cache", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

                foreach (Type interfaceType in variableFieldInfo.FieldType.GetInterfaces())
                {
                    if (interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(ICachedComponent<>))
                    {
                        ComponentType = interfaceType.GetGenericArguments()[0];

                        break;
                    }
                }

                // Cache custom target
                Attribute attribute = variableFieldInfo.GetCustomAttribute(typeof(CachedTargetAttribute), true);
                if (attribute != null)
                {
                    CachedTargetAttribute cachedTargetAttribute = (CachedTargetAttribute)attribute;
                    if (cachedTargetAttribute != null)
                    {
                        customTargetFieldInfo = targetType.GetField(cachedTargetAttribute.TargetVariableName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                        if (customTargetFieldInfo == null)
                        {
                            Debug.LogWarning($"Target variable with name {cachedTargetAttribute.TargetVariableName} can't be found!", TargetObject);
                        }
                    }
                }
            }

            private string[] CacheProperties(SerializedProperty property)
            {
                List<string> properties = new List<string>();

                SerializedProperty iterator = property.Copy();
                SerializedProperty end = iterator.GetEndProperty();

                iterator.NextVisible(enterChildren: true);

                int propertyDepth = iterator.depth;

                do
                {
                    if (SerializedProperty.EqualContents(iterator, end))
                        break;

                    if (propertyDepth != iterator.depth)
                        continue;

                    properties.Add(iterator.name);
                }
                while (iterator.NextVisible(enterChildren: true));

                return properties.ToArray();
            }

            public void InvokeCacheMethod(object component)
            {
                CacheMethodInfo?.Invoke(variableObject, new object[] { component });
            }

            public void InvokeApplyMethod(object component)
            {
                ApplyMethodInfo?.Invoke(variableObject, new object[] { component });
            }

            public Component GetTargetComponent()
            {
                if(customTargetFieldInfo != null)
                {
                    object customTargetObject = customTargetFieldInfo.GetValue(TargetObject);
                    if (customTargetObject != null)
                    {
                        return (Component)customTargetObject;
                    }
                }

                Component component = (Component)TargetObject;
                if (component != null)
                {
                    return component.GetComponent(ComponentType);
                }

                return null;
            }
        }
    }
}