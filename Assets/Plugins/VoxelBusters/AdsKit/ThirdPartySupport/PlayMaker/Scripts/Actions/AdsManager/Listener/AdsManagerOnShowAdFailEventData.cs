#if PLAYMAKER
using UnityEngine;
using HutongGames.PlayMaker;
using TooltipAttribute = HutongGames.PlayMaker.TooltipAttribute;

namespace VoxelBusters.AdsKit.PlayMaker
{
    [ActionCategory("VoxelBusters - AdsKit - Ads Manager (Listener)")]
    [Tooltip("Reads show ad fail event data from the current PlayMaker event.")]
    public class AdsManagerOnShowAdFailEventData : FsmStateAction
    {
        [ActionSection("Results")]
        [UIHint(UIHint.Variable)]
        public FsmString placement;

        [UIHint(UIHint.Variable)]
        public FsmString errorDomain;

        [UIHint(UIHint.Variable)]
        public FsmInt errorCode;

        [UIHint(UIHint.Variable)]
        public FsmString errorDescription;

        [ActionSection("Options")]
        [Tooltip("Logs the event details received by this action.")]
        public bool logEventDetails;

        public override void Reset()
        {
            placement = new FsmString { UseVariable = true };
            errorDomain = new FsmString { UseVariable = true };
            errorCode = new FsmInt { UseVariable = true };
            errorDescription = new FsmString { UseVariable = true };
            logEventDetails = false;
        }

        public override void OnEnter()
        {
            string resolvedPlacement = string.Empty;
            var wrapper = Fsm.EventData.ObjectData as AdsManagerOnShowAdFailEventDataWrapper;
            var error = wrapper != null ? wrapper.error : null;

            if (wrapper != null)
            {
                resolvedPlacement = wrapper.placement ?? string.Empty;
            }

            if (!placement.IsNone)
            {
                placement.Value = resolvedPlacement;
            }

            if (!errorDomain.IsNone)
            {
                errorDomain.Value = error != null ? (error.Domain ?? string.Empty) : string.Empty;
            }

            if (!errorCode.IsNone)
            {
                errorCode.Value = error != null ? error.Code : 0;
            }

            if (!errorDescription.IsNone)
            {
                errorDescription.Value = error != null ? (error.Description ?? string.Empty) : string.Empty;
            }

            AdsManagerEventDataLogUtility.LogIfEnabled(logEventDetails, nameof(AdsManagerOnShowAdFailEventData), wrapper);
            Finish();
        }

    }
}
#endif
