#if PLAYMAKER
using UnityEngine;
using HutongGames.PlayMaker;
using TooltipAttribute = HutongGames.PlayMaker.TooltipAttribute;

namespace VoxelBusters.AdsKit.PlayMaker
{
    [ActionCategory("VoxelBusters - AdsKit - Ads Manager (Listener)")]
    [Tooltip("Reads ad paid event data from the current PlayMaker event.")]
    public class AdsManagerOnAdPaidEventData : FsmStateAction
    {
        [ActionSection("Results")]
        [UIHint(UIHint.Variable)]
        public FsmString placement;

        [UIHint(UIHint.Variable)]
        public FsmEnum adType;

        [UIHint(UIHint.Variable)]
        public FsmString networkId;

        [UIHint(UIHint.Variable)]
        public FsmString currencyCode;

        [UIHint(UIHint.Variable)]
        public FsmFloat value;

        [UIHint(UIHint.Variable)]
        public FsmEnum precision;

        [ActionSection("Options")]
        [Tooltip("Logs the event details received by this action.")]
        public bool logEventDetails;

        public override void Reset()
        {
            placement = new FsmString { UseVariable = true };
            adType = new FsmEnum { UseVariable = true, EnumType = typeof(AdType), Value = AdType.Interstitial };
            networkId = new FsmString { UseVariable = true };
            currencyCode = new FsmString { UseVariable = true };
            value = new FsmFloat { UseVariable = true };
            precision = new FsmEnum { UseVariable = true, EnumType = typeof(AdTransaction.PrecisionType), Value = AdTransaction.PrecisionType.Unknown };
            logEventDetails = false;
        }

        public override void OnEnter()
        {
            string resolvedPlacement = string.Empty;
            AdTransaction transaction = null;

            var wrapper = Fsm.EventData.ObjectData as AdsManagerOnAdPaidEventDataWrapper;
            if (wrapper != null)
            {
                resolvedPlacement = wrapper.placement ?? string.Empty;
                transaction = wrapper.transaction;
            }

            if (transaction == null)
            {
                ApplyDefaults(resolvedPlacement);
                AdsManagerEventDataLogUtility.LogIfEnabled(logEventDetails, nameof(AdsManagerOnAdPaidEventData), wrapper);
                Finish();
                return;
            }

            if (!placement.IsNone)
            {
                placement.Value = resolvedPlacement;
            }

            if (!adType.IsNone)
            {
                adType.Value = transaction.AdType;
            }

            if (!networkId.IsNone)
            {
                networkId.Value = transaction.NetworkId ?? string.Empty;
            }

            if (!currencyCode.IsNone)
            {
                currencyCode.Value = transaction.CurrencyCode ?? string.Empty;
            }

            if (!value.IsNone)
            {
                value.Value = (float)transaction.Value;
            }

            if (!precision.IsNone)
            {
                precision.Value = transaction.Precision;
            }

            AdsManagerEventDataLogUtility.LogIfEnabled(logEventDetails, nameof(AdsManagerOnAdPaidEventData), wrapper);
            Finish();
        }

        private void ApplyDefaults(string resolvedPlacement)
        {
            if (!placement.IsNone)
            {
                placement.Value = resolvedPlacement;
            }

            if (!adType.IsNone)
            {
                adType.Value = AdType.Interstitial;
            }

            if (!networkId.IsNone)
            {
                networkId.Value = string.Empty;
            }

            if (!currencyCode.IsNone)
            {
                currencyCode.Value = string.Empty;
            }

            if (!value.IsNone)
            {
                value.Value = 0f;
            }

            if (!precision.IsNone)
            {
                precision.Value = AdTransaction.PrecisionType.Unknown;
            }
        }

    }
}
#endif
