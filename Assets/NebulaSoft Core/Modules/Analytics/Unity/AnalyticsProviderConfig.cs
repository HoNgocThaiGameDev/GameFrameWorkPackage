using NebulaSoft.Analytics;
using UnityEngine;

namespace NebulaSoft
{
    public abstract class AnalyticsProviderConfig : ScriptableObject
    {
        public virtual bool AllowInEditor => false;
        public abstract IAnalyticsProvider CreateProvider();
    }
}
