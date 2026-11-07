using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEditor;
using UnityEngine;

namespace NebulaSoft
{
    [CustomPropertyDrawer(typeof(SimpleReward))]
    public class SimpleRewardDrawer : PropertyDrawer
    {
        static Type[] rewardTypes;
        static string[] rewardDisplay;
        const string NONE = "None";

        static SimpleRewardDrawer()
        {
            RefreshTypes();
        }

        static void RefreshTypes()
        {
            TypeCache.TypeCollection all = TypeCache.GetTypesDerivedFrom<Reward>();
            rewardTypes = all.Where(t => !t.IsAbstract && !t.IsInterface)
                             .OrderBy(t => t.Name)
                             .ToArray();

            List<string> names = new List<string> { NONE };
            names.AddRange(rewardTypes.Select(t => t.Name));

            rewardDisplay = names.ToArray();
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty rewardProp = property.FindPropertyRelative("reward");

            float line = EditorGUIUtility.singleLineHeight;
            float vpad = EditorGUIUtility.standardVerticalSpacing;
            float btnW = 60f;
            float lblW = EditorGUIUtility.labelWidth;

            // Header row: label, dropdown, reset button
            Rect headerRect = new Rect(position.x, position.y, position.width, line);

            // Layout: Label | Dropdown | Reset Button
            Rect labelRect = new Rect(headerRect.x, headerRect.y, lblW, line);

            float popupW = headerRect.width - lblW - btnW - 4f;
            Rect popupRect = new Rect(labelRect.xMax, headerRect.y, popupW, line);
            Rect btnRect = new Rect(popupRect.xMax + 4f, headerRect.y, btnW, line);

            int currentIndex = 0;
            Type currentType = null;

            if (rewardProp.managedReferenceValue != null)
            {
                currentType = rewardProp.managedReferenceValue.GetType();
                int idx = Array.IndexOf(rewardTypes, currentType);
                if (idx >= 0) currentIndex = idx + 1; // +1 because index 0 is None
            }

            EditorGUI.BeginChangeCheck();
            int newIndex = EditorGUI.Popup(popupRect, currentIndex, rewardDisplay);
            if (EditorGUI.EndChangeCheck())
            {
                TryChangeTypeWithConfirm(property, rewardProp, currentIndex, newIndex);
            }

            using (new EditorGUI.DisabledScope(rewardProp.managedReferenceValue == null))
            {
                if (GUI.Button(btnRect, "Reset"))
                {
                    if (EditorUtility.DisplayDialog("Reset reward", "This will clear the current Reward and its View. Continue?", "Yes", "No"))
                    {
                        ApplyToAllTargets(property, (obj) =>
                        {
                            var sp = new SerializedObject(obj).FindProperty(property.propertyPath);
                            var rw = sp.FindPropertyRelative("reward");
                            rw.managedReferenceValue = null;
                            sp.serializedObject.ApplyModifiedProperties();
                        });
                    }
                }
            }

            float y = headerRect.yMax + vpad;

            // Draw reward and view if reward exists
            if (rewardProp.managedReferenceValue != null)
            {
                EditorGUI.PropertyField(position, rewardProp, new GUIContent("Reward"), true);
            }
            else
            {
                EditorGUI.LabelField(labelRect, label);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty rewardProp = property.FindPropertyRelative("reward");

            float h = EditorGUIUtility.singleLineHeight; // header
            if (rewardProp.managedReferenceValue != null)
            {
                h += EditorGUIUtility.standardVerticalSpacing;
                h += EditorGUI.GetPropertyHeight(rewardProp, true);
            }
            return h;
        }

        void DrawHeader(Rect rect, GUIContent label, SerializedProperty containerProp, SerializedProperty rewardProp)
        {
        }

        void TryChangeTypeWithConfirm(SerializedProperty containerProp, SerializedProperty rewardProp, int currentIndex, int newIndex)
        {
            if (newIndex == currentIndex) return;

            bool needConfirm = rewardProp.managedReferenceValue != null && currentIndex != 0;
            if (needConfirm)
            {
                if (!EditorUtility.DisplayDialog("Change reward type",
                    "Changing the type will replace existing data for Reward and View. Continue?",
                    "Yes", "No"))
                {
                    return;
                }
            }

            Type targetType = (newIndex <= 0) ? null : rewardTypes[newIndex - 1];

            ApplyToAllTargets(containerProp, (obj) =>
            {
                var root = new SerializedObject(obj);
                var sp = root.FindProperty(containerProp.propertyPath);
                var rw = sp.FindPropertyRelative("reward");

                object newReward = CreateInstanceSafe(targetType);
                rw.managedReferenceValue = newReward;

                root.ApplyModifiedProperties();
            });
        }

        static object CreateInstanceSafe(Type t)
        {
            if (t == null) return null;
            try
            {
                return Activator.CreateInstance(t, true);
            }
            catch
            {
                try
                {
                    return FormatterServices.GetUninitializedObject(t);
                }
                catch
                {
                    Debug.LogWarning($"[SingleRewardDrawer] Can't create instance of {t.FullName}");
                    return null;
                }
            }
        }

        static void ApplyToAllTargets(SerializedProperty containerProp, Action<UnityEngine.Object> action)
        {
            var so = containerProp.serializedObject;
            Undo.RecordObjects(so.targetObjects, "Change SingleReward");
            foreach (var o in so.targetObjects)
            {
                action(o);
                EditorUtility.SetDirty(o);
            }
        }
    }
}