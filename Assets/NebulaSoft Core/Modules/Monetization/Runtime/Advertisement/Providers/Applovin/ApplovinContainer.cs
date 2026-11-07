using UnityEngine;

namespace NebulaSoft
{
    [System.Serializable]
    [Define("MODULE_APPLOVIN", "MaxSdk")]
    public class ApplovinContainer
    {
        //Banned ID
        [Header("Banner ID")]
        [SerializeField] string androidBannerID = "banner";
        public string AndroidBannerID => androidBannerID;
        [SerializeField] string iOSBannerID = "banner";
        public string IOSBannerID => iOSBannerID;

        //Interstitial ID
        [Header("Interstitial ID")]
        [SerializeField] string androidInterstitialID = "video";
        public string AndroidInterstitialID => androidInterstitialID;
        [SerializeField] string iOSInterstitialID = "video";
        public string IOSInterstitialID => iOSInterstitialID;

        //Rewarder Video ID
        [Header("Rewarded Video ID")]
        [SerializeField] string androidRewardedVideoID = "rewardedVideo";
        public string AndroidRewardedVideoID => androidRewardedVideoID;
        [SerializeField] string iOSRewardedVideoID = "rewardedVideo";
        public string IOSRewardedVideoID => iOSRewardedVideoID;

        [Space]
        [SerializeField] BannerPositionAL bannerPosition;
        public BannerPositionAL BannerPosition => bannerPosition;

        [SerializeField] bool useAdaptiveBanner = true;
        public bool UseAdaptiveBanner => useAdaptiveBanner;

        [SerializeField] Color bannerBackgroundColor = Color.white;
        public Color BannerBackgroundColor => bannerBackgroundColor;

        public enum BannerPositionAL
        {
            TopLeft,
            TopCenter,
            TopRight,
            Centered,
            CenterLeft,
            CenterRight,
            BottomLeft,
            BottomCenter,
            BottomRight
        }
    }
}