// RewardSetEditor.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace NebulaSoft
{
    [CustomEditor(typeof(RewardsSet))]
    public class RewardSetEditor : Editor
    {
        private SerializedProperty scriptProp;
        private SerializedProperty rewardsProp;
        private SerializedProperty notesProp;

        // Grouping support (same helpers as in your other editors)
        private IEnumerable<SerializedProperty> systemGroupProps;   // props in [Group("System")] except rewards
        private IEnumerable<SerializedProperty> ungroupedProps;     // props without any group
        private List<Type> nestedTypes;

        private static List<Type> availableRewardTypes; // all concrete Reward subclasses

        private GUIContent addIcon;
        private GUIStyle removeButtonStyle;

        private RewardsSet targetRewardSet;

        [InitializeOnLoadMethod]
        private static void InitializeTypes()
        {
            availableRewardTypes = new List<Type>();

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm == null) continue;

                Type[] types;
                try { types = asm.GetTypes(); }
                catch (ReflectionTypeLoadException e) { types = e.Types.Where(t => t != null).ToArray(); }

                foreach (var t in types)
                {
                    if (t != null && t.IsClass && !t.IsAbstract && typeof(Reward).IsAssignableFrom(t))
                        availableRewardTypes.Add(t);
                }
            }

            availableRewardTypes = availableRewardTypes.OrderBy(t => t.Name).ToList();
        }

        private void OnEnable()
        {
            targetRewardSet = (RewardsSet)target;

            scriptProp = serializedObject.FindProperty("m_Script");
            rewardsProp = serializedObject.FindProperty("rewards");
            notesProp = serializedObject.FindProperty("notes");

            // Group discovery
            nestedTypes = PropertyUtility.GetClassNestedTypes(target.GetType());

            ungroupedProps = serializedObject.GetUngroupProperties();

            addIcon = new GUIContent("", EditorCustomStyles.GetIcon("icon_add"));

            removeButtonStyle = new GUIStyle(EditorCustomStyles.buttonRed)
            {
                padding = new RectOffset(0, 0, 0, 0),
                fontSize = 9,
                fontStyle = FontStyle.Bold
            };

            Undo.undoRedoPerformed += OnUndoRedoPerformed;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
        }

        private void OnUndoRedoPerformed()
        {
            serializedObject.Update();

            Repaint();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // m_Script (readonly)
            if (scriptProp != null)
            {
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.PropertyField(scriptProp);
            }

            EditorGUILayout.Space(4);

            // --- SYSTEM GROUP BOX (Rewards + any other System props) ---
            Rect boxRect = EditorGUILayout.BeginVertical(EditorCustomStyles.box);
            EditorGUILayout.LabelField("Rewards", EditorCustomStyles.labelBold);

            // Draw any other [Group("System")] properties here (except rewards)
            if (systemGroupProps != null)
            {
                foreach (var gp in systemGroupProps)
                    EditorGUILayout.PropertyField(gp, includeChildren: true);
            }

            // Rewards array block (style like RewardsHolderEditor)
            if (rewardsProp != null)
            {
                int size = rewardsProp.isArray ? rewardsProp.arraySize : 0;

                if (size > 0)
                {
                    for (int i = 0; i < size; i++)
                    {
                        var element = rewardsProp.GetArrayElementAtIndex(i);

                        using (new EditorGUILayout.VerticalScope(EditorCustomStyles.box))
                        {
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                string typeName = GetNiceTypeName(element.managedReferenceFullTypename);

                                element.isExpanded = EditorGUILayout.Foldout(element.isExpanded, new GUIContent(typeName), true);

                                using (new EditorGUI.DisabledScope(rewardsProp.arraySize <= 0))
                                {
                                    if (GUILayout.Button("X", removeButtonStyle, GUILayout.Width(14), GUILayout.Height(14)))
                                    {
                                        bool confirm = EditorUtility.DisplayDialog(
                                            "Remove Reward",
                                            $"Are you sure you want to remove [{i}] {typeName}?",
                                            "Remove", "Cancel");

                                        if (confirm)
                                        {
                                            Undo.RecordObject(target, "Remove Reward");
                                            rewardsProp.DeleteArrayElementAtIndex(i);

                                            serializedObject.ApplyModifiedProperties();
                                            serializedObject.Update();
                                            GUIUtility.ExitGUI();
                                            return;
                                        }
                                    }
                                }
                            }

                            if (element.isExpanded)
                            {
                                EditorGUI.indentLevel++;
                                DrawManagedReferenceContents(element);
                                EditorGUI.indentLevel--;
                            }
                        }
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("List is empty.");
                }
            }

            EditorGUILayout.EndVertical();

            // --- ADD BUTTON (bottom-right of box) ---
            GUILayout.Space(20);
            Rect buttonsPanelRect = new Rect(boxRect.x + boxRect.width - 35, boxRect.y + boxRect.height - 1, 24, 16);
            Rect addButtonRect = new Rect(buttonsPanelRect.x + 5, buttonsPanelRect.y, 14, 14);

            GUI.Box(buttonsPanelRect, "", EditorCustomStyles.boxBottomPanel);
            GUI.Label(addButtonRect, addIcon, EditorCustomStyles.labelCentered);

            if (GUI.Button(buttonsPanelRect, GUIContent.none, GUIStyle.none))
            {
                ShowAddMenu();
                GUIUtility.ExitGUI();
                return;
            }

            if (ungroupedProps != null)
            {
                foreach (SerializedProperty prop in ungroupedProps)
                {
                    EditorGUILayout.PropertyField(prop, includeChildren: true);
                }
            }

            EditorGUILayout.PrefixLabel("Notes");

            GUIStyle style = EditorStyles.textArea;

            float height = Mathf.Clamp(style.CalcHeight(new GUIContent(notesProp.stringValue), EditorGUIUtility.currentViewWidth), 60, float.MaxValue);

            notesProp.stringValue = EditorGUILayout.TextArea(notesProp.stringValue, style, GUILayout.Height(height));

            serializedObject.ApplyModifiedProperties();
        }

        // Build GenericMenu with duplicate blocking
        private void ShowAddMenu()
        {
            var menu = new GenericMenu();

            // Collect already present Reward TYPES to block duplicates
            var existingRewardTypes = new HashSet<Type>();
            if (rewardsProp != null && rewardsProp.isArray)
            {
                for (int i = 0; i < rewardsProp.arraySize; i++)
                {
                    var el = rewardsProp.GetArrayElementAtIndex(i);
                    var t = GetManagedReferenceSystemType(el);
                    if (t != null) existingRewardTypes.Add(t);
                }
            }

            foreach (var rewardType in availableRewardTypes)
            {
                string label = rewardType.Name.AddSpaces();

                if (existingRewardTypes.Contains(rewardType))
                {
                    menu.AddDisabledItem(new GUIContent($"{label}  (Already Added)"));
                }
                else
                {
                    menu.AddItem(new GUIContent(label), false, () => AddRewardInstance(rewardType));
                }
            }

            menu.ShowAsContext();
        }

        private void AddRewardInstance(Type rewardType)
        {
            if (rewardsProp == null || rewardType == null)
                return;

            serializedObject.Update();

            Undo.RecordObject(target, "Add Reward");

            int newIndex = rewardsProp.arraySize;
            rewardsProp.InsertArrayElementAtIndex(newIndex);
            var el = rewardsProp.GetArrayElementAtIndex(newIndex);

            object instance = null;
            try
            {
                instance = Activator.CreateInstance(rewardType);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to create reward of type {rewardType.FullName}: {e.Message}");
            }

            el.managedReferenceValue = instance;
            el.isExpanded = true;

            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();

            EditorUtility.SetDirty(target);
        }

        // Render children of a managed reference without the root foldout
        private static void DrawManagedReferenceContents(SerializedProperty element)
        {
            if (element == null) return;

            var copy = element.Copy();
            var end = copy.GetEndProperty();

            bool enterChildren = true;

            while (copy.NextVisible(enterChildren) && !SerializedProperty.EqualContents(copy, end))
            {
                if (copy.name == "m_Script")
                {
                    enterChildren = false;
                    continue;
                }

                EditorGUILayout.PropertyField(copy, includeChildren: true);
                enterChildren = false;
            }
        }

        // Resolve CLR type from "AssemblyName TypeFullName"
        private static Type GetManagedReferenceSystemType(SerializedProperty prop)
        {
            if (prop == null) return null;
            string full = prop.managedReferenceFullTypename;
            if (string.IsNullOrEmpty(full)) return null;

            int space = full.IndexOf(' ');
            if (space < 0 || space + 1 >= full.Length) return null;

            string asmName = full.Substring(0, space);
            string typeName = full.Substring(space + 1);

            var asm = AppDomain.CurrentDomain
                               .GetAssemblies()
                               .FirstOrDefault(a => a.GetName().Name == asmName);
            if (asm != null)
            {
                var t = asm.GetType(typeName);
                if (t != null) return t;
            }

            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = a.GetType(typeName);
                if (t != null) return t;
            }

            return Type.GetType($"{typeName}, {asmName}");
        }

        private static string GetNiceTypeName(string managedReferenceFullTypename)
        {
            if (string.IsNullOrEmpty(managedReferenceFullTypename)) return "(null)";

            int space = fullTypenameSpaceIndex(managedReferenceFullTypename);
            if (space >= 0 && space + 1 < managedReferenceFullTypename.Length)
            {
                var full = managedReferenceFullTypename.Substring(space + 1);
                int lastDot = full.LastIndexOf('.');
                return lastDot >= 0 ? full.Substring(lastDot + 1).AddSpaces() : full;
            }
            return managedReferenceFullTypename;

            static int fullTypenameSpaceIndex(string s) => s.IndexOf(' ');
        }
    }
}
