using System.Collections.Generic;
using UnityEngine;

namespace NebulaSoft
{
    public abstract class TweenCase
    {
        // System variables
        public int ActiveID = -1;
        public bool IsActive;
        internal TweensHolder Holder { get; set; }
        internal bool HasInvokedCompletion { get; private set; }
        internal event SimpleCallback CompletionDelivered;

        protected float currentDelay;
        public float CurrentDelay => currentDelay;

        protected float delay;
        public float Delay => delay;

        protected float state;
        public float State => state;

        protected int updateMethodIndex;
        public int UpdateMethodIndex => updateMethodIndex;
        public UpdateMethod UpdateMethod => (UpdateMethod)updateMethodIndex;

        protected float duration;
        public float Duration => duration;

        protected bool isPaused;
        public bool IsPaused => isPaused;

        protected bool isUnscaled;
        public bool IsUnscaled => isUnscaled;

        protected bool isCompleted;
        public bool IsCompleted => isCompleted;

        protected bool isKilling;
        public bool IsKilling => isKilling;

        protected Ease.IEasingFunction easeFunction;

        protected event SimpleCallback tweenCompleted;

        protected GameObject parentObject;
        public GameObject ParentObject => parentObject;

        private List<CallbackData> callbackData;

        public TweenCase()
        {
            SetEasing(Ease.Type.Linear);
        }

        public virtual TweenCase StartTween()
        {
            Tween.AddTween(this);

            return this;
        }

        public abstract bool Validate();

        /// <summary>
        /// Stop and remove tween
        /// </summary>
        public TweenCase Kill()
        {
            if (!isKilling)
            {
                isKilling = true;
                IsActive = false;
                tweenCompleted = null;
                CompletionDelivered = null;
                callbackData = null;
                Holder?.RequestCompaction();
            }

            return this;
        }

        /// <summary>
        /// Complete tween
        /// </summary>
        public TweenCase Complete()
        {
            if (isKilling || HasInvokedCompletion) return this;
            if (isPaused)
                isPaused = false;

            state = 1;

            isCompleted = true;

            return this;
        }

        /// <summary>
        /// Pause current coroutine.
        /// </summary>
        public TweenCase Pause()
        {
            isPaused = true;

            return this;
        }

        /// <summary>
        /// Play tween if it was paused.
        /// </summary>
        public TweenCase Resume()
        {
            isPaused = false;

            return this;
        }


        /// <summary>
        /// Reset tween state
        /// </summary>
        public void Reset()
        {
            if (isKilling || HasInvokedCompletion) return;
            state = 0;
            isCompleted = false;
        }

        /// <summary>
        /// Set tween easing based on animation curve
        /// </summary>
        public TweenCase SetCurveEasing(AnimationCurve easingCurve)
        {
            easeFunction = new AnimationCurveEasingFunction(easingCurve);

            return this;
        }

        /// <summary>
        /// Set tween easing
        /// </summary>
        public TweenCase SetCustomEasing(Ease.IEasingFunction easeFunction)
        {
            this.easeFunction = easeFunction;

            return this;
        }

        /// <summary>
        /// Interpolate current easing function.
        /// </summary>
        public float Interpolate(float p)
        {
            return easeFunction.Interpolate(p);
        }

        #region Set
        public TweenCase SetDelay(float delay)
        {
            this.delay = delay;

            currentDelay = 0;

            return this;
        }

        /// <summary>
        /// Update method can be set only before StartTween method is called.
        /// </summary>
        public TweenCase SetUpdateMethod(UpdateMethod updateMethod)
        {
            if (Holder != null) return this;
            if (updateMethod < UpdateMethod.Update || updateMethod > UpdateMethod.LateUpdate)
                throw new System.ArgumentOutOfRangeException(nameof(updateMethod));
            this.updateMethodIndex = (int)updateMethod;

            return this;
        }

        public TweenCase SetUnscaledMode(bool isUnscaled)
        {
            this.isUnscaled = isUnscaled;

            return this;
        }

        /// <summary>
        /// Set tween easing function.
        /// </summary>
        public TweenCase SetEasing(Ease.Type ease)
        {
            easeFunction = Ease.GetFunction(ease);

            return this;
        }

        /// <summary>
        /// Change tween duration.
        /// </summary>
        public TweenCase SetDuration(float duration)
        {
            this.duration = duration;

            return this;
        }
        #endregion

        /// <summary>
        /// System method. Update state value.
        /// </summary>
        public void UpdateState(float deltaTime)
        {
            AdvanceState(deltaTime);
        }

        internal bool AdvanceState(float deltaTime)
        {
            if (!isCompleted)
            {
                state = duration > 0 ? Mathf.Clamp01(state + Mathf.Max(0, deltaTime) / duration) : 1;
                isCompleted = state >= 1;
            }
            return InvokeReachedCallbacks();
        }

        internal bool InvokeReachedCallbacks()
        {
            if (callbackData == null || callbackData.Count == 0 || callbackData[0].t > state) return false;
            int reached = 0;
            while (reached < callbackData.Count && callbackData[reached].t <= state) reached++;
            if (reached == 0) return false;

            // Detach before invoking so callbacks can safely change the tween or register other callbacks.
            var callbacks = callbackData.GetRange(0, reached);
            callbackData.RemoveRange(0, reached);
            foreach (var callback in callbacks)
            {
                if (isKilling) break;
                InvokeSafely(callback.callback);
            }
            return true;
        }

        /// <summary>
        /// System method. Update delay value.
        /// </summary>
        public void UpdateDelay(float deltaTime)
        {
            currentDelay += deltaTime;
        }

        /// <summary>
        /// Init function that called when it will completed.
        /// </summary>
        /// <param name="callback">Complete function.</param>
        public TweenCase OnComplete(SimpleCallback callback)
        {
            if (!isKilling && !HasInvokedCompletion) tweenCompleted += callback;

            return this;
        }

        /// <summary>
        /// Set the method that is called once the tween reaches a point in time
        /// </summary>
        /// <param name="t">0-1</param>
        /// <param name="callback">method</param>
        /// <returns></returns>
        public TweenCase OnTimeReached(float t, SimpleCallback callback)
        {
            if (isKilling || HasInvokedCompletion || callback == null || float.IsNaN(t)) return this;
            if (callbackData == null) callbackData = new List<CallbackData>();
            t = Mathf.Clamp01(t);
            int index = callbackData.Count;
            while (index > 0 && callbackData[index - 1].t > t) index--;
            callbackData.Insert(index, new CallbackData(t, callback));

            return this;
        }

        public void InvokeCompleteEvent()
        {
            if (isKilling || HasInvokedCompletion) return;
            HasInvokedCompletion = true;
            SimpleCallback callbacks = tweenCompleted;
            SimpleCallback delivered = CompletionDelivered;
            tweenCompleted = null;
            CompletionDelivered = null;
            InvokeSubscribers(callbacks);
            InvokeSubscribers(delivered);
        }

        internal static void InvokeSubscribers(SimpleCallback callbacks)
        {
            if (callbacks == null) return;
            foreach (SimpleCallback callback in callbacks.GetInvocationList()) InvokeSafely(callback);
        }

        private static void InvokeSafely(SimpleCallback callback)
        {
            try { callback(); }
            catch (System.Exception exception) { Debug.LogException(exception); }
        }

        public abstract void Invoke(float deltaTime);
        public abstract void DefaultComplete();

        private class CallbackData
        {
            public SimpleCallback callback;
            public float t;

            public CallbackData(float t, SimpleCallback callback)
            {
                this.t = t;
                this.callback = callback;
            }
        }
    }
}
