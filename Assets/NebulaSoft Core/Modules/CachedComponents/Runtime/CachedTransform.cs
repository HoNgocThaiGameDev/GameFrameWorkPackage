using UnityEngine;

namespace NebulaSoft
{
    [System.Serializable]
    public sealed class CachedTransform : ICachedComponent<Transform>
    {
        [SerializeField] Vector3 position;
        public Vector3 Position => position;

        [SerializeField] Quaternion rotation;
        public Quaternion Rotation => rotation;

        [SerializeField] Vector3 scale;
        public Vector3 Scale => scale;

        public void Apply(Transform transform)
        {
            transform.position = position;
            transform.rotation = rotation;
            transform.localScale = scale;
        }

        public void Cache(Transform transform)
        {
            position = transform.position;
            rotation = transform.rotation;
            scale = transform.localScale;
        }
    }
}