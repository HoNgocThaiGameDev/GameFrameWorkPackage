#if PLAYMAKER
using UnityEngine;
using HutongGames.PlayMaker;
using VoxelBusters.CoreLibrary;
using TooltipAttribute = HutongGames.PlayMaker.TooltipAttribute;

namespace VoxelBusters.AdsKit.PlayMaker
{
    [ActionCategory("VoxelBusters - AdsKit - Ads Manager (Load, show, hide ads)")]
    [Tooltip("Hides a banner ad view for the specified placement. Use after banner load/show when you need to remove it. Works for Banner ads only.")]
    public class AdsManagerHideAd : FsmStateAction
    {
        [ActionSection("Inputs")]
        [RequiredField]
        [Tooltip("Placement identifier configured in Ads Kit settings.")]
        public FsmString placement;

        [Tooltip("Set true to destroy the banner instance. Banner only.")]
        public FsmBool destroy;

        [ActionSection("Events")]
        [Tooltip("Event sent when hide call is issued.")]
        public FsmEvent successEvent;

        [Tooltip("Event sent when validation fails.")]
        public FsmEvent failureEvent;

        public override void Reset()
        {
            placement = new FsmString { UseVariable = false };
            destroy = new FsmBool { UseVariable = false, Value = false };
            successEvent = null;
            failureEvent = null;
        }

        public override void OnEnter()
        {
#if ENABLE_VOXELBUSTERS_ADS_KIT
            string placementId = placement.Value;
            if (string.IsNullOrEmpty(placementId))
            {
                Debug.LogError("AdsManagerHideAd: Placement is null or empty.");
                Fsm.Event(failureEvent);
                Finish();
                return;
            }

            AdsManager.HideAd(placementId, !destroy.IsNone && destroy.Value);
            Fsm.Event(successEvent);
#else
            Debug.LogWarning("AdsManagerHideAd: Ads Kit is not enabled.");
            Fsm.Event(failureEvent);
#endif
            Finish();
        }
    }
}
#endif
