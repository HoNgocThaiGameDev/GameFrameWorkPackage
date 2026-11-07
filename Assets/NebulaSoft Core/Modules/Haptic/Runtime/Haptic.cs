using UnityEngine;

namespace NebulaSoft
{
    public enum HapticPriority
    {
        Tap = 0,
        Outcome = 1,
    }

    [StaticUnload]
    public static class Haptic
    {
        public static readonly HapticData HAPTIC_LIGHT = new HapticData(0.045f, 0.2f);
        public static readonly HapticData HAPTIC_MEDIUM = new HapticData(0.07f, 0.35f);
        public static readonly HapticData HAPTIC_HARD = new HapticData(0.1f, 0.45f);

        public static readonly HapticPattern PATTERN_LIGHT = new HapticPattern("light", new HapticEvent[] { new HapticEvent() { Duration = 0.05f, Intensity = 0.2f, Sharpness = 0.0f, StartTime = 0.0f } });

        private const float MIN_PLAY_INTERVAL = 0.09f;

        private static bool isActive;
        public static bool IsActive
        {
            get { return isActive; }
            set
            {
                isActive = value;

                save.IsActive = value;

                SaveController.MarkAsSaveIsRequired();

                if (VerboseLogging)
                    Debug.Log(string.Format("[Haptic]: Haptic state changed: {0}", isActive ? "Active" : "Disabled"));

                StateChanged?.Invoke(value);
            }
        }

        public static bool IsInitialized { get; private set; }
        public static bool VerboseLogging { get; private set; }

        private static readonly BaseHapticWrapper WRAPPER = GetPlatformWrapper();

        private static HapticSave save;

        private static float lastPlayTime = float.MinValue;
        private static HapticPriority lastPlayPriority = HapticPriority.Tap;

        public static event SimpleBoolCallback StateChanged;

        public static void Init()
        {
            if (IsInitialized) return;
            // Get saved state
            save = SaveController.GetSaveObject<HapticSave>("haptic");

            // Set saved state
            isActive = save.IsActive;

            if (WRAPPER == null)
            {
                Debug.LogWarning("[Haptic]: Unsupported platform");

                return;
            }

            // Mark as Initialized
            IsInitialized = true;

            // Initialize platform handler
            WRAPPER.Init();

            // Register default patterns
            WRAPPER.RegisterPattern(PATTERN_LIGHT);
        }

        public static void RegisterPattern(HapticPattern hapticPattern)
        {
            if (WRAPPER == null) return;

            WRAPPER.RegisterPattern(hapticPattern);
        }

        public static void Play(HapticData hapticData)
        {
            Play(hapticData, HapticPriority.Outcome);
        }

        public static void Play(HapticData hapticData, HapticPriority priority)
        {
            if (hapticData == null) return;

            Play(hapticData.Duration, hapticData.Intensity, priority);
        }

        public static void Play(float duration, float intensity = 1.0f)
        {
            Play(duration, intensity, HapticPriority.Outcome);
        }

        public static void Play(float duration, float intensity, HapticPriority priority)
        {
            if (!IsActive) return;

            if (WRAPPER == null) return;

            if (duration <= 0) return;

            if (!CanPlay(priority)) return;

            WRAPPER.Play(duration, Mathf.Clamp01(intensity));
        }

        public static void Play(HapticPattern pattern)
        {
            Play(pattern, HapticPriority.Outcome);
        }

        public static void Play(HapticPattern pattern, HapticPriority priority)
        {
            if (!IsActive) return;

            if (WRAPPER == null) return;

            if (!CanPlay(priority)) return;

            WRAPPER.Play(pattern.ID);
        }

        public static void Play(string patternID)
        {
            Play(patternID, HapticPriority.Outcome);
        }

        public static void Play(string patternID, HapticPriority priority)
        {
            if (!IsActive) return;

            if (WRAPPER == null) return;

            if (!CanPlay(priority)) return;

            WRAPPER.Play(patternID);
        }

        public static void EnableVerboseLogging()
        {
            VerboseLogging = true;
        }

        private static BaseHapticWrapper GetPlatformWrapper()
        {
#if UNITY_EDITOR
            return new EditorHapticWrapper();
#elif UNITY_IOS
            return new IOSHapticWrapper();
#elif UNITY_ANDROID
            return new AndroidHapticWrapper();
#elif UNITY_WEBGL
            return new WebGLHapticWrapper();
#else
            return null;
#endif
        }

        private static bool CanPlay(HapticPriority priority)
        {
            float currentTime = Time.unscaledTime;
            if (currentTime < lastPlayTime + MIN_PLAY_INTERVAL && priority <= lastPlayPriority)
                return false;

            lastPlayTime = currentTime;
            lastPlayPriority = priority;

            return true;
        }

        public static void Shutdown()
        {
            WRAPPER?.Shutdown();
            UnloadStatic();
        }

        private static void UnloadStatic()
        {
            isActive = false;

            IsInitialized = false;
            VerboseLogging = false;

            lastPlayTime = float.MinValue;
            lastPlayPriority = HapticPriority.Tap;

            save = null;

            StateChanged = null;
        }
    }
}
