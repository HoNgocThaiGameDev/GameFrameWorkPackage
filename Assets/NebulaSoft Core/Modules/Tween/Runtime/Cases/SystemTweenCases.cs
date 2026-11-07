using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NebulaSoft
{
    public static class SystemTweenCases
    {
        #region Extensions
        public static TweenCase DOAction<T>(this object tweenObject, System.Action<T, T, float> action, T startValue, T resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod updateMethod = UpdateMethod.Update)
        {
            return new Action<T>(startValue, resultValue, action).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(updateMethod).StartTween();
        }

        public static TweenCase OnCompleted(this AsyncOperation tweenObject, SimpleCallback onCompleted)
        {
            return new AsyncOperationTweenCase(tweenObject).SetUnscaledMode(true).SetUpdateMethod(UpdateMethod.Update).OnComplete(onCompleted).StartTween();
        }
        #endregion

        public class Default : TweenCase
        {
            public override void DefaultComplete() { }
            public override void Invoke(float deltaTime) { }

            public override bool Validate()
            {
                return true;
            }
        }

        public class Condition : TweenCase
        {
            public TweenConditionCallback callback;

            public Condition(TweenConditionCallback callback)
            {
                this.callback = callback;
            }

            public override void DefaultComplete()
            {

            }

            public override void Invoke(float deltaTime)
            {
                callback.Invoke(this);
            }

            public override bool Validate()
            {
                return callback != null;
            }

            public delegate void TweenConditionCallback(Condition tweenCase);
        }

        public class Action<T> : TweenCase
        {
            private System.Action<T, T, float> action;

            private T startValue;
            private T resultValue;

            public Action(T startValue, T resultValue, System.Action<T, T, float> action)
            {
                this.startValue = startValue;
                this.resultValue = resultValue;

                this.action = action;
            }

            public override bool Validate()
            {
                return true;
            }

            public override void DefaultComplete()
            {
                action.Invoke(startValue, resultValue, 1);
            }

            public override void Invoke(float deltaTime)
            {
                action.Invoke(startValue, resultValue, Interpolate(state));
            }
        }

        public class NextFrame : TweenCase
        {
            private SimpleCallback callback;
            protected int framesOffset;
            private int lastFrame;
            private double lastFixedTime;
            private readonly bool waitForNextTick;

            public NextFrame(SimpleCallback callback, int framesOffset)
            {
                this.callback = callback;
                this.framesOffset = Mathf.Max(1, framesOffset);
                waitForNextTick = framesOffset > 0;
                lastFrame = Time.frameCount;
                lastFixedTime = Time.fixedTimeAsDouble;
            }

            public override void Invoke(float deltaTime)
            {
                if (waitForNextTick)
                {
                    if (UpdateMethod == UpdateMethod.FixedUpdate)
                    {
                        double fixedTime = Time.fixedTimeAsDouble;
                        if (fixedTime == lastFixedTime) return;
                        lastFixedTime = fixedTime;
                    }
                    else
                    {
                        int frame = Time.frameCount;
                        if (frame == lastFrame) return;
                        lastFrame = frame;
                    }
                }
                framesOffset--;

                if (framesOffset <= 0)
                    Complete();
            }

            public override void DefaultComplete()
            {
                callback?.Invoke();
            }

            public override bool Validate()
            {
                return true;
            }
        }

        public class Float : TweenCase
        {
            public float startValue;
            public float resultValue;

            public TweenFloatCallback callback;

            public Float(float startValue, float resultValue, TweenFloatCallback callback)
            {
                this.startValue = startValue;
                this.resultValue = resultValue;

                this.callback = callback;
            }

            public override bool Validate()
            {
                return true;
            }

            public override void DefaultComplete()
            {
                callback.Invoke(resultValue);
            }

            public override void Invoke(float deltaTime)
            {
                callback.Invoke(Mathf.LerpUnclamped(startValue, resultValue, Interpolate(state)));
            }

            public delegate void TweenFloatCallback(float value);
        }

        public class ColorCase : TweenCase
        {
            public Color startValue;
            public Color resultValue;

            public TweenColorCallback callback;

            public ColorCase(Color startValue, Color resultValue, TweenColorCallback callback)
            {
                this.startValue = startValue;
                this.resultValue = resultValue;

                this.callback = callback;
            }

            public override bool Validate()
            {
                return true;
            }

            public override void DefaultComplete()
            {
                callback.Invoke(resultValue);
            }

            public override void Invoke(float deltaTime)
            {
                callback.Invoke(Color.LerpUnclamped(startValue, resultValue, Interpolate(state)));
            }

            public delegate void TweenColorCallback(Color color);
        }

        public class AsyncOperationTweenCase : TweenCase
        {
            public AsyncOperation asyncOperation;

            public float Progress => asyncOperation.progress;
            public bool IsOperationDone => asyncOperation.isDone;

            public AsyncOperationTweenCase(AsyncOperation asyncOperation)
            {
                this.asyncOperation = asyncOperation;

                duration = float.MaxValue;
            }

            public override void DefaultComplete()
            {

            }

            public override void Invoke(float deltaTime)
            {
                if (asyncOperation.isDone)
                    Complete();
            }

            public override bool Validate()
            {
                return asyncOperation != null;
            }
        }
    }
}
