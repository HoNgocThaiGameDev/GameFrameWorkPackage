using UnityEditor;
using UnityEngine;

namespace NebulaSoft
{
    [CustomPropertyDrawer(typeof(Pool))]
    public class PoolPropertyDrawer : UnityEditor.PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            float x = position.x;
            float y = position.y;
            float width = (position.width - EditorGUIUtility.labelWidth);
            float height = EditorGUIUtility.singleLineHeight;

            SerializedProperty nameProperty = property.FindPropertyRelative("name");
            SerializedProperty prefabProperty = property.FindPropertyRelative("prefab");

            string labelText = property.isExpanded ? property.displayName : GetLabel(property.displayName, nameProperty.stringValue);

            property.isExpanded = EditorGUI.Foldout(new Rect(x, y, position.width, height), property.isExpanded, GUIContent.none, true);
            EditorGUI.PrefixLabel(new Rect(x, y, EditorGUIUtility.labelWidth - 15, height), new GUIContent(labelText, labelText));

            if(property.isExpanded)
            {
                x += 18;
                y += EditorGUIUtility.singleLineHeight + 2;
                width = position.width - 18;

                // Name
                EditorGUI.PropertyField(new Rect(x, y, width, height), nameProperty);

                // Prefab
                y += EditorGUIUtility.singleLineHeight + 2;

                EditorGUI.BeginChangeCheck();

                EditorGUI.PropertyField(new Rect(x, y, width, height), prefabProperty);

                if(EditorGUI.EndChangeCheck())
                {
                    if(prefabProperty.objectReferenceValue != null && string.IsNullOrEmpty(nameProperty.stringValue))
                    {
                        nameProperty.stringValue = prefabProperty.objectReferenceValue.name;
                    }
                }

                // Container
                SerializedProperty containerProperty = property.FindPropertyRelative("objectsContainer");

                y += EditorGUIUtility.singleLineHeight + 2;

                EditorGUI.PropertyField(new Rect(x, y, width, height), containerProperty);

                // Cap size
                SerializedProperty capSizeProperty = property.FindPropertyRelative("capSize");

                y += EditorGUIUtility.singleLineHeight + 2;

                EditorGUI.PropertyField(new Rect(x, y, width, height), capSizeProperty);

                if(capSizeProperty.boolValue)
                {
                    SerializedProperty maxSizeProperty = property.FindPropertyRelative("maxSize");

                    y += EditorGUIUtility.singleLineHeight + 2;

                    EditorGUI.PropertyField(new Rect(x, y, width, height), maxSizeProperty);
                }
            }
            else
            {
                using(new EditorGUI.DisabledScope(true))
                {
                    EditorGUI.PropertyField(new Rect(x + EditorGUIUtility.labelWidth, y, width, height), prefabProperty, GUIContent.none);
                }
            }

            EditorGUI.EndProperty();
        }

        private string GetLabel(string propertyName, string poolName)
        {
            if(!string.IsNullOrEmpty(poolName))
            {
                propertyName += " (" + poolName + ")";
            }

            return propertyName;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if(property.isExpanded)
            {
                int variablesCount = 5;

                float height = EditorGUIUtility.singleLineHeight * variablesCount + 2 * (variablesCount - 1);

                if (property.FindPropertyRelative("capSize").boolValue)
                    height += (EditorGUIUtility.singleLineHeight + 2);

                return height;
            }

            return EditorGUIUtility.singleLineHeight;
        }
    }
}