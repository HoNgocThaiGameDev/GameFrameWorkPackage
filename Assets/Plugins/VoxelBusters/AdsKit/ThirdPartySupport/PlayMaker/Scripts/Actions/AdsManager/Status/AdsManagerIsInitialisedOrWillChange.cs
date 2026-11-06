#if PLAYMAKER
using System;
using UnityEngine;
using HutongGames.PlayMaker;
using TooltipAttribute = HutongGames.PlayMaker.TooltipAttribute;

namespace VoxelBusters.AdsKit.PlayMaker
{
    [ActionCategory("VoxelBusters - AdsKit - Ads Manager (Status and state)")]
    [Tooltip("Checks whether Ads Kit is already initialised or currently initialising.")]
    public class AdsManagerIsInitialisedOrWillChange : FsmStateAction
    {
        [ActionSection("Results")]
        [UIHint(UIHint.Variable)]
        [Tooltip("True if Ads Kit is initialised or in-progress.")]
        public FsmBool isInitialisedOrWillChange;

        public override void Reset()
        {
            isInitialisedOrWillChange = new FsmBool { UseVariable = true };
        }

        public override void OnEnter()
        {
#if ENABLE_VOXELBUSTERS_ADS_KIT
            bool value;
            try
            {
                value = AdsManager.IsInitialisedOrWillChange;
            }
            catch (Exception)
            {
                value = AdsManager.IsInitialised;
            }

            if (!isInitialisedOrWillChange.IsNone)
            {
                isInitialisedOrWillChange.Value = value;
            }
#else
            Debug.LogWarning("AdsManagerIsInitialisedOrWillChange: Ads Kit is not enabled.");
            if (!isInitialisedOrWillChange.IsNone)
            {
                isInitialisedOrWillChange.Value = false;
            }
#endif
            Finish();
        }
    }
}
#endif
