using System.Collections.Generic;

namespace NebulaSoft
{
    public class CurrencyEvent : IAnalyticsEventData
    {
        public string Source;
        public Dictionary<CurrencyType, int> CurrenciesDelta;
    }
}
