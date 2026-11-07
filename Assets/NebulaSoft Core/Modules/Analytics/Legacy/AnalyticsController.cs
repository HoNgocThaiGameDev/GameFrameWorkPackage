using System;
using System.Collections.Generic;
using UnityEngine;

namespace NebulaSoft
{
    [StaticUnload]
    public static partial class AnalyticsController
    {
        public static event AnalyticsEventCallback EventFired;

        public static void OnCurrencySource(string source, Dictionary<CurrencyType, int> currenciesDelta)
        {
            CurrencyEvent analyticsCurrencyData = new CurrencyEvent();
            analyticsCurrencyData.Source = source;
            analyticsCurrencyData.CurrenciesDelta = currenciesDelta;

            TrackEvent(AnalyticsEventType.CurrencySource, analyticsCurrencyData);
        }

        public static void OnCurrencySink(string sink, Dictionary<CurrencyType, int> currenciesDelta)
        {
            CurrencyEvent analyticsCurrencyData = new CurrencyEvent();
            analyticsCurrencyData.Source = sink;
            analyticsCurrencyData.CurrenciesDelta = currenciesDelta;

            TrackEvent(AnalyticsEventType.CurrencySink, analyticsCurrencyData);
        }

        public static void OnIAPClicked(IAPItem item)
        {
            PurchaseEvent analyticsData = new PurchaseEvent();
            analyticsData.Item = item;

            TrackEvent(AnalyticsEventType.IAPClicked, analyticsData);
        }

        public static void OnRVClicked(string source)
        {
            RewardedAdEvent analyticsData = new RewardedAdEvent();
            analyticsData.Source = source;

            TrackEvent(AnalyticsEventType.RVClicked, analyticsData);
        }

        public static void OnInterstitialDisplayed(string source)
        {
            InterstitialEvent analyticsData = new InterstitialEvent();
            analyticsData.Source = source;

            TrackEvent(AnalyticsEventType.InterstitialDisplayed, analyticsData);
        }

#if MODULE_IAP
        public static void OnIAPPurchased(PurchaseEvent analyticsIAPData)
        {
            TrackEvent(AnalyticsEventType.IAPPurchased, analyticsIAPData);
        }

        public static void OnIAPFailed(IAPItem item, NebulaSoft.PurchaseFailureReason failureReason)
        {
            PurchaseFailEvent analyticsData = new PurchaseFailEvent();
            analyticsData.Item = item;
            analyticsData.FailureReason = failureReason;

            TrackEvent(AnalyticsEventType.IAPFailed, analyticsData);
        }
#endif

        public static void TrackEvent(AnalyticsEventType analyticsEventType, IAnalyticsEventData eventData = null)
        {
            AnalyticsLegacyBridge.Track(analyticsEventType, eventData, Events);
#if DEBUG_LOGS
            Debug.Log(string.Format("[Analytics]: Event <b>\"{0}\"</b> fired.", analyticsEventType));

#endif

            if (EventFired != null)
                foreach (AnalyticsEventCallback callback in EventFired.GetInvocationList())
                    try { callback(analyticsEventType, eventData); }
                    catch (Exception exception) { Debug.LogException(exception); }
        }

        private static void UnloadStatic()
        {
            Shutdown();
            EventFired = null;
        }

        public delegate void AnalyticsEventCallback(AnalyticsEventType type, IAnalyticsEventData analyticsEventData);
    }
}
