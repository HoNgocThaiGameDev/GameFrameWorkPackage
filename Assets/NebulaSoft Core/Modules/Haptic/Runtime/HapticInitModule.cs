using UnityEngine;

namespace NebulaSoft
{
    [RegisterModule("Haptic")]
    public class HapticInitModule : InitModule
    {
        public override System.Type[] Dependencies => new[] { typeof(SaveInitModule) };
        public override void Shutdown() => Haptic.Shutdown();
        [SerializeField] bool verboseLogging = false;

        public override string ModuleName => "Haptic";

        public override void CreateComponent()
        {
            if (verboseLogging)
                Haptic.EnableVerboseLogging();

            Haptic.Init();
        }
    }
}
