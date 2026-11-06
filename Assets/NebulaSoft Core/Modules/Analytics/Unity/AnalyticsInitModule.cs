using UnityEngine;

namespace NebulaSoft
{
    [RegisterModule("Analytics", order: 750)]
    public sealed class AnalyticsInitModule : InitModule
    {
        [SerializeField] private AnalyticsSettings settings;
        public AnalyticsSettings Settings => settings;
        public override string ModuleName => "Analytics";
        public override void CreateComponent()
        {
            if (settings == null) { Debug.LogWarning("[Analytics] Assign Analytics Settings in Project Init Settings."); return; }
            var module = Initializer.GameObject.GetComponent<AnalyticsModule>();
            if (module == null) module = Initializer.GameObject.AddComponent<AnalyticsModule>();
            module.Configure(settings);
        }
    }
}
