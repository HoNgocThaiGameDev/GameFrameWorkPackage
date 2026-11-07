using UnityEngine.UI;

namespace NebulaSoft
{
    public static class ImageTweenCases
    {
        #region Extensions
        /// <summary>
        /// Change image fill
        /// </summary>
        public static TweenCase DOFillAmount(this Image tweenObject, float resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod updateMethod = UpdateMethod.Update)
        {
            return new ImageFill(tweenObject, resultValue).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(updateMethod).StartTween();
        }
        #endregion

        public class ImageFill : TweenCaseFunction<Image, float>
        {
            public ImageFill(Image tweenObject, float resultValue) : base(tweenObject, resultValue)
            {
                parentObject = tweenObject.gameObject;

                startValue = tweenObject.fillAmount;
            }

            public override bool Validate()
            {
                return parentObject != null;
            }

            public override void DefaultComplete()
            {
                tweenObject.fillAmount = resultValue;
            }

            public override void Invoke(float deltaTime)
            {
                tweenObject.fillAmount = startValue + (resultValue - startValue) * Interpolate(state);
            }
        }
    }
}
