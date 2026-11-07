using UnityEngine;

namespace NebulaSoft
{
    public abstract class SDKBehavior : MonoBehaviour
    {
        public virtual void Shutdown() { }
        public virtual void Init() { }

        public abstract void OnUserConsentReceived();
    }
}
