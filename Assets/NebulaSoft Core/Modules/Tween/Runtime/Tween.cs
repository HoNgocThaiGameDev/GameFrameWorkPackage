using UnityEngine;
using System.Collections;

namespace NebulaSoft
{
    [StaticUnload]
    public class Tween : MonoBehaviour
    {
        private static Tween instance;

        private readonly int TWEEN_UPDATE = (int)UpdateMethod.Update;
        private readonly int TWEEN_FIXED_UPDATE = (int)UpdateMethod.FixedUpdate;
        private readonly int TWEEN_LATE_UPDATE = (int)UpdateMethod.LateUpdate;

        private static TweensHolder[] tweens;
        public static TweensHolder[] Tweens => tweens;

        private static TweenCaseCollection activeTweenCaseCollection;
        private static bool isActiveTweenCaseCollectionEnabled;

        public void Init(int tweensUpdateCount, int tweensFixedUpdateCount, int tweensLateUpdateCount, bool systemLogs)
        {
            if (instance != null) instance.StopAllCoroutines();
            UnloadStatic();
            instance = this;

            DontDestroyOnLoad(gameObject);

            tweens = new TweensHolder[]
            {
                new TweensHolder(UpdateMethod.Update, tweensUpdateCount),
                new TweensHolder(UpdateMethod.FixedUpdate, tweensFixedUpdateCount),
                new TweensHolder(UpdateMethod.LateUpdate, tweensLateUpdateCount)
            };
        }

        public static void AddTween(TweenCase tween)
        {
            if (tween == null || tween.Holder != null || tween.IsKilling) return;
            tweens[tween.UpdateMethodIndex].AddTween(tween);

            if (isActiveTweenCaseCollectionEnabled)
                activeTweenCaseCollection.AddTween(tween);
        }

        public static void Pause(UpdateMethod tweenType)
        {
            tweens[(int)tweenType].Pause();
        }

        public static void PauseAll()
        {
            foreach(TweensHolder tween in tweens)
            {
                tween.Pause();
            }
        }

        public static void Resume(UpdateMethod tweenType)
        {
            tweens[(int)tweenType].Resume();
        }

        public static void ResumeAll()
        {
            foreach (TweensHolder tween in tweens)
            {
                tween.Resume();
            }
        }

        public static void Remove(UpdateMethod tweenType)
        {
            tweens[(int)tweenType].Kill();
        }

        public static void RemoveAll()
        {
            foreach (TweensHolder tween in tweens)
            {
                tween.Kill();
            }
        }

        private void Update()
        {
            if (tweens == null || instance != this) return;
            tweens[TWEEN_UPDATE].Update(Time.deltaTime, Time.unscaledDeltaTime);
        }

        private void FixedUpdate()
        {
            if (tweens == null || instance != this) return;
            tweens[TWEEN_FIXED_UPDATE].Update(Time.fixedDeltaTime, Time.fixedUnscaledDeltaTime);
        }

        private void LateUpdate()
        {
            if (tweens == null || instance != this) return;
            tweens[TWEEN_LATE_UPDATE].Update(Time.deltaTime, Time.unscaledDeltaTime);
        }

        public static void MarkForKilling(TweenCase tween)
        {
            tween?.Kill();
        }

        public static TweenCaseCollection BeginTweenCaseCollection()
        {
            isActiveTweenCaseCollectionEnabled = true;

            activeTweenCaseCollection = new TweenCaseCollection();

            return activeTweenCaseCollection;
        }

        public static void EndTweenCaseCollection()
        {
            isActiveTweenCaseCollectionEnabled = false;
            activeTweenCaseCollection = null;
        }

        #region Custom Tweens
        /// <summary>
        /// Delayed call of delegate.
        /// </summary>
        /// <param name="callback">Callback to call.</param>
        /// <param name="delay">Delay in seconds.</param>
        public static TweenCase DelayedCall(float delay, SimpleCallback callback, bool unscaledTime = false, UpdateMethod tweenType = UpdateMethod.Update)
        {
            if (delay <= 0)
            {
                callback?.Invoke();
                return null;
            }
            else
            {
                return new SystemTweenCases.Default().SetDuration(delay).SetUnscaledMode(unscaledTime).OnComplete(callback).SetUpdateMethod(tweenType).StartTween();
            }
        }

        /// <summary>
        /// Interpolate float value
        /// </summary>
        public static TweenCase DoColor(Color startValue, Color resultValue, float time, SystemTweenCases.ColorCase.TweenColorCallback callback, bool unscaledTime = false, UpdateMethod tweenType = UpdateMethod.Update)
        {
            return new SystemTweenCases.ColorCase(startValue, resultValue, callback).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(tweenType).StartTween();
        }

        /// <summary>
        /// Interpolate float value
        /// </summary>
        public static TweenCase DoFloat(float startValue, float resultValue, float time, SystemTweenCases.Float.TweenFloatCallback callback, float delay = 0.0f, bool unscaledTime = false, UpdateMethod tweenType = UpdateMethod.Update)
        {
            return new SystemTweenCases.Float(startValue, resultValue, callback).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(tweenType).StartTween();
        }

        /// <summary>
        /// Wait for condition
        /// </summary>
        public static TweenCase DoWaitForCondition(SystemTweenCases.Condition.TweenConditionCallback callback, bool unscaledTime = false, UpdateMethod tweenType = UpdateMethod.Update)
        {
            return new SystemTweenCases.Condition(callback).SetDuration(float.MaxValue).SetUnscaledMode(unscaledTime).SetUpdateMethod(tweenType).StartTween();
        }

        /// <summary>
        /// Call function in next frame
        /// </summary>
        public static TweenCase NextFrame(SimpleCallback callback, int framesOffset = 1, bool unscaledTime = true, UpdateMethod updateMethod = UpdateMethod.Update)
        {
            return new SystemTweenCases.NextFrame(callback, framesOffset).SetDuration(float.MaxValue).SetUnscaledMode(unscaledTime).SetUpdateMethod(updateMethod).StartTween();
        }

        /// <summary>
        /// Invoke coroutine from non-monobehavior script
        /// </summary>
        public static Coroutine InvokeCoroutine(IEnumerator enumerator)
        {
            if (instance == null) return null;

            return instance.StartCoroutine(enumerator);
        }

        /// <summary>
        /// Stop custom coroutine
        /// </summary>
        public static void StopCustomCoroutine(Coroutine coroutine)
        {
            if (instance == null) return;

            instance.StopCoroutine(coroutine);
        }
        #endregion

        public static void DestroyObject()
        {
            // Stop all coroutines
            if(instance != null)
                instance.StopAllCoroutines();

            if (tweens == null) return;
            foreach (TweensHolder tween in tweens)
            {
                tween.Unload();
            }
        }

        private void OnDestroy()
        {
            if (instance == this) UnloadStatic();
        }

        private static void UnloadStatic()
        {
            instance = null;

            if (tweens != null)
            {
                foreach (TweensHolder tween in tweens)
                {
                    tween.Unload();
                }
            }
            tweens = null;

            activeTweenCaseCollection = null;
            isActiveTweenCaseCollectionEnabled = false;
        }
    }

    public enum UpdateMethod
    {
        Update = 0,
        FixedUpdate = 1,
        LateUpdate = 2
    }
}
