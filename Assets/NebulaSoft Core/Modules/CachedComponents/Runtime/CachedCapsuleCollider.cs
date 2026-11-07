using UnityEngine;

namespace NebulaSoft
{
    [System.Serializable]
    public sealed class CachedCapsuleCollider : ICachedComponent<CapsuleCollider>
    {
        [SerializeField] bool isTrigger;
        public bool IsTrigger => isTrigger;

        [SerializeField] Vector3 center;
        public Vector3 Center => center;

        [SerializeField] float radius;
        public float Radius => radius;

        [SerializeField] float height;
        public float Height => height;

        public void Apply(CapsuleCollider collider)
        {
            collider.isTrigger = isTrigger;
            collider.center = center;
            collider.radius = radius;
            collider.height = height;
        }

        public void Cache(CapsuleCollider collider)
        {
            isTrigger = collider.isTrigger;
            center = collider.center;
            radius = collider.radius;
            height = collider.height;
        }
    }
}