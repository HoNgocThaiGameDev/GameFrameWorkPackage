using UnityEditor;
using UnityEngine;

namespace NebulaSoft
{
    public class DefinePostprocessor : AssetPostprocessor
    {
        private const string PREFS_KEY = "DefinesCheck";

        [UnityEditor.Callbacks.DidReloadScripts]
        private static void AssemblyReload()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || string.IsNullOrEmpty(CoreEditor.FOLDER_CORE))
            {
                EditorApplication.delayCall += AssemblyReload;

                return;
            }

            EditorApplication.delayCall += () => DefineManager.CheckAutoDefines();
        }

        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
        {
            ValidateRequirement(importedAssets, deletedAssets);

            if (EditorApplication.isCompiling || EditorApplication.isUpdating || string.IsNullOrEmpty(CoreEditor.FOLDER_CORE))
            {
                EditorApplication.delayCall += () => OnPostprocessAllAssets(importedAssets, deletedAssets, movedAssets, movedFromAssetPaths, didDomainReload);

                return;
            }

            if (EditorPrefs.GetBool(PREFS_KEY, false))
            {
                DefineManager.CheckAutoDefines(deletedAssets);

                EditorPrefs.SetBool(PREFS_KEY, false);
            }
        }

        private static void ValidateRequirement(string[] importedAssets, string[] deletedAssets)
        {
            if (!importedAssets.IsNullOrEmpty())
            {
                foreach (string str in importedAssets)
                {
                    if (str.EndsWith(".cs") || str.EndsWith(".dll"))
                    {
                        EditorPrefs.SetBool(PREFS_KEY, true);

                        return;
                    }
                }
            }

            if (!deletedAssets.IsNullOrEmpty())
            {
                foreach (string str in deletedAssets)
                {
                    if (str.EndsWith(".cs") || str.EndsWith(".dll"))
                    {
                        EditorPrefs.SetBool(PREFS_KEY, true);

                        return;
                    }
                }
            }
        }
    }
}