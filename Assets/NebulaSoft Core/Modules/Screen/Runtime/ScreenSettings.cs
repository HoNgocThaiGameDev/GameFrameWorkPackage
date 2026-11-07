#pragma warning disable 0649
#pragma warning disable 0414

using UnityEngine;

namespace NebulaSoft
{
    [RegisterModule("Screen Settings", false)]
    public class ScreenSettings : InitModule
    {
        public override string ModuleName => "Screen Settings";

        [Header("Frame Rate")]
        [SerializeField] bool setFrameRateAutomatically = false;

        [Space]
        [SerializeField] AllowedFrameRates defaultFrameRate = AllowedFrameRates.Rate60;
        [SerializeField] AllowedFrameRates batterySaveFrameRate = AllowedFrameRates.Rate30;

        [Header("Sleep")]
        [SerializeField] int sleepTimeout = -1;

        public override void CreateComponent()
        {
            Screen.sleepTimeout = sleepTimeout;

            if (setFrameRateAutomatically)
            {
                uint numerator = Screen.currentResolution.refreshRateRatio.numerator;
                uint denominator = Screen.currentResolution.refreshRateRatio.denominator;

                if (numerator != 0 && denominator != 0)
                {
                    Application.targetFrameRate = Mathf.RoundToInt(numerator / denominator);
                }
                else
                {
                    Application.targetFrameRate = (int)defaultFrameRate;
                }
            }
            else
            {
#if UNITY_IOS
                if(UnityEngine.iOS.Device.lowPowerModeEnabled)
                {
                    Application.targetFrameRate = (int)batterySaveFrameRate;
                }
                else
                {
                    Application.targetFrameRate = (int)defaultFrameRate;
                }    
#else
                Application.targetFrameRate = (int)defaultFrameRate;
#endif
            }
        }

        private enum AllowedFrameRates
        {
            Rate30 = 30,
            Rate60 = 60,
            Rate90 = 90,
            Rate120 = 120,
        }
    }
}
