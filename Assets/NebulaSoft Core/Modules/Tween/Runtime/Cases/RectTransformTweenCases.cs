using UnityEngine;
using UnityEngine.UI;

namespace NebulaSoft
{
    public static class RectTransformTweenCases
    {
        #region Extensions
        /// <summary>
        /// Change anchored position of rectTransform
        /// </summary>
        public static TweenCase DOAnchoredPosition(this RectTransform tweenObject, Vector2 resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod updateMethod = UpdateMethod.Update)
        {
            return new AnchoredPosition(tweenObject, resultValue).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(updateMethod).StartTween();
        }

        /// <summary>
        /// Change anchored position of rectTransform
        /// </summary>
        public static TweenCase DOAnchoredPosition(this RectTransform tweenObject, Vector3 resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod updateMethod = UpdateMethod.Update)
        {
            return new AnchoredPosition3D(tweenObject, resultValue).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(updateMethod).StartTween();
        }

        /// <summary>
        /// Change anchored position of rectTransform
        /// </summary>
        public static TweenCase DOAnchoredPosition(this Graphic tweenObject, Vector2 resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod updateMethod = UpdateMethod.Update)
        {
            return new AnchoredPosition(tweenObject.rectTransform, resultValue).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(updateMethod).StartTween();
        }

        /// <summary>
        /// Change anchored position of rectTransform
        /// </summary>
        public static TweenCase DOAnchoredPosition(this Graphic tweenObject, Vector3 resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod updateMethod = UpdateMethod.Update)
        {
            return new AnchoredPosition3D(tweenObject.rectTransform, resultValue).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(updateMethod).StartTween();
        }

        /// <summary>
        /// Change anchored position of rectTransform
        /// </summary>
        public static TweenCase DOAnchoredPositionWithVerticalOffset(this RectTransform tweenObject, Vector2 resultValue, AnimationCurve verticalOffset, float time, float delay = 0, bool unscaledTime = false, UpdateMethod updateMethod = UpdateMethod.Update)
        {
            return new AnchoredPositionWithVerticalOffset(tweenObject, resultValue, verticalOffset).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(updateMethod).StartTween();
        }

        /// <summary>
        /// Shake object in 2D space
        /// </summary>
        public static TweenCase DOAnchoredPositionShake(this RectTransform tweenObject, float magnitude, float time, float delay = 0, bool unscaledTime = false, UpdateMethod updateMethod = UpdateMethod.Update)
        {
            return new Shake(tweenObject, magnitude).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(updateMethod).StartTween();
        }

        /// <summary>
        /// Shake object in 2D space
        /// </summary>
        public static TweenCase DOAnchoredPositionShake(this Graphic tweenObject, float magnitude, float time, float delay = 0, bool unscaledTime = false, UpdateMethod updateMethod = UpdateMethod.Update)
        {
            return new Shake(tweenObject.rectTransform, magnitude).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(updateMethod).StartTween();
        }

        /// <summary>
        /// Change sizeDelta of rectTransform
        /// </summary>
        public static TweenCase DOSizeScale(this RectTransform tweenObject, float resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod updateMethod = UpdateMethod.Update)
        {
            return new SizeScale(tweenObject, tweenObject.sizeDelta * resultValue).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(updateMethod).StartTween();
        }

        /// <summary>
        /// Change sizeDelta of rectTransform
        /// </summary>
        public static TweenCase DOSizeScale(this Graphic tweenObject, float resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod updateMethod = UpdateMethod.Update)
        {
            return new SizeScale(tweenObject.rectTransform, tweenObject.rectTransform.sizeDelta * resultValue).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(updateMethod).StartTween();
        }

        /// <summary>
        /// Change sizeDelta of rectTransform
        /// </summary>
        public static TweenCase DOSize(this RectTransform tweenObject, Vector3 resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod updateMethod = UpdateMethod.Update)
        {
            return new SizeScale(tweenObject, resultValue).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(updateMethod).StartTween();
        }

