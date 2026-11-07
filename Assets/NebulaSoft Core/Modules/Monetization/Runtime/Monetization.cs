namespace NebulaSoft
{
    public static class Monetization
    {
        public static bool IsActive { get; private set; }
        public static bool DebugMode { get; private set; }
        public static bool VerboseLogging { get; private set; }

        public static MonetizationSettings Settings { get; private set; }

        public static AdsSettings AdsSettings => Settings.AdsSettings;
        public static IAPSettings IAPSettings => Settings.IAPSettings;

        public static void Init(MonetizationSettings settings)
        {
            Settings = settings;

            UpdateData(settings);
        }

        public static void UpdateData(MonetizationSettings settings)
        {
            IsActive = settings.IsModuleActive;
            DebugMode = settings.DebugMode;
            VerboseLogging = settings.VerboseLogging;
        }
    }
}
