using UnityEngine;

namespace NebulaSoft
{
    [System.Serializable]
    public sealed class CachedSphereCollider : ICachedComponent<SphereCollider>
    {
        [SerializeField] bool isTrigger;
        public bool IsTrigger => isTrigger;

        [SerializeField] Vector3 center;
        public Vector3 Center => center;

        [SerializeField] float radius;
        public float Radius => radius;

        public void Apply(SphereCollider collider)
        {
            collider.isTrigger = isTrigger;
            collider.center = center;
            collider.radius = radius;
        }

        public void Cache(SphereCollider collider)
        {
            isTrigger = collider.isTrigger;
            center = collider.center;
            radius = collider.radius;
        }
    }
}