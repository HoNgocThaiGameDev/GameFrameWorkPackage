using System;

namespace NebulaSoft
{
    [Flags]
    public enum AnalyticsBridgeOptions
    {
        None = 0,
        Ads = 1,
        IAP = 2,
        UI = 4,
        All = Ads | IAP | UI
    }
}
