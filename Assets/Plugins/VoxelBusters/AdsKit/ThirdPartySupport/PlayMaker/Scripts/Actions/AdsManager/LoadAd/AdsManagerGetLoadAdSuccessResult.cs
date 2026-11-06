#if PLAYMAKER
using HutongGames.PlayMaker;
using TooltipAttribute = HutongGames.PlayMaker.TooltipAttribute;
using VoxelBusters.AdsKit.Adapters;

    namespace VoxelBusters.AdsKit.PlayMaker
{
    [ActionCategory("VoxelBusters - AdsKit - Ads Manager (Load, show, hide ads)")]
    [Tooltip("Reads LoadAd success data from current PlayMaker event payload (Fsm.EventData). Use AdsManagerGetLoadAdStateInfo for per-network load state entries.")]
    public class AdsManagerGetLoadAdSuccessResult : FsmStateAction
    {
        [ActionSection("Results")]

        [UIHint(UIHint.Variable)]
        public FsmString adUnitId;

        [UIHint(UIHint.Variable)]
        public FsmEnum adType;

        [UIHint(UIHint.Variable)]
        public FsmEnum placementState;

        [UIHint(UIHint.Variable)]
        public FsmString preferredAdProvider;

        [UIHint(UIHint.Variable)]
        public FsmInt loadStateCount;

        public override void Reset()
        {
            adUnitId = new FsmString { UseVariable = true };
            adType = new FsmEnum { UseVariable = true, EnumType = typeof(AdType), Value = AdType.Interstitial };
            placementState = new FsmEnum { UseVariable = true, EnumType = typeof(AdPlacementState), Value = AdPlacementState.Unknown };
            preferredAdProvider = new FsmString { UseVariable = true };
            loadStateCount = new FsmInt { UseVariable = true };
        }

        public override void OnEnter()
        {
            var wrapper = Fsm.EventData.ObjectData as AdsManagerOnLoadAdCompleteEventDataWrapper;
            var result = wrapper != null ? wrapper.result : null;
            if (result == null)
            {
                LogWarning("AdsManagerGetLoadAdSuccessResult: No load success payload found in Fsm.EventData.ObjectData.");
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

            if (!placementState.IsNone)
            {
                placementState.Value = result.PlacementState;
            }

            if (!preferredAdProvider.IsNone)
            {
                preferredAdProvider.Value = result.PreferredAdProvider ?? string.Empty;
            }

            AdNetworkLoadAdStateInfo[] states = result.LoadStateArray;

            if (!loadStateCount.IsNone)
            {
                loadStateCount.Value = states != null ? states.Length : 0;
            }

            Finish();
        }

        private void ApplyDefaults()
        {
            if (!adUnitId.IsNone)
            {
                adUnitId.Value = string.Empty;
            }

            if (!preferredAdProvider.IsNone)
            {
                preferredAdProvider.Value = string.Empty;
            }

            if (!loadStateCount.IsNone)
            {
                loadStateCount.Value = 0;
            }
        }
    }
}
#endif
