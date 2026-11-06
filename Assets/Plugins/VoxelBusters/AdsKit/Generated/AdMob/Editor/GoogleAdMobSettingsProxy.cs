using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace VoxelBusters.AdsKit.Adapters.Editor
{
    public sealed class GoogleAdMobSettingsProxy
    {
        private readonly object m_instance;
        private readonly Type m_type;

        public GoogleAdMobSettingsProxy()
        {
            m_type = Type.GetType("GoogleMobileAds.Editor.GoogleMobileAdsSettings, GoogleMobileAds.Editor", throwOnError: true);
            if (m_type == null)
                throw new InvalidOperationException("GoogleMobileAds.Editor assembly not found or GoogleMobileAdsSettings class not accessible. Please contact VoxelBusters support with this AdMob version details.");

            // Load the instance using reflection
            var loadMethod = m_type.GetMethod("LoadInstance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (loadMethod == null)
                throw new InvalidOperationException("LoadInstance method not found on GoogleMobileAdsSettings. Please contact VoxelBusters support with this AdMob version details.");

            m_instance = loadMethod.Invoke(null, null);
            if (m_instance == null)
                throw new InvalidOperationException("Failed to load GoogleMobileAdsSettings instance. Please contact VoxelBusters support with this AdMob version details.");
        }

        private T SafeGetValue<T>(string memberName, BindingFlags flags, T defaultValue = default)
        {
            var property = m_type.GetProperty(memberName, flags);
            if (property != null)
                return (T)property.GetValue(m_instance) ?? defaultValue;
                
            var field = m_type.GetField(memberName, flags);
            if (field != null)
                return (T)field.GetValue(m_instance) ?? defaultValue;
            
            Debug.LogWarning($"GmaSettingsFacade: Property/Field '{memberName}' not found. Contact Voxel Busters support team at gameon@voxelbusters.com");
            return defaultValue;
        }

        private void SafeSetValue(string memberName, object value, BindingFlags flags)
        {
            var property = m_type.GetProperty(memberName, flags);
            if (property != null)
            {
                property.SetValue(m_instance, value);
                return;
            }
                
            var field = m_type.GetField(memberName, flags);
            if (field != null)
            {
                field.SetValue(m_instance, value);
                return;
            }
            
            Debug.LogWarning($"GoogleAdMobSettingsProxy: Property/Field '{memberName}' not found. Contact Voxel Busters support team at gameon@voxelbusters.com");
        }

        // App ID Properties
        public string AndroidAppId
        {
            get => SafeGetValue<string>("GoogleMobileAdsAndroidAppId", BindingFlags.Public | BindingFlags.Instance, string.Empty);
            set => SafeSetValue("GoogleMobileAdsAndroidAppId", value, BindingFlags.Public | BindingFlags.Instance);
        }

        public string IOSAppId
        {
            get => SafeGetValue<string>("GoogleMobileAdsIOSAppId", BindingFlags.Public | BindingFlags.Instance, string.Empty);
            set => SafeSetValue("GoogleMobileAdsIOSAppId", value, BindingFlags.Public | BindingFlags.Instance);
        }

        // String Properties
        public string UserTrackingUsageDescription
        {
            get => SafeGetValue<string>("UserTrackingUsageDescription", BindingFlags.Public | BindingFlags.Instance, string.Empty);
            set => SafeSetValue("UserTrackingUsageDescription", value, BindingFlags.Public | BindingFlags.Instance);
        }
        public void Save()
        {
            EditorUtility.SetDirty(m_instance as UnityEngine.Object);
            AssetDatabase.SaveAssets();
        }
    }

}
