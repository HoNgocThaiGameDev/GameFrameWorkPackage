#if MODULE_ADSKIT
using System;
using UnityEngine;
using VoxelBusters.AdsKit;

namespace NebulaSoft
{
    [Define("MODULE_ADMOB", "GoogleMobileAds.Api.MobileAds", "GoogleMobileAds.Unity.dll")]
    [CreateAssetMenu(menuName = "NebulaSoft/Ads/Providers/Ads Kit AdMob Consent")]
    public sealed class AdsKitAdMobConsentConfig : AdsKitConsentProviderConfig
    {
        [SerializeField] bool ageRestricted;
        public override IConsentFormProvider CreateProvider()
        {
#if UNITY_EDITOR
            return new DefaultAlwaysAuthorizedConsentFormProvider();
#elif MODULE_ADMOB
            return new VoxelBusters.AdsKit.Adapters.AdMobConsentFormProvider { IsAgeRestrictedUser = ageRestricted };
#else
            throw new InvalidOperationException("AdMob consent requires the Ads Kit AdMob integration. Provide another consent factory for other networks.");
#endif
        }
    }
}
#endif
