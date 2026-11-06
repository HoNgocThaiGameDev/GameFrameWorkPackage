#if PLAYMAKER
using UnityEngine;
using HutongGames.PlayMaker;
using TooltipAttribute = HutongGames.PlayMaker.TooltipAttribute;

namespace VoxelBusters.AdsKit.PlayMaker
{
    [ActionCategory("VoxelBusters - AdsKit - Ads Manager (Status and state)")]
    [Tooltip("Checks whether Ads Kit is running in test mode.")]
    public class AdsManagerIsTestMode : FsmStateAction
    {
        [ActionSection("Results")]
        [UIHint(UIHint.Variable)]
        [Tooltip("True if Ads Kit is in test mode.")]
        public FsmBool isTestMode;

        public override void Reset()
        {
            isTestMode = new FsmBool { UseVariable = true };
        }

        public override void OnEnter()
        {
#if ENABLE_VOXELBUSTERS_ADS_KIT
            if (!isTestMode.IsNone)
            {
                isTestMode.Value = AdsManager.IsTestMode;
            }
#else
            Debug.LogWarning("AdsManagerIsTestMode: Ads Kit is not enabled.");
            if (!isTestMode.IsNone)
            {
                isTestMode.Value = false;
            }
#endif
            Finish();
        }
    }
}
#endif
