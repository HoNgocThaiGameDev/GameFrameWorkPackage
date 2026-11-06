#if PLAYMAKER
using UnityEngine;
using HutongGames.PlayMaker;
using TooltipAttribute = HutongGames.PlayMaker.TooltipAttribute;

namespace VoxelBusters.AdsKit.PlayMaker
{
    [ActionCategory("VoxelBusters - AdsKit - Ads Manager (Listener)")]
    [Tooltip("Reads ad impression event data from the current PlayMaker event.")]
    public class AdsManagerOnAdImpressionRecordedEventData : FsmStateAction
    {
        [ActionSection("Results")]
        [UIHint(UIHint.Variable)]
        public FsmString placement;

        [ActionSection("Options")]
        [Tooltip("Logs the event details received by this action.")]
        public bool logEventDetails;

        public override void Reset()
        {
            placement = new FsmString { UseVariable = true };
            logEventDetails = false;
        }

        public override void OnEnter()
        {
            string resolvedPlacement = string.Empty;
            var wrapper = Fsm.EventData.ObjectData as AdsManagerOnAdImpressionRecordedEventDataWrapper;
            if (wrapper != null)
            {
                resolvedPlacement = wrapper.placement ?? string.Empty;
            }

            if (!placement.IsNone)
            {
                placement.Value = resolvedPlacement;
            }

            AdsManagerEventDataLogUtility.LogIfEnabled(logEventDetails, nameof(AdsManagerOnAdImpressionRecordedEventData), wrapper);
            Finish();
        }

    }
}
#endif
