using System;
using System.Reflection;
using GoogleMobileAds.Editor;
using UnityEditor;
using UnityEditor.Build.Reporting;
using VoxelBusters.AdsKit.Editor;
using VoxelBusters.CoreLibrary;

namespace VoxelBusters.AdsKit.Adapters.Editor
{
    public class AdMobSettingsSyncer : AdNetworkSettingsSyncer
    {

        protected override string GetNetworkId() => AdNetworkServiceId.kGoogleMobileAds;

        protected override void Sync(string apiKey, string apiSecret)
        {
            if (!string.IsNullOrEmpty(apiKey))
            {
                var settingsProxy = new GoogleAdMobSettingsProxy();

                if (settingsProxy != null)
                {

#if UNITY_ANDROID
                    settingsProxy.AndroidAppId = apiKey;
#elif UNITY_IOS
                    settingsProxy.IOSAppId      = apiKey;
#else
                    var propertyName = "";    
#endif
                    settingsProxy.Save();
                }
                else
                {
                    DebugLogger.LogError("Unable to fetch admob settings via reflection. Contact AdsKit plugin support with your AdMob version.");
                }
            }
        }
    }
}
