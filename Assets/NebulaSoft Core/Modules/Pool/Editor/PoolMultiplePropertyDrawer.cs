using System.Globalization;
using UnityEditor;
using UnityEngine;

namespace NebulaSoft
{
    [CustomPropertyDrawer(typeof(PoolMultiple))]
    public class PoolMultiplePropertyDrawer : UnityEditor.PropertyDrawer
    {
        private GUIStyle multiListLablesStyle;

        private void InitStyles()
        {
            Color labelColor = EditorGUIUtility.isProSkin ? new Color(1.0f, 1.0f, 1.0f) : new Color(0.12f, 0.12f, 0.12f);

            multiListLablesStyle = new GUIStyle();
            multiListLablesStyle.fontSize = 8;
            multiListLablesStyle.normal.textColor = labelColor;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            InitStyles();

            EditorGUI.BeginProperty(position, label, property);

            float x = position.x;
            float y = position.y;
            float width = (position.width - EditorGUIUtility.labelWidth);
            float height = EditorGUIUtility.singleLineHeight;

            SerializedProperty nameProperty = property.FindPropertyRelative("name");
            SerializedProperty poolsListProperty = property.FindPropertyRelative("multiPoolPrefabsList");

            property.isExpanded = EditorGUI.Foldout(new Rect(x, y, position.width, height), property.isExpanded, property.isExpanded ? property.displayName : GetLabel(property.displayName, nameProperty.stringValue), true);
            if (property.isExpanded)
            {
                x += 18;
                y += EditorGUIUtility.singleLineHeight + 2;
                width = position.width - 18;

                // Name
                EditorGUI.PropertyField(new Rect(x, y, width, height), nameProperty);

                // Pools list
                int arraySize = poolsListProperty.arraySize;

                y += EditorGUIUtility.singleLineHeight + 2;

                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUI.IntField(new Rect(x, y, width - 40, height), new GUIContent("Prefabs amount:"), arraySize);
                }

                if (GUI.Button(new Rect(x + width - 38, y, 18, 18), "-"))
                {

                }

                if (GUI.Button(new Rect(x + width - 18, y, 18, 18), "+"))
                {

                }

                // Titles
                y += EditorGUIUtility.singleLineHeight;

                EditorGUI.LabelField(new Rect(x + 2, y + 5, 60, 10), "objects", multiListLablesStyle);
                EditorGUI.LabelField(new Rect(x + width - 75, y + 5, 75, 10), "weights", multiListLablesStyle);

                // Array elements
                if (arraySize > 0)
                {
                    for(int i = 0; i < arraySize; i++)
                    {
                        y += EditorGUIUtility.singleLineHeight + 2;

                        SerializedProperty arrayProperty = poolsListProperty.GetArrayElementAtIndex(i);

                        SerializedProperty arrayPrefabProperty = arrayProperty.FindPropertyRelative("Prefab");
                        SerializedProperty arrayWeightProperty = arrayProperty.FindPropertyRelative("Weight");
                        SerializedProperty arrayIsLockedProperty = arrayProperty.FindPropertyRelative("isWeightLocked");

                        float prefabWidth = width * 0.6f;
                        float weightWidth = width * 0.1f;

                        EditorGUI.PropertyField(new Rect(x, y, prefabWidth, height), arrayPrefabProperty, GUIContent.none);
                        EditorGUI.PropertyField(new Rect(x + prefabWidth + 4 + weightWidth, y, weightWidth, height), arrayWeightProperty, GUIContent.none);
                        EditorGUI.LabelField(new Rect(x + prefabWidth + 4 + weightWidth + 4 + weightWidth, y, 50, height), new GUIContent(GetChance(poolsListProperty, arrayWeightProperty.intValue).ToString("P01", CultureInfo.InvariantCulture)));
                    }
                }
                else
                {

                }

                // Container
                SerializedProperty containerProperty = property.FindPropertyRelative("objectsContainer");

                y += EditorGUIUtility.singleLineHeight + 2;

                EditorGUI.PropertyField(new Rect(x, y, width, height), containerProperty);

                // Cap size
                SerializedProperty capSizeProperty = property.FindPropertyRelative("capSize");

                y += EditorGUIUtility.singleLineHeight + 2;

                EditorGUI.PropertyField(new Rect(x, y, width, height), capSizeProperty);

                if (capSizeProperty.boolValue)
                {
                    SerializedProperty maxSizeProperty = property.FindPropertyRelative("maxSize");

                    y += EditorGUIUtility.singleLineHeight + 2;

                    EditorGUI.PropertyField(new Rect(x, y, width, height), maxSizeProperty);
                }
            }

            EditorGUI.EndProperty();
        }

        private float GetChance(SerializedProperty arrayProperty, int weight)
        {
            int totalWeight = 0;

            int arraySize = arrayProperty.arraySize;
            for (int i = 0; i < arraySize; i++)
            {
                totalWeight += arrayProperty.GetArrayElementAtIndex(i).FindPropertyRelative("Weight").intValue;
            }

            return (float)weight / totalWeight;
        }

        private string GetLabel(string propertyName, string poolName)
        {
            if (!string.IsNullOrEmpty(poolName))
            {
                propertyName += " (" + poolName + ")";
            }

            return propertyName;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.isExpanded)
            {
                int variablesCount = 5;

                float height = EditorGUIUtility.singleLineHeight * variablesCount + 2 * (variablesCount - 1);

                height += EditorGUIUtility.singleLineHeight * 2 + 4;

                SerializedProperty poolsListProperty = property.FindPropertyRelative("multiPoolPrefabsList");
                height += poolsListProperty.arraySize * EditorGUIUtility.singleLineHeight;

                if (property.FindPropertyRelative("capSize").boolValue)
                    height += (EditorGUIUtility.singleLineHeight + 2);

                return height;
            }

            return EditorGUIUtility.singleLineHeight;
        }
    }
}