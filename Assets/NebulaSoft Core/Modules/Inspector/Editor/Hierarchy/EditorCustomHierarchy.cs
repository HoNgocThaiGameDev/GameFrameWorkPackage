using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

#if UNITY_6000_6_OR_NEWER
using HierarchyObjectId = UnityEngine.EntityId;
#else
using HierarchyObjectId = System.Int32;
#endif

namespace NebulaSoft
{
    [InitializeOnLoad]
    public static class EditorCustomHierarchy
    {
        public static Dictionary<int, Texture> hierarchyTextures;

        private static EditorData editorData;

        private static int dataLoadAttempt = 0;

        static EditorCustomHierarchy()
        {
            LoadEditorData();
        }

        private static void LoadEditorData()
        {
            if (editorData != null)
            {
                return;
            }

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += LoadEditorData;

                return;
            }

            editorData = EditorUtils.GetAsset<EditorData>();

            if (editorData != null)
            {
                hierarchyTextures = new Dictionary<int, Texture>();

                EditorData.HierarchyItem[] hierarchyIcons = editorData.HierarchyIcons;
                foreach (EditorData.HierarchyItem hierarchyIcon in hierarchyIcons)
                {
                    int hash = hierarchyIcon.Name.GetHashCode();
                    if (!hierarchyTextures.ContainsKey(hash))
                    {
                        hierarchyTextures.Add(hash, hierarchyIcon.Texture);
                    }
                }

#if UNITY_6000_6_OR_NEWER
                EditorApplication.hierarchyWindowItemByEntityIdOnGUI -= HighlightItems;
                EditorApplication.hierarchyWindowItemByEntityIdOnGUI += HighlightItems;
#else
                EditorApplication.hierarchyWindowItemOnGUI -= HighlightItems;
                EditorApplication.hierarchyWindowItemOnGUI += HighlightItems;
#endif
                
                PrefabStage.prefabStageClosing += OnPrefabStageClosing;
            }
            else
            {
                dataLoadAttempt++;

                if (dataLoadAttempt <= 10)
                {
                    EditorApplication.delayCall += LoadEditorData;
                }
            }
        }

        private static void OnPrefabStageClosing(PrefabStage stage)
        {
            Hierarchy.ClearHierarchies();
        }

        private static void HighlightItems(HierarchyObjectId instanceID, Rect selectionRect)
        {
            if (!CoreEditor.UseHierarchyIcons)
            {
                Hierarchy.ClearHierarchies();

                return;
            }

            Hierarchy hierarchy = Hierarchy.GetLastHierarchy();
            hierarchy.DrawElementGUI(instanceID, selectionRect);
        }

        public static Texture GetTexture(UnityEngine.Object hierarchyObject)
        {
            int instanceID = hierarchyObject.name.GetHashCode();

            if (hierarchyTextures.ContainsKey(instanceID))
            {
                return hierarchyTextures[instanceID];
            }

            return AssetPreview.GetMiniThumbnail(hierarchyObject);
        }
    }
}
