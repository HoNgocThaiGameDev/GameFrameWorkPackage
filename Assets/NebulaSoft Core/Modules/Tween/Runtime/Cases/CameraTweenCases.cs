using UnityEngine;

namespace NebulaSoft
{
    public static class CameraTweenCases
    {
        #region Extensions
        public static TweenCase DOSize(this Camera tweenObject, float resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod updateMethod = UpdateMethod.Update)
        {
            return new CameraSize(tweenObject, resultValue).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(updateMethod).StartTween();
        }

        public static TweenCase DOFieldOfView(this Camera tweenObject, float resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod updateMethod = UpdateMethod.Update)
        {
            return new CameraFOV(tweenObject, resultValue).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(updateMethod).StartTween();
        }
        #endregion

        public class CameraSize : TweenCaseFunction<Camera, float>
        {
            public CameraSize(Camera tweenObject, float resultValue) : base(tweenObject, resultValue)
            {
                parentObject = tweenObject.gameObject;

                startValue = tweenObject.orthographicSize;
            }

            public override bool Validate()
            {
                return parentObject != null;
            }

            public override void DefaultComplete()
            {
                tweenObject.orthographicSize = resultValue;
            }

            public override void Invoke(float deltaTime)
            {
                tweenObject.orthographicSize = Mathf.LerpUnclamped(startValue, resultValue, Interpolate(state));
            }
        }

        public class CameraFOV : TweenCaseFunction<Camera, float>
        {
            public CameraFOV(Camera tweenObject, float resultValue) : base(tweenObject, resultValue)
            {
                parentObject = tweenObject.gameObject;

                startValue = tweenObject.fieldOfView;
            }

            public override bool Validate()
            {
                return parentObject != null;
            }

            public override void DefaultComplete()
            {
                tweenObject.fieldOfView = resultValue;
            }

            public override void Invoke(float deltaTime)
            {
                tweenObject.fieldOfView = Mathf.LerpUnclamped(startValue, resultValue, Interpolate(state));
            }
        }
    }
}
