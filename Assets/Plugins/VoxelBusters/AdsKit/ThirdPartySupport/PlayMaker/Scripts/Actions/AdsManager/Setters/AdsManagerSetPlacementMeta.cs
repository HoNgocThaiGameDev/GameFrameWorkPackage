#if PLAYMAKER
using UnityEngine;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using TooltipAttribute = HutongGames.PlayMaker.TooltipAttribute;

namespace VoxelBusters.AdsKit.PlayMaker
{
    [ActionCategory("VoxelBusters - AdsKit - Ads Manager (Configuration setters)")]
    [Tooltip("Creates one AdPlacementMeta entry and applies it via AdsManager.SetPlacementMeta.")]
    public class AdsManagerSetPlacementMeta : FsmStateAction
    {
        [ActionSection("Inputs")]
        [RequiredField]
        [Tooltip("Placement name/id.")]
        public FsmString placement;

        [Tooltip("Ad type for this placement.")]
        public FsmEnum adType;

        [Tooltip("Enable auto-load for this placement.")]
        public FsmBool autoLoad;

        [ObjectType(typeof(AdContentOptionsAsset))]
        [Tooltip("Optional options asset for this placement (for example BannerAdOptionsAsset).")]
        public FsmObject adContentOptionsAsset;

        [ActionSection("Events")]
        public FsmEvent successEvent;

        public FsmEvent failureEvent;

        [ActionSection("Results")]
        [UIHint(UIHint.Variable)]
        public FsmString errorMessage;

        public override void Reset()
        {
            placement = new FsmString { UseVariable = false };
            adType = new FsmEnum { UseVariable = false, EnumType = typeof(AdType), Value = AdType.Interstitial };
            autoLoad = new FsmBool { UseVariable = false, Value = false };
            adContentOptionsAsset = new FsmObject { UseVariable = true };
            successEvent = null;
            failureEvent = null;
            errorMessage = new FsmString { UseVariable = true };
        }

        public override void OnEnter()
        {
#if ENABLE_VOXELBUSTERS_ADS_KIT
            string placementId = placement.Value;
            if (string.IsNullOrEmpty(placementId))
            {
                SetFailure("Placement is null or empty.");
                return;
            }

            AdContentOptionsAsset optionsAsset = null;
            if (!adContentOptionsAsset.IsNone && adContentOptionsAsset.Value != null)
            {
                optionsAsset = adContentOptionsAsset.Value as AdContentOptionsAsset;
                if (optionsAsset == null)
                {
                    SetFailure("adContentOptionsAsset is not a valid AdContentOptionsAsset.");
                    return;
                }
            }

            var meta = new AdPlacementMeta(name: placementId,
                                           adType: (AdType)adType.Value,
                                           autoLoad: !autoLoad.IsNone && autoLoad.Value,
                                           optionsObject: optionsAsset);

            AdsManager.SetPlacementMeta(meta);

            if (!errorMessage.IsNone)
            {
                errorMessage.Value = string.Empty;
            }

            Fsm.Event(successEvent);
            Finish();
#else
            SetFailure("Ads Kit is not enabled.");
#endif
        }

        private void SetFailure(string message)
        {
            Debug.LogError($"AdsManagerSetPlacementMeta: {message}");
            if (!errorMessage.IsNone)
            {
                errorMessage.Value = message ?? string.Empty;
            }
            Fsm.Event(failureEvent);
            Finish();
        }
    }
}
#endif
