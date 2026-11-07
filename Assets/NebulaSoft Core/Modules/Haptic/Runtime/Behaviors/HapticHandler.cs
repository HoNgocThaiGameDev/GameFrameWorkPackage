using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NebulaSoft
{
    [System.Serializable]
    public class HapticHandler
    {
        [SerializeField] float duration = 0.1f;

        [Slider(0.0f, 1.0f)]
        [SerializeField] float intensity = 0.0f;

        [SerializeField] bool advancedSettings;

        [ShowIf("advancedSettings")]
        [SerializeField] float minDelay;

        [ShowIf("advancedSettings")]
        [SerializeField] bool dynamicIntensity;
        [ShowIf("advancedSettings")]
        [SerializeField] DuoFloat intensityRange = new DuoFloat(0.2f, 1.0f);
        [ShowIf("advancedSettings")]
        [SerializeField] int intensitySteps = 10;
        [ShowIf("advancedSettings")]
        [SerializeField] float intensityResetTime = 1.0f;

        private float lastPlayedTime = float.MinValue;

        private int currentIntensityStep;
        private float lastIntensityStepTime;

        public void Play()
        {
            if (advancedSettings)
            {
                if (Time.timeSinceLevelLoad < lastPlayedTime + minDelay) return;

                lastPlayedTime = Time.timeSinceLevelLoad;

                if (dynamicIntensity)
                {
                    currentIntensityStep++;

                    if (Time.timeSinceLevelLoad > lastIntensityStepTime)
                        currentIntensityStep = 0;

                    lastIntensityStepTime = Time.timeSinceLevelLoad + intensityResetTime;

                    Haptic.Play(duration, intensityRange.Lerp((float)currentIntensityStep / intensitySteps));
                }
                else
                {
                    Haptic.Play(duration, intensity);
                }
            }
            else
            {
                Haptic.Play(duration, intensity);
            }
        }
    }
}
