#if PLAYMAKER
using UnityEngine;
using HutongGames.PlayMaker;
using TooltipAttribute = HutongGames.PlayMaker.TooltipAttribute;

namespace VoxelBusters.AdsKit.PlayMaker
{
    [ActionCategory("VoxelBusters - AdsKit - Ads Manager (Configuration setters)")]
    [Tooltip("Disables AdsManager operations and hides active ads.")]
    public class AdsManagerDisable : FsmStateAction
    {
        public override void Reset()
        {
        }

        public override void OnEnter()
        {
#if ENABLE_VOXELBUSTERS_ADS_KIT
            AdsManager.Disable();
#else
            Debug.LogWarning("AdsManagerDisable: Ads Kit is not enabled.");
#endif
            Finish();
        }
    }
}
#endif
