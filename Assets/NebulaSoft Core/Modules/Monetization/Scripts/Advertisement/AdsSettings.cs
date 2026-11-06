#pragma warning disable 0414

using UnityEngine;

namespace NebulaSoft
{
    [HelpURL("")]
    public class AdsSettings : ScriptableObject
    {
        [SerializeField] NebulaSoft.Ads.AdsModuleSettings moduleSettings;
        public NebulaSoft.Ads.AdsModuleSettings ModuleSettings => moduleSettings;
        [BoxGroup("Advertisement", "Advertisement")]
        [SerializeField] AdProvider bannerType = AdProvider.Dummy;
        public AdProvider BannerType => bannerType;

        [BoxGroup("Advertisement")]
        [SerializeField] AdProvider interstitialType = AdProvider.Dummy;
        public AdProvider InterstitialType => interstitialType;

        [BoxGroup("Advertisement")]
        [SerializeField] AdProvider rewardedVideoType = AdProvider.Dummy;
        public AdProvider RewardedVideoType => rewardedVideoType;

        [BoxGroup("Settings", "Settings")]
        [SerializeField] bool loadAdsOnStart = true;
        public bool LoadAdsOnStart => loadAdsOnStart;

        [BoxGroup("Reward", "Reward")]
        [SerializeField] Sprite noAdsRewardSprite;
        public Sprite NoAdsRewardSprite => noAdsRewardSprite;

        [Space]
        [BoxGroup("Settings/Interstitial")]
        [Tooltip("Delay in seconds before interstitial appearings on first game launch.")]
        [SerializeField] float interstitialFirstStartDelay = 40f;
        public float InterstitialFirstStartDelay => interstitialFirstStartDelay;

        [BoxGroup("Settings/Interstitial")]
        [Tooltip("Delay in seconds before interstitial appearings.")]
        [SerializeField] float interstitialStartDelay = 40f;
        public float InterstitialStartDelay => interstitialStartDelay;

        [BoxGroup("Settings/Interstitial")]
        [Tooltip("Delay in seconds between interstitial appearings.")]
        [SerializeField] float interstitialShowingDelay = 30f;
        public float InterstitialShowingDelay => interstitialShowingDelay;

        [BoxGroup("Settings/Delay")]
        [SerializeField] float loadingAdDuration = 0f;
        public float LoadingAdDuration => loadingAdDuration;

        [BoxGroup("Settings/Delay")]
        [SerializeField] string loadingMessage = "Ad is loading..";
        public string LoadingMessage => loadingMessage;

        // Providers
        [SerializeField, Hide] AdDummyContainer dummyContainer;
        public AdDummyContainer DummyContainer => dummyContainer;

        [SerializeField, Hide] ApplovinContainer applovinContainer;
        public ApplovinContainer ApplovinContainer => applovinContainer;

        public bool IsDummyEnabled()
        {
            if (bannerType == AdProvider.Dummy)
                return true;

            if (interstitialType == AdProvider.Dummy)
                return true;

            if (rewardedVideoType == AdProvider.Dummy)
                return true;

            return false;
        }

        public void DisableBanner()
        {
            bannerType = AdProvider.Disable;
        }

        public void DisableInterstitial()
        {
            interstitialType = AdProvider.Disable;
        }
    }
}
