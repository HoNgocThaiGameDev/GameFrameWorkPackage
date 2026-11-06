using UnityEditor;

namespace NebulaSoft
{
    internal static class IAPSettingsResolver
    {
        public static IAPSettings Resolve(out string error)
        {
            error = null;
            IAPSettings selected = null;
            bool hasModule = false;
            foreach (string guid in AssetDatabase.FindAssets("t:ProjectInitSettings"))
            {
                var registry = AssetDatabase.LoadAssetAtPath<ProjectInitSettings>(AssetDatabase.GUIDToAssetPath(guid));
                var module = registry.GetModule<IAPInitModule>();
                if (module == null) continue;
                hasModule = true;
                if (module.Settings == null) { error = "Assign IAP Settings in the IAP Init Module."; return null; }
                if (selected != null && selected != module.Settings) { error = "Multiple IAP modules reference different settings. Use one catalog per project."; return null; }
                selected = module.Settings;
            }
            if (hasModule) return selected;
            foreach (string guid in AssetDatabase.FindAssets("t:MonetizationSettings"))
            {
                var monetization = AssetDatabase.LoadAssetAtPath<MonetizationSettings>(AssetDatabase.GUIDToAssetPath(guid));
                if (monetization.IAPSettings == null) continue;
                if (selected != null && selected != monetization.IAPSettings) { error = "Multiple catalogs found. Add an IAP Init Module to select one."; return null; }
                selected = monetization.IAPSettings;
            }
            if (selected == null) error = "Add Monetization/IAP to Project Init Settings and assign IAP Settings.";
            return selected;
        }
    }
}
