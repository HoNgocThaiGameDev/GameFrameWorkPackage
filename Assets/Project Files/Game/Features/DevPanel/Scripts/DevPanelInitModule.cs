using UnityEngine;

namespace NebulaSoft
{
    [RegisterModule("Dev Panel")]
    public class DevPanelInitModule : InitModule
    {
        public override string ModuleName => "Dev Panel";

        [SerializeField] DevPanelSettings settings;
        public DevPanelSettings Settings => settings;

        public override void CreateComponent()
        {
            if (settings == null)
            {
                Debug.LogError("Dev Panel settings is not set!", this);

                return;
            }

            DevPanelEnabler.LinkSettings(settings);
        }
    }
}
