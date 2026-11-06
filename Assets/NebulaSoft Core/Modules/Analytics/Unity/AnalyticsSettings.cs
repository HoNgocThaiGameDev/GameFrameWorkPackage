using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace NebulaSoft
{
    [CreateAssetMenu(menuName = "NebulaSoft/Analytics/Settings", fileName = "Analytics Settings")]
    [MovedFrom(true, "NebulaSoft", "Assembly-CSharp", "AnalyticsModuleSettings")]
    public sealed class AnalyticsSettings : ScriptableObject
    {
        [SerializeField] private AnalyticsBackend provider = AnalyticsBackend.Console;
        [Tooltip("Initial collection state. Your application's consent flow can change it through AnalyticsController.")]
        [SerializeField] private bool collectionEnabled = true;
        public AnalyticsBackend Provider => provider;
        public bool CollectionEnabled => collectionEnabled;
    }
}
