using UnityEngine;

namespace NebulaSoft
{
    public static class AnimationTweenCases
    {
        #region Extensions
        public static TweenCase WaitForEnd(this Animation tweenObject, float delay = 0, bool unscaledTime = false, UpdateMethod updateMethod = UpdateMethod.Update)
        {
            return new Wait(tweenObject).SetDelay(delay).SetUnscaledMode(unscaledTime).SetUpdateMethod(updateMethod).StartTween();
        }

        public static TweenCase DOLayerWeight(this Animator tweenObject, string layerName, float resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod updateMethod = UpdateMethod.Update)
        {
            return new AnimatorWeight(tweenObject, resultValue, tweenObject.GetLayerIndex(layerName)).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(updateMethod).StartTween();
        }

        public static TweenCase DOLayerWeight(this Animator tweenObject, int layerID, float resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod updateMethod = UpdateMethod.Update)
        {
            return new AnimatorWeight(tweenObject, resultValue, layerID).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(updateMethod).StartTween();
        }
        #endregion

        public class Wait : TweenCase
        {
            public Animation animation;

            public Wait(Animation animation)
            {
                this.animation = animation;

                duration = float.MaxValue;
            }

            public override void DefaultComplete()
            {

            }

            public override void Invoke(float deltaTime)
            {
                if (!animation.isPlaying)
                    Complete();
            }

            public override bool Validate()
            {
                return animation != null;
            }
        }

        public class AnimatorWeight : TweenCaseFunction<Animator, float>
        {
            private int layerID;

            public AnimatorWeight(Animator tweenObject, float resultValue, int layerID) : base(tweenObject, resultValue)
            {
                this.layerID = layerID;

                parentObject = tweenObject.gameObject;

                if (Validate()) startValue = tweenObject.GetLayerWeight(layerID);
            }

            public override bool Validate()
            {
                return tweenObject != null && layerID >= 0 && layerID < tweenObject.layerCount;
            }

            public override void DefaultComplete()
            {
                tweenObject.SetLayerWeight(layerID, resultValue);
            }

            public override void Invoke(float deltaTime)
            {
                tweenObject.SetLayerWeight(layerID, Mathf.LerpUnclamped(startValue, resultValue, Interpolate(state)));
            }
        }
    }
}
