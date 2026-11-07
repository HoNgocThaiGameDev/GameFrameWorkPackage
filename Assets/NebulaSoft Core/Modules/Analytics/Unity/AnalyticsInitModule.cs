using UnityEngine;

namespace NebulaSoft
{
    [RegisterModule("Analytics", order: 750)]
    public sealed class AnalyticsInitModule : InitModule
    {
        [SerializeField] private AnalyticsSettings settings;
        [System.NonSerialized] private AnalyticsModule runtimeModule;
        public AnalyticsSettings Settings => settings;
        public override string ModuleName => "Analytics";
        public override void CreateComponent()
        {
            if (settings == null) { Debug.LogWarning("[Analytics] Assign Analytics Settings in Project Init Settings."); return; }
            runtimeModule = Initializer.GameObject.GetComponent<AnalyticsModule>();
            if (runtimeModule == null) runtimeModule = Initializer.GameObject.AddComponent<AnalyticsModule>();
            runtimeModule.Configure(settings);
        }

        public override void Shutdown()
        {
            if (runtimeModule != null) runtimeModule.Shutdown();
            runtimeModule = null;
        }
    }
}
