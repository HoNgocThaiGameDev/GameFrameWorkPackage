using UnityEditor;

namespace NebulaSoft
{
    [CustomEditor(typeof(DevPanelSettings))]
    public class DevPanelSettingsEditor : CustomInspector
    {
        private SerializedProperty enableProperty;
        private bool currentState;

        protected override void OnEnable()
        {
            base.OnEnable();

            enableProperty = serializedObject.FindProperty("isEnabled");
        }

        public override void OnInspectorGUI()
        {
            currentState = enableProperty.boolValue;

            base.OnInspectorGUI();

            if (currentState != enableProperty.boolValue)
            {
                DevPanelEditor.UpdateDevPanelState();
            }
        }
    }
}
