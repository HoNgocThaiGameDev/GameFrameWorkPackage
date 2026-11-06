#if UNITY_EDITOR
using UnityEngine;

namespace NebulaSoft.EditorTools
{
    internal static class DailyRewardDevTools
    {
        private static void ResetToDayOne()
        {
            if (!DailyRewardService.ResetForTesting())
            {
                Debug.LogWarning("[Daily Reward] Start Play mode and open the Menu before resetting Daily Reward.");
                return;
            }

            Debug.Log("[Daily Reward] Reset complete. Day 1 is now available to claim.");
        }
    }
}
#endif
