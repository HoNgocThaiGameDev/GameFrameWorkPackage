using NebulaSoft.Analytics;
using NebulaSoft.Integrations.Firebase;
using UnityEngine;

namespace NebulaSoft
{
    [CreateAssetMenu(menuName = "NebulaSoft/Analytics/Providers/Firebase")]
    public sealed class FirebaseAnalyticsProviderConfig : AnalyticsProviderConfig
    {
        public override IAnalyticsProvider CreateProvider() => FirebaseBackendFactory.CreateAnalytics();
    }
}
