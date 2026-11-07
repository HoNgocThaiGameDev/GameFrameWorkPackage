using System.Collections.Generic;
using UnityEngine;

namespace NebulaSoft
{
    public sealed class AndroidHapticWrapper : BaseHapticWrapper
    {
        private Dictionary<int, AndroidHapticPattern> registeredPatterns;

        private AndroidJavaObject vibrationService;
        private int sdkVersion;
        public override void Shutdown()
        {
            if (vibrationService != null)
            {
                Try(() => vibrationService.Call("cancel"), "Failed to stop haptic playback.");
                vibrationService.Dispose();
                vibrationService = null;
            }
            registeredPatterns?.Clear();
        }

        public override void Init()
        {
            registeredPatterns = new Dictionary<int, AndroidHapticPattern>();

#if UNITY_ANDROID
            Try(() =>
            {
                using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                {
                    AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

                    vibrationService = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                }

                // Get the Android SDK version
                using (AndroidJavaClass versionClass = new AndroidJavaClass("android.os.Build$VERSION"))
                {
                    sdkVersion = versionClass.GetStatic<int>("SDK_INT");
                }
            }, "Failed to Initialize haptic module!");
#endif
        }

        public override void Play(float duration = 0.3f, float intensity = 1.0f)
        {
            if (vibrationService == null) return;

#if UNITY_ANDROID
            Try(() =>
            {
                if (sdkVersion >= 26)
                {
                    using (AndroidJavaClass vibrationEffectClass = new AndroidJavaClass("android.os.VibrationEffect"))
                    {
                        AndroidJavaObject vibrationEffect = vibrationEffectClass.CallStatic<AndroidJavaObject>("createOneShot", (long)(duration * 1000), (int)Mathf.Lerp(1, 255, Mathf.Clamp01(intensity)));

                        vibrationService.Call("vibrate", vibrationEffect);
                    }
                }
                else
                {
                    vibrationService.Call("vibrate", duration);
                }
            }, "Failed to play haptic!");
#endif
        }

        public override void Play(string patternID)
        {
            int patternHash = patternID.GetHashCode();

            if (!registeredPatterns.ContainsKey(patternHash)) return;

            if (vibrationService == null) return;

#if UNITY_ANDROID
            Try(() =>
            {
                AndroidHapticPattern androidHapticPattern = registeredPatterns[patternHash];

                if (sdkVersion >= 26)
                {
                    using (AndroidJavaClass vibrationEffectClass = new AndroidJavaClass("android.os.VibrationEffect"))
                    {
                        AndroidJavaObject vibrationEffect = vibrationEffectClass.CallStatic<AndroidJavaObject>("createWaveform", androidHapticPattern.Pattern, androidHapticPattern.Amplitudes, -1);

                        vibrationService.Call("vibrate", vibrationEffect);
                    }
                }
                else
                {
                    vibrationService.Call("vibrate", androidHapticPattern.Pattern, -1);
                }
            }, string.Format("Failed to play pattern with ID: {0}!", patternID));
#endif
        }

        public override void RegisterPattern(HapticPattern pattern)
        {
            int patternHash = pattern.ID.GetHashCode();

            if (registeredPatterns.ContainsKey(patternHash)) return;

            registeredPatterns.Add(patternHash, new AndroidHapticPattern(pattern));
        }

        private class AndroidHapticPattern
        {
            public long[] Pattern { get; private set; }
            public int[] Amplitudes { get; private set; }

            public AndroidHapticPattern(HapticPattern hapticPattern)
            {
                List<long> patternList = new List<long>();
                List<int> amplitudeList = new List<int>();

                float previousEndTime = 0f;

                foreach (HapticEvent hapticEvent in hapticPattern.Pattern)
                {
                    // Calculate the delay (pause) before the current haptic event
                    if (hapticEvent.StartTime > previousEndTime)
                    {
                        patternList.Add((long)((hapticEvent.StartTime - previousEndTime) * 1000)); // convert to milliseconds
                        amplitudeList.Add(0); // no vibration during the pause
                    }

                    // Add the duration of the haptic event
                    long duration = (long)(hapticEvent.Duration * 1000); // convert to milliseconds
                    patternList.Add(duration);

                    // Convert intensity to a value between 0 and 255
                    int intensity = (int)Mathf.Lerp(1, 255, Mathf.Clamp01(hapticEvent.Intensity));
                    amplitudeList.Add(intensity);

                    // Update previousEndTime
                    previousEndTime = hapticEvent.StartTime + hapticEvent.Duration;
                }

                // Convert lists to arrays
                Pattern = patternList.ToArray();
                Amplitudes = amplitudeList.ToArray();
            }
        }
    }
}
