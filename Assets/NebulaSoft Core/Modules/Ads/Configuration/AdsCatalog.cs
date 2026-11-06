using System;
using System.Collections.Generic;
using UnityEngine;

namespace NebulaSoft.Ads
{
    [CreateAssetMenu(menuName = "NebulaSoft/Ads/Catalog")]
    public sealed class AdsCatalog : ScriptableObject
    {
        [SerializeField] AdPlacement[] placements = Array.Empty<AdPlacement>();
        public IReadOnlyList<AdPlacement> Placements => placements;

        public List<string> ValidateCatalog()
        {
            var errors = new List<string>();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var placement in placements ?? Array.Empty<AdPlacement>())
            {
                if (placement == null) { errors.Add("Catalog contains a missing placement."); continue; }
                if (string.IsNullOrWhiteSpace(placement.Id)) errors.Add("Placement ID is missing: " + placement.name);
                else if (!ids.Add(placement.Id)) errors.Add("Duplicate placement ID: " + placement.Id);
                if (!Enum.IsDefined(typeof(AdFormat), placement.Format)) errors.Add("Invalid format: " + placement.name);
            }
            return errors;
        }

        public AdPlacementDefinition[] CreateDefinitions()
        {
            var definitions = new AdPlacementDefinition[placements.Length];
            for (int i = 0; i < placements.Length; i++) definitions[i] = placements[i].ToDefinition();
            return definitions;
        }
    }
}
