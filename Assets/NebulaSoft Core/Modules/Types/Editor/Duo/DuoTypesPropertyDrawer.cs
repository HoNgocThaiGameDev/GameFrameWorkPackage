using UnityEditor;
using UnityEngine;

namespace NebulaSoft
{
    [CustomPropertyDrawer(typeof(DuoInt)), CustomPropertyDrawer(typeof(DuoFloat)), CustomPropertyDrawer(typeof(DuoDouble))]
    public class DuoTypeDrawer : UnityEditor.PropertyDrawer
    {
        // Draw the property inside the given rect
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Using BeginProperty / EndProperty on the parent property means that
            // prefab override logic works on the entire property.
            EditorGUI.BeginProperty(position, label, property);

            float width = (position.width - EditorGUIUtility.labelWidth - 4) / 2;
            float offset = EditorGUIUtility.labelWidth + 2;

            EditorGUI.PrefixLabel(position, label);
            EditorGUI.PropertyField(new Rect(position.x + offset, position.y, width, position.height), property.FindPropertyRelative("firstValue"), GUIContent.none);
            EditorGUI.PropertyField(new Rect(position.x + offset + width + 2, position.y, width, position.height), property.FindPropertyRelative("secondValue"), GUIContent.none);

            EditorGUI.EndProperty();
        }
    }
}