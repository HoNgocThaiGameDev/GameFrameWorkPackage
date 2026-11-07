using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NebulaSoft
{
    public static class SpriteRendererTweenCases
    {
        #region Extensions
        /// <summary>
        /// Change color of sprite renderer
        /// </summary>
        public static TweenCase DOColor(this SpriteRenderer tweenObject, Color resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod updateMethod = UpdateMethod.Update)
        {
            return new ColorChange(tweenObject, resultValue).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(updateMethod).StartTween();
        }

        /// <summary>
        /// Change sprite renderer color alpha
        /// </summary>
        public static TweenCase DOFade(this SpriteRenderer tweenObject, float resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod updateMethod = UpdateMethod.Update)
        {
            return new Fade(tweenObject, resultValue).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(updateMethod).StartTween();
        }
        #endregion

        public class ColorChange : TweenCaseFunction<SpriteRenderer, Color>
        {
            public ColorChange(SpriteRenderer tweenObject, Color resultValue) : base(tweenObject, resultValue)
            {
                parentObject = tweenObject.gameObject;

                startValue = tweenObject.color;
            }

            public override bool Validate()
            {
                return parentObject != null;
            }

            public override void DefaultComplete()
            {
                tweenObject.color = resultValue;
            }

            public override void Invoke(float deltaTime)
            {
                tweenObject.color = Color.LerpUnclamped(startValue, resultValue, Interpolate(state));
            }
        }

        public class Fade : TweenCaseFunction<SpriteRenderer, float>
        {
            public Fade(SpriteRenderer tweenObject, float resultValue) : base(tweenObject, resultValue)
            {
                parentObject = tweenObject.gameObject;

                startValue = tweenObject.color.a;
            }

            public override bool Validate()
            {
                return parentObject != null;
            }

            public override void DefaultComplete()
            {
                tweenObject.color = new Color(tweenObject.color.r, tweenObject.color.g, tweenObject.color.b, resultValue);
            }

            public override void Invoke(float deltaTime)
            {
                tweenObject.color = new Color(tweenObject.color.r, tweenObject.color.g, tweenObject.color.b, Mathf.LerpUnclamped(startValue, resultValue, Interpolate(state)));
            }
        }
    }
}
