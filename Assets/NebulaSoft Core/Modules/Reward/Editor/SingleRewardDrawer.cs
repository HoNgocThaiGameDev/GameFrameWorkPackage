using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEditor;
using UnityEngine;

namespace NebulaSoft
{
    [CustomPropertyDrawer(typeof(SingleReward))]
    public class SingleRewardDrawer : PropertyDrawer
    {
        static Type[] rewardTypes;
        static string[] rewardDisplay;
        const string NONE = "None";

        static SingleRewardDrawer()
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
            SerializedProperty viewProp = property.FindPropertyRelative("rewardView");

            float line = EditorGUIUtility.singleLineHeight;
            float vpad = EditorGUIUtility.standardVerticalSpacing;

            // Header row: label, dropdown, reset button
            Rect headerRect = new Rect(position.x, position.y, position.width, line);
            DrawHeader(headerRect, label, property, rewardProp, viewProp);

            float y = headerRect.yMax + vpad;

            // Draw reward and view if reward exists
            if (rewardProp.managedReferenceValue != null)
            {
                EditorGUI.indentLevel++;
                Rect r1 = new Rect(position.x, y, position.width, EditorGUI.GetPropertyHeight(rewardProp, true));
                EditorGUI.PropertyField(r1, rewardProp, new GUIContent("Reward"), true);
                y = r1.yMax + vpad;

                Rect r2 = new Rect(position.x, y, position.width, EditorGUI.GetPropertyHeight(viewProp, true));
                EditorGUI.PropertyField(r2, viewProp, new GUIContent("View"), true);
                EditorGUI.indentLevel--;
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty rewardProp = property.FindPropertyRelative("reward");
            SerializedProperty viewProp = property.FindPropertyRelative("rewardView");

            float h = EditorGUIUtility.singleLineHeight; // header
            if (rewardProp.managedReferenceValue != null)
            {
                h += EditorGUIUtility.standardVerticalSpacing;
                h += EditorGUI.GetPropertyHeight(rewardProp, true);
                h += EditorGUIUtility.standardVerticalSpacing;
                h += EditorGUI.GetPropertyHeight(viewProp, true);
            }
            return h;
        }

        void DrawHeader(Rect rect, GUIContent label, SerializedProperty containerProp, SerializedProperty rewardProp, SerializedProperty viewProp)
        {
            float line = EditorGUIUtility.singleLineHeight;
            float btnW = 60f;
            float lblW = EditorGUIUtility.labelWidth;

            // Layout: Label | Dropdown | Reset Button
            Rect labelRect = new Rect(rect.x, rect.y, lblW, line);
            EditorGUI.LabelField(labelRect, label);

            float popupW = rect.width - lblW - btnW - 4f;
            Rect popupRect = new Rect(labelRect.xMax, rect.y, popupW, line);
            Rect btnRect = new Rect(popupRect.xMax + 4f, rect.y, btnW, line);

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
                TryChangeTypeWithConfirm(containerProp, rewardProp, viewProp, currentIndex, newIndex);
            }

            using (new EditorGUI.DisabledScope(rewardProp.managedReferenceValue == null))
            {
                if (GUI.Button(btnRect, "Reset"))
                {
                    if (EditorUtility.DisplayDialog("Reset reward",
                        "This will clear the current Reward and its View. Continue?",
                        "Yes", "No"))
                    {
                        ApplyToAllTargets(containerProp, (obj) =>
                        {
                            var sp = new SerializedObject(obj).FindProperty(containerProp.propertyPath);
                            var rw = sp.FindPropertyRelative("reward");
                            var vw = sp.FindPropertyRelative("rewardView");
                            rw.managedReferenceValue = null;
                            vw.managedReferenceValue = null;
                            sp.serializedObject.ApplyModifiedProperties();
                        });
                    }
                }
            }
        }

        void TryChangeTypeWithConfirm(SerializedProperty containerProp, SerializedProperty rewardProp, SerializedProperty viewProp, int currentIndex, int newIndex)
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
                var vw = sp.FindPropertyRelative("rewardView");

                object newReward = CreateInstanceSafe(targetType);
                rw.managedReferenceValue = newReward;

                if (targetType != null)
                {
                    var viewType = RewardsUtils.GetViewTypeFor(targetType);
                    vw.managedReferenceValue = CreateInstanceSafe(viewType);
                }
                else
                {
                    vw.managedReferenceValue = null;
                }

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