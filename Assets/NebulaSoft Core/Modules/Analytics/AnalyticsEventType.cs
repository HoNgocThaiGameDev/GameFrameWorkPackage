#define DEBUG_LOGS

namespace NebulaSoft
{
    public enum AnalyticsEventType
    {
        // Core Events
        CurrencySource = 10,
        CurrencySink = 11,

        IAPClicked = 20,
        IAPPurchased = 21,
        IAPFailed = 22,
        IAPFirstPurchase = 23,

        AdFreePeriodExpired = 25,

        RVClicked = 26,
        InterstitialDisplayed = 27,
    }
}
