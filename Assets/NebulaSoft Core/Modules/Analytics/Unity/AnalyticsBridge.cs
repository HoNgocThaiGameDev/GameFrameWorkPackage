using System;
using NebulaSoft.Ads;
using NebulaSoft.Analytics;

namespace NebulaSoft
{
    /// <summary>Connects Ads, IAP and UI callbacks to analytics events; show requests are not impressions.</summary>
    public sealed class AnalyticsBridge : IDisposable
    {
        private readonly AnalyticsEvents events;
        private IAdsService ads;
        private string pendingPurchase;

        public AnalyticsBridge(AnalyticsEvents events)
        {
            this.events = events;
            AdsManager.Configured += AttachAds;
            IAPManager.StateChanged += OnPurchaseState;
            IAPManager.ProductPurchaseCompleted += OnPurchaseCompleted;
            IAPManager.ProductPurchaseFailed += OnPurchaseFailed;
            UIController.PageOpened += OnPageOpened;
            AttachAds();
        }

        private void AttachAds()
        {
            if (ads != null) ads.AdEvent -= OnAdEvent;
            ads = AdsManager.Service;
            if (ads != null) ads.AdEvent += OnAdEvent;
        }

        public void OnAdEvent(AdEvent value)
        {
            string format = value.Format.ToString().ToLowerInvariant();
            switch (value.Kind)
            {
                case AdEventKind.Started: events.AdShown(value.PlacementId, format, value.ProviderId); break;
                case AdEventKind.Impression: events.AdImpression(value.PlacementId, format, value.ProviderId); break;
                case AdEventKind.Clicked: events.AdClicked(value.PlacementId, format); break;
                case AdEventKind.RewardGranted: events.AdRewarded(value.PlacementId); break;
                case AdEventKind.Closed: events.AdClosed(value.PlacementId, format); break;
                case AdEventKind.ShowFailed: events.AdFailed(value.PlacementId, format, "show", value.Error); break;
                case AdEventKind.LoadFailed: events.AdFailed(value.PlacementId, format, "load", value.Error); break;
            }
        }

        private void OnPurchaseState()
        {
            string key = IAPManager.ActivePurchaseKey;
            if (string.IsNullOrEmpty(key)) { pendingPurchase = null; return; }
            if (pendingPurchase == key) return;
            pendingPurchase = key;
            events.IapAttempt(key);
        }
        private void OnPurchaseCompleted(string key) => events.IapCompleted(key);
        private void OnPurchaseFailed(string key, PurchaseFailureReason reason) => events.IapFailed(key, reason.ToString());
        private void OnPageOpened(UIPage page, Type pageType) => events.ScreenView(pageType.Name, pageType.FullName);

        public void Dispose()
        {
            if (ads != null) ads.AdEvent -= OnAdEvent;
            ads = null;
            AdsManager.Configured -= AttachAds;
            IAPManager.StateChanged -= OnPurchaseState;
            IAPManager.ProductPurchaseCompleted -= OnPurchaseCompleted;
            IAPManager.ProductPurchaseFailed -= OnPurchaseFailed;
            UIController.PageOpened -= OnPageOpened;
        }
    }
}
