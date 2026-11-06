#if PLAYMAKER
using UnityEngine;
using HutongGames.PlayMaker;
using TooltipAttribute = HutongGames.PlayMaker.TooltipAttribute;

namespace VoxelBusters.AdsKit.PlayMaker
{
    [ActionCategory("VoxelBusters - AdsKit - Ads Manager (Listener)")]
    [Tooltip("Reads show ad complete event data from the current PlayMaker event.")]
    public class AdsManagerOnShowAdCompleteEventData : FsmStateAction
    {
        [ActionSection("Results")]
        [UIHint(UIHint.Variable)]
        public FsmString placement;

        [UIHint(UIHint.Variable)]
        public FsmString adUnitId;

        [UIHint(UIHint.Variable)]
        public FsmEnum adType;

        [UIHint(UIHint.Variable)]
        public FsmString networkId;

        [UIHint(UIHint.Variable)]
        public FsmBool clicked;

        [UIHint(UIHint.Variable)]
        public FsmEnum resultCode;

        [UIHint(UIHint.Variable)]
        public FsmFloat watchDuration;

        [ActionSection("Options")]
        [Tooltip("Logs the event details received by this action.")]
        public bool logEventDetails;

        public override void Reset()
        {
            placement = new FsmString { UseVariable = true };
            adUnitId = new FsmString { UseVariable = true };
            adType = new FsmEnum { UseVariable = true, EnumType = typeof(AdType), Value = AdType.Interstitial };
            networkId = new FsmString { UseVariable = true };
            clicked = new FsmBool { UseVariable = true };
            resultCode = new FsmEnum { UseVariable = true, EnumType = typeof(ShowAdResultCode), Value = ShowAdResultCode.Unknown };
            watchDuration = new FsmFloat { UseVariable = true };
            logEventDetails = false;
        }

        public override void OnEnter()
        {
            string resolvedPlacement = string.Empty;
            ShowAdResult result = null;

            var wrapper = Fsm.EventData.ObjectData as AdsManagerOnShowAdCompleteEventDataWrapper;
            if (wrapper != null)
            {
                resolvedPlacement = wrapper.placement ?? string.Empty;
                result = wrapper.result;
            }

            if (result == null)
            {
                ApplyDefaults(resolvedPlacement);
                AdsManagerEventDataLogUtility.LogIfEnabled(logEventDetails, nameof(AdsManagerOnShowAdCompleteEventData), wrapper);
                Finish();
                return;
            }

            if (!placement.IsNone)
            {
                placement.Value = resolvedPlacement;
            }

            if (!adUnitId.IsNone)
            {
                adUnitId.Value = result.AdUnitId ?? string.Empty;
            }

            if (!adType.IsNone)
            {
                adType.Value = result.AdType;
            }

            if (!networkId.IsNone)
            {
                networkId.Value = result.NetworkId ?? string.Empty;
            }

            if (!clicked.IsNone)
            {
                clicked.Value = result.Clicked;
            }

            if (!resultCode.IsNone)
            {
                resultCode.Value = result.ResultCode;
            }

            if (!watchDuration.IsNone)
            {
                watchDuration.Value = result.WatchDuration.HasValue ? (float)result.WatchDuration.Value : 0f;
            }

            AdsManagerEventDataLogUtility.LogIfEnabled(logEventDetails, nameof(AdsManagerOnShowAdCompleteEventData), wrapper);
            Finish();
        }

        private void ApplyDefaults(string resolvedPlacement)
        {
            if (!placement.IsNone)
            {
                placement.Value = resolvedPlacement;
            }

            if (!adUnitId.IsNone)
            {
                adUnitId.Value = string.Empty;
            }

            if (!adType.IsNone)
            {
                adType.Value = AdType.Interstitial;
            }

            if (!networkId.IsNone)
            {
                networkId.Value = string.Empty;
            }

            if (!clicked.IsNone)
            {
                clicked.Value = false;
            }

            if (!resultCode.IsNone)
            {
                resultCode.Value = ShowAdResultCode.Unknown;
            }

            if (!watchDuration.IsNone)
            {
                watchDuration.Value = 0f;
            }
        }

    }
}
#endif
