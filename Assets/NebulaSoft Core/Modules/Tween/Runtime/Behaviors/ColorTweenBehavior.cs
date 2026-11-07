using UnityEngine;
using UnityEngine.UI;

namespace NebulaSoft
{
    [RequireComponent(typeof(Graphic))]
    public class ColorTweenBehavior : TweenBehavior<Graphic, Color>
    {
        protected override Color TargetValue
        {
            get => TargetComponent.color;
            set => TargetComponent.color = value;
        }

        protected override void StartLoop(float delay)
        {
            TargetValue = startValue;
            tweenCase = TargetComponent.DOColor(endValue, duration);

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
