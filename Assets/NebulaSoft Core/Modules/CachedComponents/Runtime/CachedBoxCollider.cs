using UnityEngine;

namespace NebulaSoft
{
    [System.Serializable]
    public sealed class CachedBoxCollider : ICachedComponent<BoxCollider>
    {
        [SerializeField] bool isTrigger;
        public bool IsTrigger => isTrigger;

        [SerializeField] Vector3 center;
        public Vector3 Center => center;

        [SerializeField] Vector3 size;
        public Vector3 Size => size;

        public void Apply(BoxCollider collider)
        {
            collider.isTrigger = isTrigger;
            collider.center = center;
            collider.size = size;
        }

        public void Cache(BoxCollider collider)
        {
            isTrigger = collider.isTrigger;
            center = collider.center;
            size = collider.size;
        }
    }
}