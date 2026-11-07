using UnityEngine;

namespace NebulaSoft
{
    [System.Serializable]
    public class FloatingTextCase
    {
        [SerializeField] string name;
        public string Name => name;

        [SerializeField] FloatingTextBaseBehavior floatingTextBehavior;
        public FloatingTextBaseBehavior FloatingTextBehavior => floatingTextBehavior;

        private Pool floatingTextPool;
        public Pool FloatingTextPool => floatingTextPool;

        public void Init()
        {
            floatingTextPool = new Pool(floatingTextBehavior.gameObject);
        }
    }
}