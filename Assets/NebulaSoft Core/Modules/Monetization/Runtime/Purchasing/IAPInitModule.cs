using UnityEngine;

namespace NebulaSoft
{
    [RegisterModule("Monetization/IAP")]
    public sealed class IAPInitModule : InitModule
    {
        [SerializeField] IAPSettings settings;
        public IAPSettings Settings => settings;
        public override string ModuleName => "IAP";
        public override System.Type[] Dependencies => new[] { typeof(SaveInitModule) };
        public override void Shutdown() => IAPManager.Shutdown();

        public override void CreateComponent()
        {
            // Configure here; MonetizationSDKBehavior connects after consent.
            IAPManager.ConfigureModule(settings);
        }
    }
}
