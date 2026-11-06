#if PLAYMAKER
using UnityEngine;
using HutongGames.PlayMaker;
using TooltipAttribute = HutongGames.PlayMaker.TooltipAttribute;

namespace VoxelBusters.AdsKit.PlayMaker
{
    [ActionCategory("VoxelBusters - AdsKit - Ads Manager (Listener)")]
    [Tooltip("Reads load ad complete event data from the current PlayMaker event.")]
    public class AdsManagerOnLoadAdCompleteEventData : FsmStateAction
    {
        [ActionSection("Results")]
        [UIHint(UIHint.Variable)]
        public FsmString placement;

        [UIHint(UIHint.Variable)]
        public FsmString adUnitId;

        [UIHint(UIHint.Variable)]
        public FsmEnum adType;

        [UIHint(UIHint.Variable)]
        public FsmEnum placementState;

        [UIHint(UIHint.Variable)]
        public FsmString preferredAdProvider;

        [ActionSection("Options")]
        [Tooltip("Logs the event details received by this action.")]
        public bool logEventDetails;

        public override void Reset()
        {
            placement = new FsmString { UseVariable = true };
            adUnitId = new FsmString { UseVariable = true };
            adType = new FsmEnum { UseVariable = true, EnumType = typeof(AdType), Value = AdType.Interstitial };
            placementState = new FsmEnum { UseVariable = true, EnumType = typeof(AdPlacementState), Value = AdPlacementState.Unknown };
            preferredAdProvider = new FsmString { UseVariable = true };
            logEventDetails = false;
        }

        public override void OnEnter()
        {
            string resolvedPlacement = string.Empty;
            LoadAdResult result = null;

            var wrapper = Fsm.EventData.ObjectData as AdsManagerOnLoadAdCompleteEventDataWrapper;
            if (wrapper != null)
            {
                resolvedPlacement = wrapper.placement ?? string.Empty;
                result = wrapper.result;
            }

            if (result == null)
            {
                ApplyDefaults(resolvedPlacement);
                AdsManagerEventDataLogUtility.LogIfEnabled(logEventDetails, nameof(AdsManagerOnLoadAdCompleteEventData), wrapper);
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

            if (!placementState.IsNone)
            {
                placementState.Value = result.PlacementState;
            }

            if (!preferredAdProvider.IsNone)
            {
                preferredAdProvider.Value = result.PreferredAdProvider ?? string.Empty;
            }

            AdsManagerEventDataLogUtility.LogIfEnabled(logEventDetails, nameof(AdsManagerOnLoadAdCompleteEventData), wrapper);
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

            if (!placementState.IsNone)
            {
                placementState.Value = AdPlacementState.Unknown;
            }

            if (!preferredAdProvider.IsNone)
            {
                preferredAdProvider.Value = string.Empty;
            }
        }

    }
}
#endif
