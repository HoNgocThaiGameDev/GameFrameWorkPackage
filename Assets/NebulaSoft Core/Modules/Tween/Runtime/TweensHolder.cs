using UnityEngine;
using System;

namespace NebulaSoft
{
    public class TweensHolder
    {
        protected TweenCase[] tweens;
        public TweenCase[] Tweens => tweens;
        protected int tweensCount;
        protected UpdateMethod updateMethod;

#if UNITY_EDITOR
        protected int maxTweensAmount;
#endif

        private bool isUpdating;
        private bool needsCompaction;
        private int unloadVersion;

        public TweensHolder(UpdateMethod updateMethod, int defaultAmount)
        {
            this.updateMethod = updateMethod;
            tweens = new TweenCase[Math.Max(0, defaultAmount)];
        }

        public void AddTween(TweenCase tween)
        {
            if (tween == null || tween.Holder != null || tween.IsKilling) return;
            if (!isUpdating) Compact();

            if (tweensCount == tweens.Length)
            {
                Array.Resize(ref tweens, tweens.Length + 50);
                Debug.LogWarning($"[Tween]: {updateMethod} capacity increased to {tweens.Length}.");
            }

            tween.Holder = this;
            tween.IsActive = true;
            tween.ActiveID = tweensCount;
            tweens[tweensCount++] = tween;
#if UNITY_EDITOR
            maxTweensAmount = Math.Max(maxTweensAmount, tweensCount);
#endif
        }

        public void Update(float deltaTime, float unscaledDeltaTime)
        {
            if (isUpdating || tweensCount == 0) return;
            isUpdating = true;
            // Callbacks can append or cancel tweens. Visit only the original slots.
            int count = tweensCount;
            int version = unloadVersion;
            try
            {
                for (int i = 0; i < count && version == unloadVersion; i++)
                {
                    TweenCase tween = tweens[i];
                    if (!tween.IsActive) continue;
                    try { Tick(tween, deltaTime, unscaledDeltaTime); }
                    catch (Exception exception)
                    {
                        tween.Kill();
                        Debug.LogException(exception);
                    }
                }
            }
            finally
            {
                isUpdating = false;
                Compact();
            }
        }

        private static void Tick(TweenCase tween, float deltaTime, float unscaledDeltaTime)
        {
            if (!tween.Validate())
            {
                tween.Kill();
                return;
            }
            if (tween.IsPaused) return;

            if (!tween.IsCompleted)
            {
                if (!tween.IsUnscaled && Time.timeScale == 0) return;
                float step = Mathf.Max(0, tween.IsUnscaled ? unscaledDeltaTime : deltaTime);
                float remainingDelay = tween.Delay - tween.CurrentDelay;
                if (remainingDelay > 0)
                {
                    float consumed = Mathf.Min(step, remainingDelay);
                    tween.UpdateDelay(consumed);
                    if (step < remainingDelay) return;
                    step -= consumed;
                }

                if (tween.AdvanceState(step) && !CanContinue(tween)) return;
                if (!tween.IsCompleted) tween.Invoke(step);
            }

            if (!tween.IsCompleted || !CanContinue(tween)) return;
            // A condition or callback can request completion without advancing the clock.
            if (tween.InvokeReachedCallbacks() && !CanContinue(tween)) return;
            if (!tween.IsCompleted) return;
            tween.DefaultComplete();
            if (tween.IsActive) tween.InvokeCompleteEvent();
            tween.Kill();
        }

        private static bool CanContinue(TweenCase tween)
        {
            if (!tween.IsActive || tween.IsPaused) return false;
            if (tween.Validate()) return true;
            tween.Kill();
            return false;
        }

        private void Compact()
        {
            if (!needsCompaction) return;
            int next = 0;
            for (int i = 0; i < tweensCount; i++)
            {
                TweenCase tween = tweens[i];
                if (tween.IsActive)
                {
                    tweens[next] = tween;
                    tween.ActiveID = next++;
                }
                else
                {
                    tween.Holder = null;
                    tween.ActiveID = -1;
                }
            }
            Array.Clear(tweens, next, tweensCount - next);
            tweensCount = next;
            needsCompaction = false;
        }

        public void Pause()
        {
            for (int i = 0; i < tweensCount; i++)
                if (tweens[i].IsActive) tweens[i].Pause();
        }

        public void Resume()
        {
            for (int i = 0; i < tweensCount; i++)
                if (tweens[i].IsActive) tweens[i].Resume();
        }

        public void Kill()
        {
            for (int i = 0; i < tweensCount; i++) tweens[i].Kill();
            if (!isUpdating) Compact();
        }

        public void MarkForKilling(TweenCase tween)
        {
            if (tween?.Holder == this) tween.Kill();
        }

        internal void RequestCompaction() => needsCompaction = true;

        public void Unload()
        {
            unloadVersion++;
            for (int i = 0; i < tweensCount; i++)
            {
                TweenCase tween = tweens[i];
                tween.Kill();
                tween.Holder = null;
                tween.ActiveID = -1;
            }
            Array.Clear(tweens, 0, tweensCount);
            tweensCount = 0;
            needsCompaction = false;
        }
    }
}
