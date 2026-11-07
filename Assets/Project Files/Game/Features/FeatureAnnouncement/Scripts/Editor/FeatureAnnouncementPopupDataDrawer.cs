using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace NebulaSoft
{
    [CustomPropertyDrawer(typeof(FeatureAnnouncementPopupData))]
    public class FeatureAnnouncementPopupDataDrawer : PropertyDrawer
    {
        private const float TOGGLE_WIDTH = 18f;
        private const float INDENT = 16f;

        private static readonly string[] SERIALIZED_FIELD_NAMES;

        static FeatureAnnouncementPopupDataDrawer()
        {
            List<string> fieldNames = new List<string>();
            System.Type type = typeof(FeatureAnnouncementPopupData);
            FieldInfo[] fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);

            foreach (FieldInfo field in fields)
            {
                if (field.Name == "showAnnouncementPopup")
                    continue;

                if (field.GetCustomAttribute<SerializeField>() != null)
                    fieldNames.Add(field.Name);
            }

            SERIALIZED_FIELD_NAMES = fieldNames.ToArray();
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty showPopupProp = property.FindPropertyRelative("showAnnouncementPopup");

            Rect toggleRect = new Rect(position.x + position.width - TOGGLE_WIDTH - 12, position.y, TOGGLE_WIDTH, EditorGUIUtility.singleLineHeight);

            if (!showPopupProp.boolValue)
            {
                float labelWidth = position.width - TOGGLE_WIDTH - 4f;
                Rect labelRect = new Rect(position.x, position.y, labelWidth, EditorGUIUtility.singleLineHeight);

                EditorGUI.LabelField(labelRect, label);

                EditorGUI.BeginChangeCheck();
                bool storedPopupState = showPopupProp.boolValue;

                EditorGUI.PropertyField(toggleRect, showPopupProp, GUIContent.none);
                if(EditorGUI.EndChangeCheck())
                {
                    if(storedPopupState != showPopupProp.boolValue && !storedPopupState)
                    {
                        property.isExpanded = true;
                    }
                }
            }
            else
            {
                Rect foldoutRect = new Rect(position.x, position.y, position.width - TOGGLE_WIDTH, EditorGUIUtility.singleLineHeight);

                property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);
                EditorGUI.PropertyField(toggleRect, showPopupProp, GUIContent.none);

                if (property.isExpanded)
                {
                    float y = position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    float width = position.width - INDENT;
                    float x = position.x + INDENT;

                    foreach (string fieldName in SERIALIZED_FIELD_NAMES)
                    {
                        SerializedProperty fieldProp = property.FindPropertyRelative(fieldName);

                        if (fieldProp != null)
                        {
                            string displayName = ObjectNames.NicifyVariableName(fieldName);
                            EditorGUI.PropertyField(
                                new Rect(x, y, width, EditorGUIUtility.singleLineHeight),
                                fieldProp, new GUIContent(displayName));

                            y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                        }
                    }
                }
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty showPopupProp = property.FindPropertyRelative("showAnnouncementPopup");

            if (!showPopupProp.boolValue)
            {
                // Only one line for label and toggle
                return EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            }
            else
            {
                float height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing; // foldout line

                if (property.isExpanded)
                {
                    foreach (string fieldName in SERIALIZED_FIELD_NAMES)
                    {
                        SerializedProperty fieldProp = property.FindPropertyRelative(fieldName);
                        if (fieldProp != null)
                        {
                            height += EditorGUI.GetPropertyHeight(fieldProp, true) + EditorGUIUtility.standardVerticalSpacing;
                        }
                    }
                }

                return height;
            }
        }
    }
}
