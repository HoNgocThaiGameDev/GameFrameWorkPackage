namespace NebulaSoft
{
    public static class SlicedFilledImageExtensions
	{
		public static TweenCase DOFillAmount(this SlicedFilledImage tweenObject, float resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod tweenType = UpdateMethod.Update)
		{
			return new TweenCaseSlicedImageFill(tweenObject, resultValue).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(tweenType).StartTween();
		}

		public class TweenCaseSlicedImageFill : TweenCaseFunction<SlicedFilledImage, float>
		{
			public TweenCaseSlicedImageFill(SlicedFilledImage tweenObject, float resultValue) : base(tweenObject, resultValue)
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
				tweenObject.fillAmount = startValue + (resultValue - startValue) * Interpolate(State);
			}
		}
	}
}