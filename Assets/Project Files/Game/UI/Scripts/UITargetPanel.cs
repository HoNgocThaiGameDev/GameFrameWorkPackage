using UnityEngine;

namespace NebulaSoft
{
    [System.Serializable]
    public sealed class UITargetPanel
    {
        [SerializeField] GameObject prefab;
        [SerializeField] RectTransform container;
        public void Init() { /* UI-only mock: gameplay supplies target sprites later. */ }
    }
}
