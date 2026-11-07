using UnityEditor;
using UnityEngine;

namespace NebulaSoft
{
    [CustomPropertyDrawer(typeof(CurrencyAmount))]
    public class CurrencyAmountPropertyDrawer : UnityEditor.PropertyDrawer
    {
        private const int ColumnCount = 2;
        private const int GapSize = 6;
        private const int GapCount = ColumnCount - 1;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            float x = position.x;
            float y = position.y;
            float width = (position.width - EditorGUIUtility.labelWidth - GapCount * GapSize) / ColumnCount;
            float height = EditorGUIUtility.singleLineHeight;
            float offset = width + GapSize;

            SerializedProperty amountProperty = property.FindPropertyRelative("amount");

            EditorGUI.PrefixLabel(new Rect(x, y, position.width, position.height), new GUIContent(property.displayName));
            EditorGUI.PropertyField(new Rect(x + EditorGUIUtility.labelWidth + 2, y, width, height), property.FindPropertyRelative("currencyType"), GUIContent.none);
            EditorGUI.PropertyField(new Rect(x + EditorGUIUtility.labelWidth + offset, y, width, height), amountProperty, GUIContent.none);

            if (amountProperty.intValue < 0)
                amountProperty.intValue = 0;

            EditorGUI.EndProperty();
        }
    }
}
