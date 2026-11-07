using NebulaSoft.Analytics;
using UnityEngine;

namespace NebulaSoft
{
    [CreateAssetMenu(menuName = "NebulaSoft/Analytics/Providers/Console")]
    public sealed class ConsoleAnalyticsProviderConfig : AnalyticsProviderConfig
    {
        public override bool AllowInEditor => true;
        public override IAnalyticsProvider CreateProvider() => new ConsoleProvider();
    }
}
