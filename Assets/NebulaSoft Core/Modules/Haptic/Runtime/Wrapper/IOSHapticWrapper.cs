using System.Runtime.InteropServices;
using UnityEngine;

namespace NebulaSoft
{
    public sealed class IOSHapticWrapper : BaseHapticWrapper
    {
#if UNITY_IOS
        [DllImport("__Internal")]
        private static extern void _Initialize();

        [DllImport("__Internal")]
        private static extern void _Play(float duration, float intensity);

        [DllImport("__Internal")]
        private static extern void _PlayPattern(string patternID);

        [DllImport("__Internal")]
        private static extern void _RegisterPattern(string jsonPatternData);
#endif

        public override void Init()
        {
#if UNITY_IOS
            Try(() => _Initialize(), "Failed to Initialize module!");
#endif
        }

        public override void Play(float duration = 0.3f, float intensity = 1.0f)
        {
#if UNITY_IOS
            Try(() => _Play(duration, intensity), "Failed to play haptic!");
#endif
        }

        public override void Play(string patternID)
        {
#if UNITY_IOS
            Try(() => _PlayPattern(patternID), string.Format("Failed to play pattern: {0}!", patternID));
#endif
        }

        public override void RegisterPattern(HapticPattern pattern)
        {
            string json = JsonUtility.ToJson(pattern);

            if (string.IsNullOrEmpty(json))
            {
                Debug.LogWarning(string.Format("[Haptic]: Failed to register pattern with ID: {0}!", pattern.ID));

                return;
            }

#if UNITY_IOS
            Try(() => _RegisterPattern(json), string.Format("Failed to register pattern with ID: {0}!", pattern.ID));
#endif
        }
    }
}
