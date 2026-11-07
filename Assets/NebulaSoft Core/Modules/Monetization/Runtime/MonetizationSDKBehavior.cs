using UnityEngine;

namespace NebulaSoft
{
    public sealed class MonetizationSDKBehavior : SDKBehavior
    {
        public override void Shutdown()
        {
            AdsManager.Shutdown();
            IAPManager.Shutdown();
        }
        [SerializeField] MonetizationSettings settings;

        public override void OnUserConsentReceived()
        {
            Monetization.Init(settings);

            AdsManager.Init(settings);
            IAPManager.Init(settings);
        }
    }
}
