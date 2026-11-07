using NebulaSoft.Analytics;

namespace NebulaSoft
{
    internal static class AnalyticsLegacyBridge
    {
        public static void Track(AnalyticsEventType type, IAnalyticsEventData data, AnalyticsEvents events)
        {
            if (data is CurrencyEvent currency && currency.CurrenciesDelta != null)
                foreach (var pair in currency.CurrenciesDelta)
                {
                    if (type == AnalyticsEventType.CurrencySource) events.EarnCurrency(pair.Key.ToString(), pair.Value, currency.Source);
                    else if (type == AnalyticsEventType.CurrencySink) events.SpendCurrency(pair.Key.ToString(), pair.Value, currency.Source);
                }
            // IAP and ads use their authoritative service callbacks in AnalyticsBridge.
            // Legacy request hooks do not prove that a purchase or impression actually occurred.
            if (type == AnalyticsEventType.IAPFirstPurchase)
                AnalyticsController.Track(new AnalyticsEvent("first_iap_purchase"));
            else if (type == AnalyticsEventType.AdFreePeriodExpired)
                AnalyticsController.Track(new AnalyticsEvent("ad_free_period_expired"));
        }
    }
}
