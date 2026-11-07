using UnityEngine;
using UnityEngine.UI;

namespace NebulaSoft
{
    public static class GraphicTweenCases 
    {
        #region Extensions
        /// <summary>
        /// Change color of image
        /// </summary>
        public static TweenCase DOColor(this Graphic tweenObject, Color resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod tweenType = UpdateMethod.Update)
        {
            return new GraphicColor(tweenObject, resultValue).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(tweenType).StartTween();
        }

        /// <summary>
        /// Change graphic color alpha
        /// </summary>
        public static TweenCase DOFade(this Graphic tweenObject, float resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod tweenType = UpdateMethod.Update)
        {
            return new Fade(tweenObject, resultValue).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(tweenType).StartTween();
        }
        #endregion

        public class GraphicColor : TweenCaseFunction<Graphic, Color>
        {
            public GraphicColor(Graphic tweenObject, Color resultValue) : base(tweenObject, resultValue)
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

        public class Fade : TweenCaseFunction<Graphic, float>
        {
            public Fade(Graphic tweenObject, float resultValue) : base(tweenObject, resultValue)
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
