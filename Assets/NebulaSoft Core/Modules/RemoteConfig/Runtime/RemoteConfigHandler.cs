using System;
using System.Collections;
using UnityEngine;

namespace NebulaSoft
{
    [RequireComponent(typeof(Initializer))]
    public abstract class RemoteConfigHandler : MonoBehaviour
    {
        private const float TIMEOUT = 20;

        public IEnumerator LoadConfig(Action<bool> configLoaded)
        {
            Init();

            float timeOutTime = Time.realtimeSinceStartup + TIMEOUT;

            while(!IsLoaded())
            {
                if(Time.realtimeSinceStartup > timeOutTime)
                {
                    OnTimeout();

                    configLoaded?.Invoke(false);

                    yield break;
                }

                yield return null;
            }

            OnConfigLoaded();

            configLoaded?.Invoke(true);
        }

        protected abstract void Init();
        protected abstract void OnConfigLoaded();
        protected abstract void OnTimeout();

        public abstract bool IsLoaded();
    }
}
