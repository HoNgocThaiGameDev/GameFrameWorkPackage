using UnityEngine;

namespace NebulaSoft.Ads
{
    [CreateAssetMenu(menuName = "NebulaSoft/Ads/Placement")]
    public sealed class AdPlacement : ScriptableObject
    {
        [SerializeField] string id;
        [SerializeField] AdFormat format;
        [SerializeField] bool isEnabled = true;
        [SerializeField] bool preload = true;
        [SerializeField, Min(0)] float cooldownSeconds;
        public string Id => id;
        public AdFormat Format => format;
        public bool IsEnabled => isEnabled;
        public AdPlacementDefinition ToDefinition() => new AdPlacementDefinition(id, format, isEnabled, preload, cooldownSeconds);
    }
}
