using UnityEngine;
using System;
using NebulaSoft.Analytics;
using NebulaSoft.Integrations.Firebase;
using UnityEngine.Scripting.APIUpdating;

namespace NebulaSoft
{
    [CreateAssetMenu(menuName = "NebulaSoft/Analytics/Settings", fileName = "Analytics Settings")]
    [MovedFrom(true, "NebulaSoft", "Assembly-CSharp", "AnalyticsModuleSettings")]
    public sealed class AnalyticsSettings : ScriptableObject
    {
        [SerializeField] private AnalyticsBackend provider = AnalyticsBackend.Console;
        [SerializeField, Tooltip("Optional factory asset. When assigned, this replaces the legacy backend selection.")]
        private AnalyticsProviderConfig providerConfig;
        [SerializeField] private AnalyticsBridgeOptions automaticBridges = AnalyticsBridgeOptions.All;
        [SerializeField, Min(1)] private int pendingEventCapacity = 128;
        [Tooltip("Initial collection state. Your application's consent flow can change it through AnalyticsController.")]
        [SerializeField] private bool collectionEnabled = true;
        [SerializeField, Min(1f), Tooltip("Maximum wait for a provider initialization attempt. Retry on resume or through AnalyticsModule.RetryInitialization.")]
        private float initializationTimeoutSeconds = 30f;
        public AnalyticsBackend Provider => provider;
        public bool CollectionEnabled => collectionEnabled;
        public AnalyticsProviderConfig ProviderConfig => providerConfig;
        public AnalyticsBridgeOptions AutomaticBridges => automaticBridges;
        public int PendingEventCapacity => Mathf.Max(1, pendingEventCapacity);
        public float InitializationTimeoutSeconds => Mathf.Max(1f, initializationTimeoutSeconds);

        public IAnalyticsProvider CreateProvider(bool isEditor)
        {
            if (providerConfig != null)
            {
                if (isEditor && !providerConfig.AllowInEditor) return new ConsoleProvider();
                return providerConfig.CreateProvider() ?? throw new InvalidOperationException("Analytics provider config returned null.");
            }
            if (isEditor || provider == AnalyticsBackend.Console) return new ConsoleProvider();
            return FirebaseBackendFactory.CreateAnalytics();
        }
    }
}
