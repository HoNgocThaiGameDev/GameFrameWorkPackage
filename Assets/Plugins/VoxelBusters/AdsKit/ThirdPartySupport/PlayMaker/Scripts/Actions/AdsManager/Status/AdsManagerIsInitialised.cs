#if PLAYMAKER
using UnityEngine;
using HutongGames.PlayMaker;
using TooltipAttribute = HutongGames.PlayMaker.TooltipAttribute;

namespace VoxelBusters.AdsKit.PlayMaker
{
    [ActionCategory("VoxelBusters - AdsKit - Ads Manager (Status and state)")]
    [Tooltip("Checks whether Ads Kit is fully initialised.")]
    public class AdsManagerIsInitialised : FsmStateAction
    {
        [ActionSection("Results")]
        [UIHint(UIHint.Variable)]
        [Tooltip("True if Ads Kit is fully initialised.")]
        public FsmBool isInitialised;

        public override void Reset()
        {
            isInitialised = new FsmBool { UseVariable = true };
        }

        public override void OnEnter()
        {
#if ENABLE_VOXELBUSTERS_ADS_KIT
            if (!isInitialised.IsNone)
            {
                isInitialised.Value = AdsManager.IsInitialised;
            }
#else
            Debug.LogWarning("AdsManagerIsInitialised: Ads Kit is not enabled.");
            if (!isInitialised.IsNone)
            {
                isInitialised.Value = false;
            }
#endif
            Finish();
        }
    }
}
#endif
