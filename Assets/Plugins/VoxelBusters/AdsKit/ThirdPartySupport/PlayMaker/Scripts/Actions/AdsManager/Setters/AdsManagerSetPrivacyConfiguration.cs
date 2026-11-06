#if PLAYMAKER
using UnityEngine;
using HutongGames.PlayMaker;
using VoxelBusters.CoreLibrary;
using TooltipAttribute = HutongGames.PlayMaker.TooltipAttribute;

namespace VoxelBusters.AdsKit.PlayMaker
{
    [ActionCategory("VoxelBusters - AdsKit - Ads Manager (Configuration setters)")]
    [Tooltip("Builds and applies ApplicationPrivacyConfiguration.")]
    public class AdsManagerSetPrivacyConfiguration : FsmStateAction
    {
        [ActionSection("Inputs")]
        [Tooltip("User consent status (GDPR).")]
        public FsmEnum usageConsent;

        [Tooltip("Set true to include IsAgeRestrictedUser in configuration.")]
        public FsmBool useAgeRestrictedUser;

        [Tooltip("COPPA flag value when UseAgeRestrictedUser is true.")]
        public FsmBool isAgeRestrictedUser;

        [Tooltip("Set true to include PreferredContentRating in configuration.")]
        public FsmBool usePreferredContentRating;

        [Tooltip("Preferred content rating value when UsePreferredContentRating is true.")]
        public FsmEnum preferredContentRating;

        [Tooltip("Set true to include DoNotSell in configuration.")]
        public FsmBool useDoNotSell;

        [Tooltip("DoNotSell value when UseDoNotSell is true.")]
        public FsmBool doNotSell;

        [Tooltip("Optional configuration version string.")]
        public FsmString version;

        public override void Reset()
        {
            usageConsent = new FsmEnum { UseVariable = false, EnumType = typeof(ConsentStatus), Value = ConsentStatus.NotDetermined };
            useAgeRestrictedUser = new FsmBool { UseVariable = false, Value = false };
            isAgeRestrictedUser = new FsmBool { UseVariable = false, Value = false };
            usePreferredContentRating = new FsmBool { UseVariable = false, Value = false };
            preferredContentRating = new FsmEnum { UseVariable = false, EnumType = typeof(ContentRating), Value = ContentRating.Unspecified };
            useDoNotSell = new FsmBool { UseVariable = false, Value = false };
            doNotSell = new FsmBool { UseVariable = false, Value = false };
            version = new FsmString { UseVariable = true };
        }

        public override void OnEnter()
        {
#if ENABLE_VOXELBUSTERS_ADS_KIT
            ConsentStatus consent = !usageConsent.IsNone
                ? (ConsentStatus)usageConsent.Value
                : ConsentStatus.NotDetermined;

            bool? ageRestricted = (!useAgeRestrictedUser.IsNone && useAgeRestrictedUser.Value)
                ? isAgeRestrictedUser.Value
                : (bool?)null;

            ContentRating? contentRating = (!usePreferredContentRating.IsNone && usePreferredContentRating.Value)
                ? (ContentRating)preferredContentRating.Value
                : (ContentRating?)null;

            bool? doNotSellValue = (!useDoNotSell.IsNone && useDoNotSell.Value)
                ? doNotSell.Value
                : (bool?)null;

            string versionValue = !version.IsNone ? version.Value : null;

            var config = new ApplicationPrivacyConfiguration(consent,
                                                             ageRestricted,
                                                             contentRating,
                                                             doNotSellValue,
                                                             versionValue);
            AdsManager.SetPrivacyConfiguration(config);
#else
            Debug.LogWarning("AdsManagerSetPrivacyConfiguration: Ads Kit is not enabled.");
#endif
            Finish();
        }
    }
}
#endif
