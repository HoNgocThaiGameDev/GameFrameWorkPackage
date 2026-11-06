#if PLAYMAKER
using HutongGames.PlayMaker;
using TooltipAttribute = HutongGames.PlayMaker.TooltipAttribute;

namespace VoxelBusters.AdsKit.PlayMaker
{
    [ActionCategory("VoxelBusters - AdsKit - Ads Manager (Load, show, hide ads)")]
    [Tooltip("Reads ShowAd success data from current PlayMaker event payload (Fsm.EventData).")]
    public class AdsManagerGetShowAdSuccessResult : FsmStateAction
    {
        [ActionSection("Results")]

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
        [Tooltip("Watch duration in seconds.")]
        public FsmFloat watchDuration;

        public override void Reset()
        {
            adUnitId = new FsmString { UseVariable = true };
            adType = new FsmEnum { UseVariable = true, EnumType = typeof(AdType), Value = AdType.Interstitial };
            networkId = new FsmString { UseVariable = true };
            clicked = new FsmBool { UseVariable = true };
            resultCode = new FsmEnum { UseVariable = true, EnumType = typeof(ShowAdResultCode), Value = ShowAdResultCode.Unknown };
            watchDuration = new FsmFloat { UseVariable = true };
        }

        public override void OnEnter()
        {
            var wrapper = Fsm.EventData.ObjectData as AdsManagerOnShowAdCompleteEventDataWrapper;
            ShowAdResult result = wrapper != null ? wrapper.result : null;
            if (result == null)
            {
                LogWarning("AdsManagerGetShowAdSuccessResult: No show success payload found in Fsm.EventData.ObjectData.");
                ApplyDefaults();
                Finish();
                return;
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

            Finish();
        }

        private void ApplyDefaults()
        {
            if (!adUnitId.IsNone)
            {
                adUnitId.Value = string.Empty;
            }

            if (!networkId.IsNone)
            {
                networkId.Value = string.Empty;
            }

            if (!clicked.IsNone)
            {
                clicked.Value = false;
            }

            if (!watchDuration.IsNone)
            {
                watchDuration.Value = 0f;
            }
        }
    }
}
#endif
