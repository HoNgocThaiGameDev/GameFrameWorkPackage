using UnityEngine;

namespace NebulaSoft
{
    /// <summary>Game UI, analytics and cloud entitlement wiring. No Ads SDK types belong here.</summary>
    public sealed class AdsGameBridge : MonoBehaviour
    {
        private void OnEnable()
        {
            AdsManager.Configured += OnConfigured;
            AdsManager.RewardedVideoConditions += CanShowRewarded;
            AdsManager.ErrorMessageRequested += OnError;
            AdsManager.LoadingPresentationChanged += OnLoading;
            AdsManager.AnalyticsRequested += OnAnalytics;
            AdsManager.BannerLayoutChanged += OnBanner;
            AdDummyController.NoAdsOfferRequested += OpenNoAdsOffer;
            if (AdsManager.Service != null) OnConfigured();
        }

        private void OnDisable()
        {
            AdsManager.Configured -= OnConfigured;
            AdsManager.RewardedVideoConditions -= CanShowRewarded;
            AdsManager.ErrorMessageRequested -= OnError;
            AdsManager.LoadingPresentationChanged -= OnLoading;
            AdsManager.AnalyticsRequested -= OnAnalytics;
            AdsManager.BannerLayoutChanged -= OnBanner;
            AdDummyController.NoAdsOfferRequested -= OpenNoAdsOffer;
        }

        private void OnConfigured() => FirebaseNoAdsEntitlementSync.ApplyPendingCloudState();
        private bool CanShowRewarded()
        {
            if (!LeaderboardBottomNavigationController.IsNoConnectionActive()) return true;
            LeaderboardBottomNavigationController.TryShowNoConnectionPopup();
            return false;
        }
        private void OnError() => SystemMessage.ShowMessage("Network error. Please try again later");
        private void OnBanner(BannerLayoutInfo info) => NotchSaveArea.Refresh(true);
        private void OnLoading(bool visible, string message)
        {
            if (visible) { SystemMessage.ShowLoadingPanel(); SystemMessage.ChangeLoadingMessage(message); }
            else SystemMessage.HideLoadingPanel();
        }
        private void OnAnalytics(string placement, bool rewarded)
        {
            if (rewarded) AnalyticsController.OnRVClicked(placement);
            else AnalyticsController.OnInterstitialDisplayed(placement);
        }

        public void OpenNoAdsOffer()
        {
            AdsManager.HideBanner();
            var offer = FindFirstObjectByType<UINoAdsOffer>(FindObjectsInactive.Include);
            if (offer == null) { AdsManager.ShowBanner(); return; }
            if (offer.Canvas == null) offer.CacheComponents();
            offer.Canvas.overrideSorting = true;
            offer.Canvas.sortingOrder = 1000;
            if (offer.IsPageDisplayed && !offer.Canvas.enabled) offer.IsPageDisplayed = false;
            UIController.ShowPage(offer);
        }
    }
}
