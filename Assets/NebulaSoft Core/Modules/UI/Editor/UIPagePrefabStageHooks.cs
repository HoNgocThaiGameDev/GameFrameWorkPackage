using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NebulaSoft
{
    [InitializeOnLoad]
    public static class UIPagePrefabStageHooks
    {
        static UIPagePrefabStageHooks()
        {
            // Fire before Unity saves the open prefab in Prefab Mode
            PrefabStage.prefabSaving += OnPrefabStageSaving;
        }

        private static void OnPrefabStageSaving(GameObject root)
        {
            if (root == null) return;

            // If you have exactly one UIPage at the root:
            UIPage page = root.GetComponent<UIPage>();
            if (page == null) return;

            if (page.OnPrefabSaving())
            {
                EditorUtility.SetDirty(page);
                EditorSceneManager.MarkSceneDirty(root.scene);
            }
        }
    }
}
