using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NebulaSoft
{
    public static class TextTweenCases
    {
        #region Extensions
        /// <summary>
        /// Change text font size
        /// </summary>
        public static TweenCase DOFontSize(this Text tweenObject, int resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod updateMethod = UpdateMethod.Update)
        {
            return new TextFontSize(tweenObject, resultValue).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(updateMethod).StartTween();
        }

        /// <summary>
        /// Change text font size
        /// </summary>
        public static TweenCase DOFontSize(this TextMesh tweenObject, int resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod updateMethod = UpdateMethod.Update)
        {
            return new TextMeshFontSize(tweenObject, resultValue).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(updateMethod).StartTween();
        }

        /// <summary>
        /// Change text color alpha
        /// </summary>
        public static TweenCase DOFade(this TextMesh tweenObject, float resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod updateMethod = UpdateMethod.Update)
        {
            return new TextMeshFade(tweenObject, resultValue).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(updateMethod).StartTween();
        }

        /// <summary>
        /// Change color of text
        /// </summary>
        public static TweenCase DOColor(this TextMesh tweenObject, Color resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod updateMethod = UpdateMethod.Update)
        {
            return new TextMeshColor(tweenObject, resultValue).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(updateMethod).StartTween();
        }
        #endregion

        public class TextFontSize : TweenCaseFunction<Text, int>
        {
            public TextFontSize(Text tweenObject, int resultValue) : base(tweenObject, resultValue)
            {
                parentObject = tweenObject.gameObject;

                startValue = tweenObject.fontSize;
            }

            public override bool Validate()
            {
                return parentObject != null;
            }

            public override void DefaultComplete()
            {
                tweenObject.fontSize = resultValue;
            }

            public override void Invoke(float deltaTime)
            {
                tweenObject.fontSize = (int)Mathf.LerpUnclamped(startValue, resultValue, Interpolate(state));
            }
        }

        public class TextMeshFontSize : TweenCaseFunction<TextMesh, int>
        {
            public TextMeshFontSize(TextMesh tweenObject, int resultValue) : base(tweenObject, resultValue)
            {
                parentObject = tweenObject.gameObject;

                startValue = tweenObject.fontSize;
            }

            public override bool Validate()
            {
                return parentObject != null;
            }

            public override void DefaultComplete()
            {
                tweenObject.fontSize = resultValue;
            }

            public override void Invoke(float deltaTime)
            {
                tweenObject.fontSize = (int)Mathf.LerpUnclamped(startValue, resultValue, Interpolate(state));
            }
        }

        public class TextMeshFade : TweenCaseFunction<TextMesh, float>
        {
            public TextMeshFade(TextMesh tweenObject, float resultValue) : base(tweenObject, resultValue)
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

        public class TextMeshColor : TweenCaseFunction<TextMesh, Color>
        {
            public TextMeshColor(TextMesh tweenObject, Color resultValue) : base(tweenObject, resultValue)
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

        public class TMPFontSize : TweenCaseFunction<TMP_Text, float>
        {
            public TMPFontSize(TMP_Text tweenObject, float resultValue) : base(tweenObject, resultValue)
            {
                parentObject = tweenObject.gameObject;

                startValue = tweenObject.fontSize;
            }

            public override bool Validate()
            {
                return parentObject != null;
            }

            public override void DefaultComplete()
            {
                tweenObject.fontSize = resultValue;
            }

            public override void Invoke(float deltaTime)
            {
                tweenObject.fontSize = Mathf.LerpUnclamped(startValue, resultValue, Interpolate(state));
            }
        }
    }
}
