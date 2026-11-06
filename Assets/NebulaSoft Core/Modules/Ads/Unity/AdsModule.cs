using System;
using System.Threading;
using System.Threading.Tasks;
using NebulaSoft.Ads;
using UnityEngine;

namespace NebulaSoft
{
    /// <summary>Application lifetime owner. Consent is supplied by the existing SDKInitializer before InitializeAsync.</summary>
    public sealed class AdsModule : MonoBehaviour
    {
        private CancellationTokenSource lifetime;
        private AdsService service;
        public IAdsService Service => service;
        public AdsSaveBridge Save { get; private set; }
        public AdsModuleSettings Settings { get; private set; }
        public string ProviderId { get; private set; }

        public void Configure(AdsModuleSettings settings)
        {
            Settings = settings;
            lifetime = new CancellationTokenSource();
            var provider = settings.Provider.CreateProvider();
            ProviderId = provider.Id;
            service = new AdsService(provider, settings.Catalog.CreateDefinitions(),
                () => Time.realtimeSinceStartupAsDouble, Debug.LogException);
            Save = new AdsSaveBridge(service, () => TimeUtils.GetCurrentUnixTimestamp());
        }

        public Task<bool> InitializeAsync() => service.InitializeAsync(lifetime.Token);

        private void Update()
        {
            if (service == null) return;
            Save.Tick();
            service.Tick();
            AdsManager.TickCompatibility();
        }

        public void Shutdown()
        {
            if (service == null) return;
            lifetime.Cancel();
            service.Dispose();
            service = null;
            lifetime.Dispose();
            lifetime = null;
        }

        private void OnDestroy() => Shutdown();
    }
}
