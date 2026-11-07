using UnityEditor;
using System.Collections.Generic;

namespace NebulaSoft
{
    public abstract class EditorAdsContainer
    {
        protected SerializedProperty containerProperty;
        private IEnumerable<SerializedProperty> containerProperties;

        protected string containerName;
        protected string propertyName;

        public EditorAdsContainer(string containerName, string propertyName)
        {
            this.containerName = containerName;
            this.propertyName = propertyName;
        }

        public virtual void Init(SerializedObject serializedObject)
        {
            containerProperty = serializedObject.FindProperty(propertyName);
            containerProperties = containerProperty.GetChildren();
        }

        public virtual void DrawContainer()
        {
            containerProperty.isExpanded = EditorGUILayoutCustom.BeginExpandBoxGroup(containerName, containerProperty.isExpanded);

            if (containerProperty.isExpanded)
            {
                foreach (SerializedProperty prop in containerProperties)
                {
                    EditorGUILayout.PropertyField(prop);
                }

                SpecialButtons();
            }

            EditorGUILayoutCustom.EndBoxGroup();
        }

        protected abstract void SpecialButtons();
    }
}