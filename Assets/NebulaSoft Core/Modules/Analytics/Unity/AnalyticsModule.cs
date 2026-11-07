using System;
using System.Threading;
using NebulaSoft.Analytics;
using UnityEngine;

namespace NebulaSoft
{
    /// <summary>Owns initialization and framework integrations for the application lifetime.</summary>
    public sealed class AnalyticsModule : MonoBehaviour
    {
        private CancellationTokenSource lifetime;
        private AnalyticsBridge frameworkBridge;
        private IAnalyticsService service;
        private AnalyticsSettings configuration;
        private bool initializing;

        public void Configure(AnalyticsSettings settings)
        {
            if (lifetime != null) return;
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            lifetime = new CancellationTokenSource();
            configuration = settings;
            service = AnalyticsController.ConfigureService(settings.PendingEventCapacity);
            service.SetCollectionEnabled(settings.CollectionEnabled);
            frameworkBridge = new AnalyticsBridge(AnalyticsController.Events, settings.AutomaticBridges);
            InitializeProvider();
        }

        private async void InitializeProvider()
        {
            if (lifetime == null || initializing || service.IsReady) return;
            var owner = lifetime;
            var activeService = service;
            var settings = configuration;
            initializing = true;
            try
            {
                IAnalyticsProvider provider = settings.CreateProvider(Application.isEditor);
                bool ready;
                using (var attempt = CancellationTokenSource.CreateLinkedTokenSource(owner.Token))
                {
                    attempt.CancelAfter(TimeSpan.FromSeconds(settings.InitializationTimeoutSeconds));
                    ready = await activeService.InitializeAsync(provider, attempt.Token);
                }
                if (!ready && lifetime == owner && !owner.IsCancellationRequested)
                    Debug.LogWarning("[Analytics] Provider initialization failed. Events remain in the bounded startup queue.");
            }
            catch (Exception exception) { Debug.LogWarning("[Analytics] " + exception.Message); }
            finally { if (lifetime == owner) initializing = false; }
        }

        private void OnApplicationPause(bool paused)
        {
            if (!paused) InitializeProvider();
        }

        public void RetryInitialization() => InitializeProvider();

        public void Shutdown()
        {
            if (lifetime == null) return;
            lifetime.Cancel();
            frameworkBridge.Dispose();
            frameworkBridge = null;
            AnalyticsController.Shutdown(service);
            lifetime.Dispose();
            lifetime = null;
            service = null;
            configuration = null;
            initializing = false;
        }
        private void OnDestroy() => Shutdown();
    }
}
