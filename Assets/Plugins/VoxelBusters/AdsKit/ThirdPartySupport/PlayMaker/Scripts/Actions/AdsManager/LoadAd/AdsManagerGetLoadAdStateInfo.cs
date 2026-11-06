#if PLAYMAKER
using HutongGames.PlayMaker;
using TooltipAttribute = HutongGames.PlayMaker.TooltipAttribute;
using VoxelBusters.AdsKit.Adapters;

namespace VoxelBusters.AdsKit.PlayMaker
{
    [ActionCategory("VoxelBusters - AdsKit - Ads Manager (Load, show, hide ads)")]
    [Tooltip("Reads per-network load state data from current PlayMaker event payload (Fsm.EventData) using an index.")]
    public class AdsManagerGetLoadAdStateInfo : FsmStateAction
    {
        [ActionSection("Inputs")]
        [Tooltip("Zero-based index in LoadStateArray.")]
        public FsmInt loadStateIndex;

        [ActionSection("Results")]
        [UIHint(UIHint.Variable)]
        public FsmString adUnitId;

        [UIHint(UIHint.Variable)]
        public FsmEnum adType;

        [UIHint(UIHint.Variable)]
        public FsmString networkId;

        [UIHint(UIHint.Variable)]
        public FsmEnum placementState;

        [UIHint(UIHint.Variable)]
        public FsmString errorDomain;

        [UIHint(UIHint.Variable)]
        public FsmInt errorCode;

        [UIHint(UIHint.Variable)]
        public FsmString errorDescription;

        public override void Reset()
        {
            loadStateIndex = new FsmInt { UseVariable = false, Value = 0 };
            adUnitId = new FsmString { UseVariable = true };
            adType = new FsmEnum { UseVariable = true, EnumType = typeof(AdType), Value = AdType.Interstitial };
            networkId = new FsmString { UseVariable = true };
            placementState = new FsmEnum { UseVariable = true, EnumType = typeof(AdPlacementState), Value = AdPlacementState.Unknown };
            errorDomain = new FsmString { UseVariable = true };
            errorCode = new FsmInt { UseVariable = true };
            errorDescription = new FsmString { UseVariable = true };
        }

        public override void OnEnter()
        {
            var wrapper = Fsm.EventData.ObjectData as AdsManagerOnLoadAdCompleteEventDataWrapper;
            LoadAdResult result = wrapper != null ? wrapper.result : null;
            if (result == null)
            {
                LogWarning("AdsManagerGetLoadAdStateInfo: No load success payload found in Fsm.EventData.ObjectData.");
                ApplyDefaults();
                Finish();
                return;
            }

            AdNetworkLoadAdStateInfo[] states = result.LoadStateArray;
            int index = loadStateIndex.Value;
            if (states == null || index < 0 || index >= states.Length)
            {
                LogWarning($"AdsManagerGetLoadAdStateInfo: Invalid loadStateIndex {index}.");
                ApplyDefaults();
                Finish();
                return;
            }

            AdNetworkLoadAdStateInfo state = states[index];
            if (state == null)
            {
                ApplyDefaults();
                Finish();
                return;
            }

            if (!adUnitId.IsNone)
            {
                adUnitId.Value = state.AdUnitId ?? string.Empty;
            }

            if (!adType.IsNone)
            {
                adType.Value = state.AdType;
            }

            if (!networkId.IsNone)
            {
                networkId.Value = state.NetworkId ?? string.Empty;
            }

            if (!placementState.IsNone)
            {
                placementState.Value = state.PlacementState;
            }

            var error = state.Error;
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

            Finish();
        }

        private void ApplyDefaults()
        {
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

            if (!placementState.IsNone)
            {
                placementState.Value = AdPlacementState.Unknown;
            }

            if (!errorDomain.IsNone)
            {
                errorDomain.Value = string.Empty;
            }

            if (!errorCode.IsNone)
            {
                errorCode.Value = 0;
            }

            if (!errorDescription.IsNone)
            {
                errorDescription.Value = string.Empty;
            }
        }
    }
}
#endif
