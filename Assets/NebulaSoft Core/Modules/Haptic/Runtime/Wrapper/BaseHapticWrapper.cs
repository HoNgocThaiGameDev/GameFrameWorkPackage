using UnityEngine;

namespace NebulaSoft
{
    public abstract class BaseHapticWrapper
    {
        public abstract void Init();
        public virtual void Shutdown() { }

        public abstract void Play(float duration = 0.3f, float intensity = 1.0f);
        public abstract void Play(string patternID);

        public abstract void RegisterPattern(HapticPattern pattern);

        protected void Log(string message)
        {
            if (!Haptic.VerboseLogging) return;

            Debug.Log(string.Format("[Haptic]: {0}", message));
        }

        protected void Try(SimpleCallback action, string errorMessage)
        {
            try
            {
                action();
            }
            catch (System.Exception e)
            {
                Debug.LogError(string.Format("[Haptic]: {0}", errorMessage));
                Debug.LogException(e);
            }
        }
    }
}
