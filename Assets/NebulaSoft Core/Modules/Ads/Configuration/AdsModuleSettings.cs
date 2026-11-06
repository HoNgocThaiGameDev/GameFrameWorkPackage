using System.Collections.Generic;
using UnityEngine;

namespace NebulaSoft.Ads
{
    [CreateAssetMenu(menuName = "NebulaSoft/Ads/Module Settings")]
    public sealed class AdsModuleSettings : ScriptableObject
    {
        [SerializeField] bool isEnabled = true;
        [SerializeField] AdsCatalog catalog;
        [SerializeField] AdsProviderConfig provider;
        [SerializeField] AdPlacement banner;
        [SerializeField] AdPlacement interstitial;
        [SerializeField] AdPlacement rewarded;
        public bool IsEnabled => isEnabled;
        public AdsCatalog Catalog => catalog;
        public AdsProviderConfig Provider => provider;
        public AdPlacement Banner => banner;
        public AdPlacement Interstitial => interstitial;
        public AdPlacement Rewarded => rewarded;

        public List<string> ValidateSettings()
        {
            var errors = catalog != null ? catalog.ValidateCatalog() : new List<string> { "Ads catalog is missing." };
            if (provider == null) errors.Add("Ads provider configuration is missing.");
            else if (catalog != null) errors.AddRange(provider.ValidateProvider(catalog));
            CheckDefault(banner, AdFormat.Banner, errors);
            CheckDefault(interstitial, AdFormat.Interstitial, errors);
            CheckDefault(rewarded, AdFormat.Rewarded, errors);
            return errors;
        }

        private void CheckDefault(AdPlacement placement, AdFormat expected, List<string> errors)
        {
            if (placement == null) return;
            if (placement.Format != expected) errors.Add("Default " + expected + " has the wrong format.");
            if (catalog != null)
            {
                bool found = false;
                foreach (var item in catalog.Placements) if (item == placement) found = true;
                if (!found) errors.Add("Default " + expected + " is outside the catalog.");
            }
        }
    }
}
