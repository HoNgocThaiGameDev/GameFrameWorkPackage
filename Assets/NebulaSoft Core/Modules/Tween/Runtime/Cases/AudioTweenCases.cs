using UnityEngine;

namespace NebulaSoft
{
    public static class AudioTweenCases
    {
        #region Extensions
        /// <summary>
        /// Change audio source volume
        /// </summary>
        public static TweenCase DOVolume(this AudioSource tweenObject, float resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod updateMethod = UpdateMethod.Update)
        {
            return new Volume(tweenObject, resultValue).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(updateMethod).StartTween();
        }
        #endregion

        public class Volume : TweenCaseFunction<AudioSource, float>
        {
            public Volume(AudioSource tweenObject, float resultValue) : base(tweenObject, resultValue)
            {
                parentObject = tweenObject.gameObject;

                startValue = tweenObject.volume;
            }

            public override bool Validate()
            {
                return parentObject != null;
            }

            public override void DefaultComplete()
            {
                tweenObject.volume = resultValue;
            }

            public override void Invoke(float deltaTime)
            {
                tweenObject.volume = Mathf.LerpUnclamped(startValue, resultValue, Interpolate(state));
            }
        }
    }
}
