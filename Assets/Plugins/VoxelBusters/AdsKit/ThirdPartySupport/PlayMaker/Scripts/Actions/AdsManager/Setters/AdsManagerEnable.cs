#if PLAYMAKER
using UnityEngine;
using HutongGames.PlayMaker;
using TooltipAttribute = HutongGames.PlayMaker.TooltipAttribute;

namespace VoxelBusters.AdsKit.PlayMaker
{
    [ActionCategory("VoxelBusters - AdsKit - Ads Manager (Configuration setters)")]
    [Tooltip("Reenables AdsManager operations.")]
    public class AdsManagerEnable : FsmStateAction
    {
        public override void Reset()
        {
        }

        public override void OnEnter()
        {
#if ENABLE_VOXELBUSTERS_ADS_KIT
            AdsManager.Enable();
#else
            Debug.LogWarning("AdsManagerEnable: Ads Kit is not enabled.");
#endif
            Finish();
        }
    }
}
#endif
