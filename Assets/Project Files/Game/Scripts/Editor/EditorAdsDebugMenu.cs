#if UNITY_EDITOR
using UnityEngine;

namespace NebulaSoft
{
    /// <summary>
    /// Editor-only local save reset for testing banner ads again.
    /// This intentionally does not change Firebase or Google Play ownership.
    /// </summary>
    public static class EditorAdsDebugMenu
    {
        private static void RestoreAdsInPlayMode()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[Editor Ads] Enter Play Mode before restoring ads.");
                return;
            }

            if (AdsManager.Settings == null)
            {
                Debug.LogWarning("[Editor Ads] AdsManager is not initialized yet. Run this after the game reaches the menu.");
                return;
            }

            AdsManager.SetNoAdsEntitlement(false);
            AdsManager.EnableBanner();
            Debug.Log("[Editor Ads] Cleared the local No Ads save and enabled the banner.");
        }
    }
}
#endif
