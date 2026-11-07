using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace NebulaSoft
{
    public sealed class WebGLHapticWrapper : BaseHapticWrapper
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void _Initialize();

        [DllImport("__Internal")]
        private static extern void _Play(int duration);

        [DllImport("__Internal")]
        private static extern void _PlayPattern(string patternID, int patternIDLength);

        [DllImport("__Internal")]
        private static extern void _RegisterPattern(string patternID, int patternIDLength, int[] pattern, int patternLength);
#endif

        public override void Init()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (!Application.isMobilePlatform) return;

            Try(() => _Initialize(), "Failed to Initialize!");
#endif
        }

        public override void Play(float duration = 0.3f, float intensity = 1.0f)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (!Application.isMobilePlatform) return;

            Try(() => _Play((int)(duration * 1000)), "Failed to play haptic!");
#endif
        }

        public override void Play(string patternID)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (!Application.isMobilePlatform) return;

            Try(() => _PlayPattern(patternID, patternID.Length), string.Format("Failed to play pattern with ID: {0}!", patternID));
#endif
        }

        public override void RegisterPattern(HapticPattern pattern)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (!Application.isMobilePlatform) return;

            int[] convertedPattern = ConvertToVibrationPattern(pattern);

            Try(() => _RegisterPattern(pattern.ID, pattern.ID.Length, convertedPattern, convertedPattern.Length), string.Format("Failed to register pattern with ID: {0}!", pattern.ID));
#endif
        }

        private int[] ConvertToVibrationPattern(HapticPattern hapticPattern)
        {
            if (hapticPattern == null || hapticPattern.Pattern.IsNullOrEmpty())
                return new int[0];

            List<int> patternList = new List<int>();

            HapticEvent[] hapticEvents = hapticPattern.Pattern;
            for (int i = 0; i < hapticEvents.Length; i++)
            {
                HapticEvent currentEvent = hapticEvents[i];

                // If this is not the first event, add the pause duration between the previous event and this one
                if (i > 0)
                {
                    float previousEndTime = hapticEvents[i - 1].StartTime + hapticEvents[i - 1].Duration;
                    int pauseDuration = Mathf.RoundToInt((currentEvent.StartTime - previousEndTime) * 1000f);
                    if (pauseDuration > 0)
                    {
                        patternList.Add(pauseDuration);
                    }
                }

                // Add the vibration duration
                int vibrationDuration = Mathf.RoundToInt(currentEvent.Duration * 1000f);
                patternList.Add(vibrationDuration);
            }

            return patternList.ToArray();
        }
    }
}
