using UnityEngine;

namespace NebulaSoft
{
    [RequireComponent(typeof(CanvasGroup))]
    public class FadeTweenBehavior : TweenBehavior<CanvasGroup, float>
    {
        protected override float TargetValue
        {
            get => TargetComponent.alpha;
            set => TargetComponent.alpha = value;
        }

        protected override void StartLoop(float delay)
        {
            TargetValue = startValue;
            tweenCase = TargetComponent.DOFade(endValue, duration);

            base.StartLoop(delay);
        }

        protected override void IncrementLoopChangeValues()
        {
            var difference = endValue - startValue;
            startValue = endValue;
            endValue += difference;
        }
    }
}
