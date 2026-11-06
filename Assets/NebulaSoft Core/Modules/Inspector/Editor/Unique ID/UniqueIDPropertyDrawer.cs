using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NebulaSoft
{

    [CustomPropertyDrawer(typeof(UniqueIDAttribute), true)]
    public class UniqueIDPropertyDrawer : UnityEditor.PropertyDrawer
    {
        private const string PREFAB_LABEL = "*prefab*";

        private SerializedObject storedSerializedObject;
        private bool lockState;

        private GUIContent copyIconContent;
        private GUIContent lockedIconContent;
        private GUIContent unlockedIconContent;
        private GUIContent resetIconContent;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if(property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.HelpBox(position, "Unsupported property type!", MessageType.Error);

                return;
            }

            if (storedSerializedObject != property.serializedObject)
            {
                lockState = false;

                storedSerializedObject = property.serializedObject;

                copyIconContent = new GUIContent(EditorCustomStyles.GetIcon("icon_copy"), "Copy to clipboard");
                lockedIconContent = new GUIContent(EditorCustomStyles.GetIcon("icon_locked"), "Allow editing");
                unlockedIconContent = new GUIContent(EditorCustomStyles.GetIcon("icon_unlocked"), "Block editing");
                resetIconContent = new GUIContent(EditorCustomStyles.GetIcon("icon_reset"), "Reset ID");
            }

            EditorGUI.BeginProperty(position, label, property);

            if (IsPrefab(property.serializedObject.targetObject))
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUI.TextField(position, label, PREFAB_LABEL);
                }

                property.stringValue = "";
            }
            else if (Application.isPlaying)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUI.PropertyField(position, property);
                }
            }
            else
            {
                string idValue = property.stringValue;

                if (string.IsNullOrEmpty(idValue))
                {
                    idValue = Regenerate(property);
                }

                UniqueIDHandler.IDCase idCase = UniqueIDHandler.GetCase(idValue);
#if UNITY_6000_6_OR_NEWER
                var instanceID = property.serializedObject.targetObject.GetEntityId();
#else
                int instanceID = property.serializedObject.targetObject.GetInstanceID();
#endif

                if (idCase != null)
                {
                    if (idCase.RequireReset(property.propertyPath, instanceID))
                    {
                        idValue = Regenerate(property);

                        UniqueIDHandler.RegisterID(new UniqueIDHandler.IDCase(property.propertyPath, instanceID, idValue));
                    }
                }
                else
                {
                    UniqueIDHandler.RegisterID(new UniqueIDHandler.IDCase(property.propertyPath, instanceID, idValue));
                }

                Rect fieldRect = new Rect(position.x, position.y, position.width - 62, position.height);

                using (new EditorGUI.DisabledScope(disabled: !lockState))
                {
                    EditorGUI.PropertyField(fieldRect, property);
                }

                Rect buttonToggleRect = new Rect(fieldRect.x + fieldRect.width + 4, position.y, 18, 18);
                if (GUI.Button(buttonToggleRect, lockState ? unlockedIconContent : lockedIconContent))
                {
                    lockState = !lockState;
                }

                Rect buttonResetRect = new Rect(fieldRect.x + fieldRect.width + 24, position.y, 18, 18);
                if (GUI.Button(buttonResetRect, resetIconContent))
                {
                    if (EditorUtility.DisplayDialog("Reset ID?", "Are you sure you want to reset ID?", "Reset", "Cancel"))
                    {
                        Undo.RecordObject(property.serializedObject.targetObject, "ID regenerated");

                        idValue = Regenerate(property);

                        UniqueIDHandler.RegisterID(new UniqueIDHandler.IDCase(property.propertyPath, instanceID, idValue));
                    }
                }

                Rect buttonCopyRect = new Rect(fieldRect.x + fieldRect.width + 44, position.y, 18, 18);
                if (GUI.Button(buttonCopyRect, copyIconContent))
                {
                    GUIUtility.systemCopyBuffer = property.stringValue;
                }
            }

            EditorGUI.EndProperty();
        }

        private string Regenerate(SerializedProperty serializedProperty)
        {
            serializedProperty.serializedObject.Update();

            string idValue = UniqueIDUtils.GetUniqueID();

            serializedProperty.stringValue = idValue;

            serializedProperty.serializedObject.ApplyModifiedProperties();

            EditorUtility.SetDirty(serializedProperty.serializedObject.targetObject);

            return idValue;
        }

        private bool IsPrefab(Object target)
        {
            Component component = target as Component;
            if (component != null)
            {
                if (component.gameObject.scene.name == null || EditorSceneManager.IsPreviewScene(component.gameObject.scene))
                    return true;
            }

            return false;
        }
    }
}
