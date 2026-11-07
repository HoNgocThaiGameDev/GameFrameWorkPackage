using UnityEngine;

namespace NebulaSoft
{
    [System.Serializable]
    public class LevelPlayContainer
    {
        [Header("Android")]
        [SerializeField] string androidAppKey;
        public string AndroidAppKey => androidAppKey;

        [Space]
        [SerializeField] string androidBannerID;
        public string AndroidBannerID => androidBannerID;

        [SerializeField] string androidInterstitialID;
        public string AndroidInterstitialID => androidInterstitialID;

        [SerializeField] string androidRVID;
        public string AndroidRVID => androidRVID;

        [Header("iOS")]
        [SerializeField] string iOSAppKey;
        public string IOSAppKey => iOSAppKey;

        [Space]
        [SerializeField] string iOSBannerID;
        public string IOSBannerID => iOSBannerID;

        [SerializeField] string iOSInterstitialID;
        public string IOSInterstitialID => iOSInterstitialID;

        [SerializeField] string iOSRVID;
        public string IOSRVID => iOSRVID;

        [Space]
        [SerializeField] BannerPosition bannerPosition;
        public BannerPosition BannerPosition => bannerPosition;
        [SerializeField] BannerPlacementType bannerType;
        public BannerPlacementType BannerType => bannerType;

        public enum BannerPlacementType
        {
            Banner = 0,
            Large = 1,
            Rectangle = 2,
            Leaderboard = 3
        }
    }
}