        /// <summary>
        /// Change sizeDelta of rectTransform
        /// </summary>
        public static TweenCase DOSize(this Graphic tweenObject, Vector3 resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod updateMethod = UpdateMethod.Update)
        {
            return new SizeScale(tweenObject.rectTransform, resultValue).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(updateMethod).StartTween();
        }
        #endregion

        public class AnchoredPosition : TweenCaseFunction<RectTransform, Vector2>
        {
            public AnchoredPosition(RectTransform tweenObject, Vector2 resultValue) : base(tweenObject, resultValue)
            {
                parentObject = tweenObject.gameObject;

                startValue = tweenObject.anchoredPosition;
            }

            public override bool Validate()
            {
                return parentObject != null;
            }

            public override void DefaultComplete()
            {
                tweenObject.anchoredPosition = resultValue;
            }

            public override void Invoke(float deltaTime)
            {
                tweenObject.anchoredPosition = Vector2.LerpUnclamped(startValue, resultValue, Interpolate(state));
            }
        }

        public class AnchoredPosition3D : TweenCaseFunction<RectTransform, Vector3>
        {
            public AnchoredPosition3D(RectTransform tweenObject, Vector3 resultValue) : base(tweenObject, resultValue)
            {
                parentObject = tweenObject.gameObject;

                startValue = tweenObject.anchoredPosition3D;
            }

            public override bool Validate()
            {
                return parentObject != null;
            }

            public override void DefaultComplete()
            {
                tweenObject.anchoredPosition3D = resultValue;
            }

            public override void Invoke(float deltaTime)
            {
                tweenObject.anchoredPosition3D = Vector3.LerpUnclamped(startValue, resultValue, Interpolate(state));
            }
        }

        public class AnchoredPositionWithVerticalOffset : TweenCaseFunction<RectTransform, Vector2>
        {
            private AnimationCurve verticalOffset;

            public AnchoredPositionWithVerticalOffset(RectTransform tweenObject, Vector2 resultValue, AnimationCurve verticalOffset) : base(tweenObject, resultValue)
            {
                this.verticalOffset = verticalOffset;

                parentObject = tweenObject.gameObject;

                startValue = tweenObject.anchoredPosition;
            }

            public override bool Validate()
            {
                return parentObject != null;
            }

            public override void DefaultComplete()
            {
                tweenObject.anchoredPosition = resultValue;
            }

            public override void Invoke(float deltaTime)
            {
                tweenObject.anchoredPosition = Vector2.LerpUnclamped(startValue, new Vector2(resultValue.x, resultValue.y * verticalOffset.Evaluate(state)), Interpolate(state));
            }
        }

        public class SizeScale : TweenCaseFunction<RectTransform, Vector2>
        {
            public SizeScale(RectTransform tweenObject, Vector2 resultValue) : base(tweenObject, resultValue)
            {
                parentObject = tweenObject.gameObject;

                startValue = tweenObject.sizeDelta;
            }

            public override bool Validate()
            {
                return parentObject != null;
            }

            public override void DefaultComplete()
            {
                tweenObject.sizeDelta = resultValue;
            }

            public override void Invoke(float deltaTime)
            {
                tweenObject.sizeDelta = Vector2.LerpUnclamped(startValue, resultValue, Interpolate(state));
            }
        }

        public class Shake : TweenCase
        {
            private RectTransform tweenObject;
            private Vector2 startPosition;
            private float magnitude;

            public Shake(RectTransform tweenObject, float magnitude)
            {
                this.tweenObject = tweenObject;
                this.magnitude = magnitude;

                parentObject = tweenObject.gameObject;

                startPosition = tweenObject.anchoredPosition;
            }

            public override bool Validate()
            {
                return parentObject != null;
            }

            public override void DefaultComplete()
            {
                tweenObject.anchoredPosition = startPosition;
            }

            public override void Invoke(float timeDelta)
            {
                tweenObject.anchoredPosition = startPosition + Random.insideUnitCircle * magnitude * Interpolate(state);
            }
        }
    }
}
