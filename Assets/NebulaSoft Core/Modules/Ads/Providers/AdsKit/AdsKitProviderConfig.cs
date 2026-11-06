using System;
using System.Collections.Generic;
using NebulaSoft.Ads;
using UnityEngine;

namespace NebulaSoft
{
    [Define("MODULE_ADSKIT", "VoxelBusters.AdsKit.AdsManager", "Assets/Plugins/VoxelBusters/AdsKit")]
    [CreateAssetMenu(menuName = "NebulaSoft/Ads/Providers/Ads Kit")]
    public sealed class AdsKitProviderConfig : AdsProviderConfig
    {
        [Serializable]
        public sealed class PlacementMapping
        {
            [SerializeField] AdPlacement placement;
            [SerializeField] string sdkPlacement;
            public AdPlacement Placement => placement;
            public string SdkPlacement => sdkPlacement;
        }

        [SerializeField] PlacementMapping[] mappings = Array.Empty<PlacementMapping>();
#if MODULE_ADSKIT
        [SerializeField] AdsKitConsentProviderConfig consentProvider;
#endif
        public IReadOnlyList<PlacementMapping> Mappings => mappings;

        public override IAdsProvider CreateProvider()
        {
#if MODULE_ADSKIT
            return new AdsKitProvider(mappings, consentProvider);
#else
            throw new InvalidOperationException("Ads Kit is not installed. Select Dummy for editor testing, or install the SDK separately.");
#endif
        }

        public override IEnumerable<string> ValidateProvider(AdsCatalog catalog)
        {
            var mapped = new Dictionary<string, PlacementMapping>(StringComparer.Ordinal);
            foreach (var mapping in mappings ?? Array.Empty<PlacementMapping>())
            {
                if (mapping == null || mapping.Placement == null || string.IsNullOrWhiteSpace(mapping.SdkPlacement))
                { yield return "Ads Kit mapping is incomplete."; continue; }
                if (string.IsNullOrWhiteSpace(mapping.Placement.Id)) continue;
                if (mapped.ContainsKey(mapping.Placement.Id)) yield return "Duplicate Ads Kit mapping: " + mapping.Placement.Id;
                else mapped.Add(mapping.Placement.Id, mapping);
            }
            foreach (var placement in catalog.Placements)
                if (placement != null && placement.IsEnabled && !mapped.ContainsKey(placement.Id)) yield return "Ads Kit mapping is missing: " + placement.Id;
#if MODULE_ADSKIT
            if (consentProvider == null) yield return "Ads Kit consent provider is missing.";
            var sdkSettings = VoxelBusters.AdsKit.AdsKitSettings.Instance;
            if (sdkSettings == null || !sdkSettings.IsEnabled) yield return "Ads Kit settings are missing or disabled.";
            else
            {
                if (sdkSettings.AutoInitOnStart) yield return "Turn off Ads Kit Auto Init On Start; AdsModule owns initialization after consent.";
                foreach (var mapping in mapped.Values)
                {
                    var meta = Array.Find(sdkSettings.PlacementMetaArray, item => item.Name == mapping.SdkPlacement);
                    if (meta == null) yield return "Placement is absent from AdsKitSettings: " + mapping.SdkPlacement;
                    else if (!AdsKitProvider.MatchesFormat(mapping.Placement.Format, meta.AdType)) yield return "Ads Kit placement format mismatch: " + mapping.SdkPlacement;
                }
            }
#else
            yield return "Ads Kit SDK is not installed; no SDK was imported by this module.";
#endif
        }
    }
}
