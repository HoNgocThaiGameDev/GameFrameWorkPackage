using NebulaSoft.Ads;
using UnityEngine;

namespace NebulaSoft
{
    [RegisterModule("Monetization/Ads")]
    public sealed class AdsInitModule : InitModule
    {
        [SerializeField] AdsModuleSettings settings;
        public AdsModuleSettings Settings => settings;
        public override string ModuleName => "Ads";
        public override void CreateComponent() => AdsManager.ConfigureModule(settings);
    }
}
