using UnityEditor;
using UnityEngine;

namespace NebulaSoft
{
    public class PagePrefabPostprocessor : AssetPostprocessor
    {
        private static bool busy;

        private static void OnPostprocessPrefab(GameObject prefab)
        {
            if (busy) return;

            try
            {
                busy = true;

                if (prefab == null) return;

                UIPage page = prefab.GetComponent<UIPage>();
                if (page == null) return;

                page.OnPrefabSaving();
            }
            finally
            {
                busy = false;
            }
        }
    }
}
