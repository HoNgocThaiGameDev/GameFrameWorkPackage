namespace NebulaSoft
{
    [StaticUnload]
    public static class DevPanelEnabler
    {
        private static DevPanelSettings settings;
        public static bool IsActive { get; private set; }

        public static SimpleBoolCallback StateChanged;

        static DevPanelEnabler()
        {
            if(settings != null)
            {
                IsActive = settings.IsEnabled;
            }
            else
            {
                IsActive = false;
            }
        }

        public static void LinkSettings(DevPanelSettings settings)
        {
            DevPanelEnabler.settings = settings;

            if (settings != null)
            {
                IsActive = settings.IsEnabled;
            }
            else
            {
                IsActive = false;
            }
        }

        public static void UpdateState()
        {
            if (settings == null) return;

            if (IsActive != settings.IsEnabled)
            {
                IsActive = settings.IsEnabled;

                StateChanged?.Invoke(IsActive);
            }
        }

        private static void UnloadStatic()
        {
            IsActive = false;

            settings = null;
            StateChanged = null;
        }
    }
}