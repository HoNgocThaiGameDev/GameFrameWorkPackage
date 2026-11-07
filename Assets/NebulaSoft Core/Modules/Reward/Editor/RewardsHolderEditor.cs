using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace NebulaSoft
{
    [CustomEditor(typeof(RewardsHolder), true)]
    public class RewardsHolderEditor : Editor
    {
        private SerializedProperty scriptsProperty;

        // Group helpers (kept from your project)
        private IEnumerable<SerializedProperty> properties;
        private IEnumerable<SerializedProperty> eventsProperties;
        private IEnumerable<SerializedProperty> ungroupedProperties;
        private List<Type> nestedTypes;

        // Core fields
        protected SerializedProperty rewardSetProp;   // exposed and relinkable
        protected SerializedProperty rewardsViewProp; // [SerializeReference] List<RewardView>

        private static List<Type> availableRewardTypes; // all concrete Reward types
        private GUIContent addButton;
        private GUIContent syncButton;
        private GUIStyle removeButtonStyle;

        // Validation cache (indices of invalid views)
        private List<int> invalidViewIndices = new List<int>();
        private bool showInvalidWarning;

        [InitializeOnLoadMethod]
        private static void InitializeTypes()
        {
            availableRewardTypes = new List<Type>();

            // Collect all non-abstract Reward subclasses across domain
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly == null) continue;

                Type[] types;
                try { types = assembly.GetTypes(); }
                catch (ReflectionTypeLoadException e) { types = e.Types.Where(t => t != null).ToArray(); }

                foreach (Type t in types)
                {
                    if (t != null && t.IsClass && !t.IsAbstract && typeof(Reward).IsAssignableFrom(t))
                        availableRewardTypes.Add(t);
                }
            }

            availableRewardTypes = availableRewardTypes.OrderBy(t => t.Name).ToList();
        }

        protected virtual void OnEnable()
        {
            nestedTypes = PropertyUtility.GetClassNestedTypes(target.GetType());

            addButton = new GUIContent("", EditorCustomStyles.GetIcon("icon_add"));
            syncButton = new GUIContent("", EditorCustomStyles.GetIcon("icon_reset"));

            removeButtonStyle = new GUIStyle(EditorCustomStyles.buttonRed)
            {
                padding = new RectOffset(0, 0, 0, 0),
                fontSize = 9,
                fontStyle = FontStyle.Bold
            };

            scriptsProperty = serializedObject.FindProperty("m_Script");
            rewardSetProp = serializedObject.FindProperty("rewardSet");
            rewardsViewProp = serializedObject.FindProperty("rewardsView");

            properties = serializedObject.GetPropertiesByGroup(nestedTypes, "Settings");
            eventsProperties = serializedObject.GetPropertiesByGroup(nestedTypes, "Events");
            ungroupedProperties = serializedObject.GetUngroupProperties();

            RevalidateViewsAgainstRewardSet();

            Undo.undoRedoPerformed += OnUndoRedoPerformed;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
        }

        private void OnUndoRedoPerformed()
        {
            serializedObject.Update();

            RevalidateViewsAgainstRewardSet();
            Repaint();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (scriptsProperty != null)
            {
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.PropertyField(scriptsProperty);
            }

            EditorGUI.BeginChangeCheck();
            foreach (SerializedProperty property in properties)
                EditorGUILayout.PropertyField(property);
            if (EditorGUI.EndChangeCheck())
                OnSettingsPropertyChanges();

            EditorGUILayout.Space(4);

            // Main box: Reward Set + Views list + Add panel
            Rect boxRect = EditorGUILayout.BeginVertical(EditorCustomStyles.box);
            EditorGUILayout.LabelField("Rewards", EditorCustomStyles.labelBold);

            object rewardSetValue = rewardSetProp.objectReferenceValue;

            // Reward Set field INSIDE the box
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            using (new EditorGUI.DisabledScope(DisableRewardsSetProperty()))
            {
                EditorGUILayout.PropertyField(rewardSetProp, new GUIContent("Reward Set"));
            }
            bool rewardSetChanged = EditorGUI.EndChangeCheck();
            if (rewardSetChanged)
            {
                serializedObject.ApplyModifiedProperties();
                serializedObject.Update();

                RevalidateViewsAgainstRewardSet();
            }

            bool hasRewardSet = rewardSetProp != null && rewardSetProp.objectReferenceValue != null;

            if (rewardSetProp != null && rewardSetProp.objectReferenceValue != null && rewardsViewProp != null && rewardsViewProp.arraySize == 0)
            {
                if (GUILayout.Button(syncButton, EditorCustomStyles.button, GUILayout.Width(20), GUILayout.Height(20)))
                {
                    AutoPopulateFromRewardSet();
                    RevalidateViewsAgainstRewardSet();

                    return;
                }
            }

            EditorGUILayout.EndHorizontal();

            if (rewardSetProp == null || rewardSetProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Assign a RewardSet to enable adding views.", MessageType.Warning);
            }

            if (showInvalidWarning && invalidViewIndices.Count > 0)
            {
                string msg = "Some views do not correspond to any Reward present in the assigned RewardSet:\n";
                for (int i = 0; i < invalidViewIndices.Count; i++)
                {
                    int idx = invalidViewIndices[i];
                    string tName = GetElementShortTypeName(rewardsViewProp, idx);
                    msg += $"- [{idx}] {tName}\n";
                }
                EditorGUILayout.HelpBox(msg.TrimEnd(), MessageType.Warning);
            }

            // Draw items
            if (rewardsViewProp != null)
            {
                int size = rewardsViewProp.isArray ? rewardsViewProp.arraySize : 0;

                if (size > 0)
                {
                    for (int i = 0; i < size; i++)
                    {
                        SerializedProperty element = rewardsViewProp.GetArrayElementAtIndex(i);

                        using (new EditorGUILayout.VerticalScope(EditorCustomStyles.box))
                        {
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                string typeName = GetNiceTypeName(element.managedReferenceFullTypename);
                                string title = $"{typeName}";

                                element.isExpanded = EditorGUILayout.Foldout(element.isExpanded, new GUIContent(title), true);

                                // Remove (confirm + Undo)
                                using (new EditorGUI.DisabledScope(rewardsViewProp.arraySize <= 0))
                                {
                                    if (GUILayout.Button("X", removeButtonStyle, GUILayout.Width(14), GUILayout.Height(14)))
                                    {
                                        bool confirm = EditorUtility.DisplayDialog(
                                            "Remove View",
                                            $"Are you sure you want to remove view [{i}] {typeName}?",
                                            "Remove", "Cancel");

                                        if (confirm)
                                        {
                                            Undo.RecordObject(target, "Remove Reward View");
                                            rewardsViewProp.DeleteArrayElementAtIndex(i);
                                            serializedObject.ApplyModifiedProperties();
                                            serializedObject.Update();

                                            RevalidateViewsAgainstRewardSet();
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
                    using (new EditorGUILayout.VerticalScope(EditorCustomStyles.box))
                    {
                        EditorGUILayout.LabelField("List is empty.");
                    }
                }
            }

            EditorGUILayout.EndVertical();

            // Bottom-right buttons panel: render ONLY if RewardSet is assigned
            if (rewardSetProp != null && rewardSetProp.objectReferenceValue != null)
            {
                GUILayout.Space(20);
                Rect buttonsPanelRect = new Rect(boxRect.x + boxRect.width - 35, boxRect.y + boxRect.height - 1, 24, 16);
                Rect addButtonRect = new Rect(buttonsPanelRect.x + 5, buttonsPanelRect.y, 14, 14);

                GUI.Box(buttonsPanelRect, "", EditorCustomStyles.boxBottomPanel);
                GUI.Label(addButtonRect, addButton, EditorCustomStyles.labelCentered);

                // If RewardSet is null, do not draw "+" at all (already hidden by hasRewardSet)
                if (GUI.Button(buttonsPanelRect, GUIContent.none, GUIStyle.none))
                {
                    ShowAddMenu();
                    GUIUtility.ExitGUI();
                    return;
                }
            }

            foreach (var property in ungroupedProperties)
                EditorGUILayout.PropertyField(property);

            foreach (var property in eventsProperties)
                EditorGUILayout.PropertyField(property);

            serializedObject.ApplyModifiedProperties();
        }

        private static void DrawManagedReferenceContents(SerializedProperty element)
        {
            if (element == null) return;

            // Make a working copy and compute the end marker for this subtree
            var copy = element.Copy();
            var end = copy.GetEndProperty();

            bool enterChildren = true;

            // Move to first child and iterate until we reach the end
            while (copy.NextVisible(enterChildren) && !SerializedProperty.EqualContents(copy, end))
            {
                // Skip Unity's internal script field if it ever appears
                if (copy.name == "m_Script")
                {
                    enterChildren = false;
                    continue;
                }

                // Draw the child field; includeChildren will handle nested structs/classes nicely
                EditorGUILayout.PropertyField(copy, includeChildren: true);

                // After the first NextVisible with enterChildren = true,
                // we should set it to false to step sibling-by-sibling.
                enterChildren = false;
            }
        }

        private void ShowAddMenu()
        {
            var menu = new GenericMenu();

            RewardsSet rewardSet = rewardSetProp.objectReferenceValue as RewardsSet;
            HashSet<Type> presentTypes = new HashSet<Type>();
            Dictionary<Type, Reward> rewardsLink = new Dictionary<Type, Reward>();

            if (rewardSet != null && rewardSet.Rewards != null)
            {
                foreach (Reward r in rewardSet.Rewards)
                {
                    if (r == null) continue;

                    Type t = r.GetType();

                    presentTypes.Add(t);

                    rewardsLink.TryAdd(t, r);
                }
            }

            // collect view types already in list
            HashSet<Type> existingViewTypes = new HashSet<Type>();
            if (rewardsViewProp != null && rewardsViewProp.isArray)
            {
                for (int i = 0; i < rewardsViewProp.arraySize; i++)
                {
                    var element = rewardsViewProp.GetArrayElementAtIndex(i);
                    var viewType = GetManagedReferenceSystemType(element);
                    if (viewType != null)
                        existingViewTypes.Add(viewType);
                }
            }

            foreach (Type rewardType in availableRewardTypes)
            {
                string label = FormatTypeName(rewardType);

                var viewType = RewardsUtils.GetViewTypeFor(rewardType);
                bool hasView = viewType != null;
                bool inSet = presentTypes.Contains(rewardType);

                if (!inSet)
                {
                    menu.AddDisabledItem(new GUIContent(label));
                }
                else if (!hasView)
                {
                    menu.AddDisabledItem(new GUIContent($"{label}  (No View Registered)"));
                }
                else if (existingViewTypes.Contains(viewType))
                {
                    menu.AddDisabledItem(new GUIContent($"{label}  (Already Added)")); // disable duplicates
                }
                else
                {
                    Reward reward = null;
                    if(rewardsLink.TryGetValue(rewardType, out reward))
                    {
                        menu.AddItem(new GUIContent(label), false, () => AddViewInstance(viewType, reward));
                    }
                    else
                    {
                        menu.AddDisabledItem(new GUIContent($"{label}  (Reward is missing)"));
                    }
                }
            }

            menu.ShowAsContext();
        }

        private void AddViewInstance(Type viewType, Reward reward)
        {
            if (rewardsViewProp == null || viewType == null)
                return;

            serializedObject.Update();

            Undo.RecordObject(target, "Add Reward View");

            int newIndex = rewardsViewProp.arraySize;
            rewardsViewProp.InsertArrayElementAtIndex(newIndex);
            SerializedProperty el = rewardsViewProp.GetArrayElementAtIndex(newIndex);

            object instance = null;
            try
            {
                instance = Activator.CreateInstance(viewType);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to create view of type {viewType.FullName}: {e.Message}");
            }

            RewardView rv = (RewardView)instance;
            rv.Fill(reward);

            el.managedReferenceValue = instance;
            el.isExpanded = true;

            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();

            RevalidateViewsAgainstRewardSet();

            EditorUtility.SetDirty(target);
        }

        protected void AutoPopulateFromRewardSet()
        {
            var rewardSet = rewardSetProp?.objectReferenceValue as RewardsSet;
            if (rewardSet == null || rewardsViewProp == null || !rewardsViewProp.isArray)
                return;

            if (rewardsViewProp.arraySize > 0)
                return;

            var rewards = rewardSet.Rewards;
            if (rewards == null || rewards.Count == 0)
                return;

            serializedObject.Update();
            Undo.RecordObject(target, "Auto Populate Reward Views");

            // Create one view per Reward entry in the set (1:1)
            for (int i = 0; i < rewards.Count; i++)
            {
                Reward reward = rewards[i];
                if (reward == null) continue;

                var viewType = RewardsUtils.GetViewTypeFor(reward.GetType());
                if (viewType == null) continue; // skip if no mapping

                int newIndex = rewardsViewProp.arraySize;
                rewardsViewProp.InsertArrayElementAtIndex(newIndex);
                var el = rewardsViewProp.GetArrayElementAtIndex(newIndex);

                object instance = null;
                try { instance = Activator.CreateInstance(viewType); }
                catch (Exception e) { Debug.LogError($"Failed to create view of type {viewType.FullName}: {e.Message}"); }

                RewardView rv = (RewardView)instance;
                rv.Fill(reward);

                el.managedReferenceValue = instance;
                el.isExpanded = true;
            }

            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();

            RevalidateViewsAgainstRewardSet();

            EditorUtility.SetDirty(target);
        }

        protected void RevalidateViewsAgainstRewardSet()
        {
            invalidViewIndices.Clear();
            showInvalidWarning = false;

            var rewardSet = rewardSetProp != null ? rewardSetProp.objectReferenceValue as RewardsSet : null;

            if (rewardsViewProp == null || !rewardsViewProp.isArray)
                return;

            // Build set of Reward types present in RewardSet
            HashSet<Type> presentRewardTypes = new HashSet<Type>();
            if (rewardSet != null && rewardSet.Rewards != null)
            {
                foreach (var r in rewardSet.Rewards)
                {
                    if (r == null) continue;
                    presentRewardTypes.Add(r.GetType());
                }
            }

            int size = rewardsViewProp.arraySize;
            for (int i = 0; i < size; i++)
            {
                var element = rewardsViewProp.GetArrayElementAtIndex(i);

                // Robustly resolve the CLR Type from managedReferenceFullTypename
                var viewType = GetManagedReferenceSystemType(element);

                // Reverse mapping: which Reward type is registered with this View
                var rewardType = GetRewardTypeForView(viewType);

                if (rewardType == null || !presentRewardTypes.Contains(rewardType))
                {
                    invalidViewIndices.Add(i);
                }
            }

            showInvalidWarning = (rewardSet != null && invalidViewIndices.Count > 0);
        }

        // Robust type resolution for managed reference values
        private static Type GetManagedReferenceSystemType(SerializedProperty prop)
        {
            if (prop == null) return null;
            string full = prop.managedReferenceFullTypename; // "AssemblyName TypeFullName"
            if (string.IsNullOrEmpty(full)) return null;

            int space = full.IndexOf(' ');
            if (space < 0 || space + 1 >= full.Length) return null;

            string asmName = full.Substring(0, space);
            string typeName = full.Substring(space + 1);

            // 1) Try the assembly stated by Unity
            var asm = AppDomain.CurrentDomain
                               .GetAssemblies()
                               .FirstOrDefault(a => a.GetName().Name == asmName);
            if (asm != null)
            {
                var t = asm.GetType(typeName);
                if (t != null) return t;
            }

            // 2) Fallback: scan all loaded assemblies
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = a.GetType(typeName);
                if (t != null) return t;
            }

            // 3) Last attempt: assembly-qualified string
            return Type.GetType($"{typeName}, {asmName}");
        }

        private static Type GetRewardTypeForView(Type viewType)
        {
            if (viewType == null) return null;

            foreach (var rType in availableRewardTypes)
            {
                var vType = RewardsUtils.GetViewTypeFor(rType);
                if (vType == viewType)
                    return rType;
            }
            return null;
        }

        private static string GetNiceTypeName(string managedReferenceFullTypename)
        {
            if (string.IsNullOrEmpty(managedReferenceFullTypename)) return "(null)";

            int space = managedReferenceFullTypename.IndexOf(' ');
            if (space >= 0 && space + 1 < managedReferenceFullTypename.Length)
            {
                var full = managedReferenceFullTypename.Substring(space + 1);
                int lastDot = full.LastIndexOf('.');
                return lastDot >= 0 ? full.Substring(lastDot + 1).AddSpaces() : full;
            }
            return managedReferenceFullTypename;
        }

        private string GetElementShortTypeName(SerializedProperty arrayProp, int index)
        {
            if (arrayProp == null || !arrayProp.isArray || index < 0 || index >= arrayProp.arraySize)
                return "(null)";

            var el = arrayProp.GetArrayElementAtIndex(index);
            return GetNiceTypeName(el.managedReferenceFullTypename);
        }

        private string FormatTypeName(Type type) => type.Name.AddSpaces();

        protected virtual bool DisableRewardsSetProperty() => false;
        protected virtual void OnSettingsPropertyChanges() { }
    }
}
