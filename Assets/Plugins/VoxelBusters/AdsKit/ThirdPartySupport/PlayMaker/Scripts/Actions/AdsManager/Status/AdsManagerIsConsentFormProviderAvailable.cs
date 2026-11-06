#if PLAYMAKER
using UnityEngine;
using HutongGames.PlayMaker;
using TooltipAttribute = HutongGames.PlayMaker.TooltipAttribute;

namespace VoxelBusters.AdsKit.PlayMaker
{
    [ActionCategory("VoxelBusters - AdsKit - Ads Manager (Initialization and consent)")]
    [Tooltip("Checks whether any IConsentFormProvider implementation is available via AdServices.")]
    public class AdsManagerIsConsentFormProviderAvailable : FsmStateAction
    {
        [ActionSection("Results")]
        [UIHint(UIHint.Variable)]
        [Tooltip("True if a consent form provider is available.")]
        public FsmBool isAvailable;

        public override void Reset()
        {
            isAvailable = new FsmBool { UseVariable = true };
        }

        public override void OnEnter()
        {
#if ENABLE_VOXELBUSTERS_ADS_KIT
            bool available = AdServices.GetConsentFormProvider() != null;
            if (!isAvailable.IsNone)
            {
                isAvailable.Value = available;
            }
#else
            Debug.LogWarning("AdsManagerIsConsentFormProviderAvailable: Ads Kit is not enabled.");
            if (!isAvailable.IsNone)
            {
                isAvailable.Value = false;
            }
#endif
            Finish();
        }
    }
}
#endif
