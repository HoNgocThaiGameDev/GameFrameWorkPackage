using UnityEngine;

namespace NebulaSoft
{
    public static class TransformTweenCases
    {
        #region Extensions
        /// <summary>
        /// Changes rotation angle of object.
        /// </summary>
        public static TweenCase DORotate(this Component tweenObject, Vector3 resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod tweenType = UpdateMethod.Update)
        {
            return new RotateAngle(tweenObject.transform, resultValue).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(tweenType).StartTween();
        }

        /// <summary>
        /// Changes quaternion rotation of object.
        /// </summary>
        public static TweenCase DORotate(this Component tweenObject, Quaternion resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod tweenType = UpdateMethod.Update)
        {
            return new RotateQuaternion(tweenObject.transform, resultValue).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(tweenType).StartTween();
        }

        /// <summary>
        /// Changes local quaternion rotation of object.
        /// </summary>
        public static TweenCase DOLocalRotate(this Component tweenObject, Quaternion resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod tweenType = UpdateMethod.Update)
        {
            return new LocalRotate(tweenObject.transform, resultValue).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(tweenType).StartTween();
        }

        /// <summary>
        /// Changes local angle rotation of object.
        /// </summary>
        public static TweenCase DOLocalRotate(this Component tweenObject, Vector3 resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod tweenType = UpdateMethod.Update)
        {
            return new RotateAngle(tweenObject.transform, resultValue).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(tweenType).StartTween();
        }

        /// <summary>
        /// Changes object rotation by given vector during specified time.
        /// </summary>
        public static TweenCase DORotateConstant(this Component tweenObject, Vector3 rotationVector, float time, float delay = 0, bool unscaledTime = false, UpdateMethod tweenType = UpdateMethod.Update)
        {
            return new RotateConstant(tweenObject.transform, rotationVector).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(tweenType).StartTween();
        }

        /// <summary>
        /// Changes position of object.
        /// </summary>
        public static TweenCase DOMove(this Component tweenObject, Vector3 resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod tweenType = UpdateMethod.Update)
        {
            return new Position(tweenObject.transform, resultValue).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(tweenType).StartTween();
        }

        /// <summary>
        /// Changes position of object.
        /// </summary>
        public static TweenCase DOMove(this Component tweenObject, Transform resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod tweenType = UpdateMethod.Update)
        {
            return new Position(tweenObject.transform, resultValue.position).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(tweenType).StartTween();
        }

        /// <summary>
        /// Changes position of object by bezier curve.
        /// </summary>
        public static TweenCase DOBezierMove(this Component tweenObject, Vector3 resultValue, float upOffset, float rightOffset, float forwardOffset, float time, float delay = 0, bool unscaledTime = false, UpdateMethod tweenType = UpdateMethod.Update)
        {
            return new BezierPosition(tweenObject.transform, resultValue, upOffset, rightOffset, forwardOffset).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(tweenType).StartTween();
        }

        /// <summary>
        /// Changes position of object by bezier curve.
        /// </summary>
        public static TweenCase DOBezierMove(this Component tweenObject, Vector3 resultValue, Vector3 controlPoint1, float time, float delay = 0, bool unscaledTime = false, UpdateMethod tweenType = UpdateMethod.Update)
        {
            return new BezierPosition(tweenObject.transform, resultValue, controlPoint1).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(tweenType).StartTween();
        }

        /// <summary>
        /// Changes position of object by bezier curve.
        /// </summary>
        public static TweenCase DOBezierMove(this Component tweenObject, Vector3 resultValue, Vector3 controlPoint1, Vector3 controlPoint2, float time, float delay = 0, bool unscaledTime = false, UpdateMethod tweenType = UpdateMethod.Update)
        {
            return new BezierPosition(tweenObject.transform, resultValue, controlPoint1, controlPoint2).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(tweenType).StartTween();
        }

        /// <summary>
        /// Follow a target transform.
        /// </summary>
        public static TweenCase DoFollow(this Component tweenObject, Transform target, float speed, float minimumDistance, float delay, bool unscaledTime = false, UpdateMethod tweenType = UpdateMethod.Update)
        {
            return new Follow(tweenObject.transform, target, speed, minimumDistance).SetDelay(delay).SetDuration(float.MaxValue).SetUnscaledMode(unscaledTime).SetUpdateMethod(tweenType).StartTween();
        }

        /// <summary>
        /// Follow a target transform by bezier curve.
        /// </summary>
        public static TweenCase DOBezierFollow(this Component tweenObject, Transform resultValue, float upOffset, float rightOffset, float forwardOffset, float time, float delay = 0, bool unscaledTime = false, UpdateMethod tweenType = UpdateMethod.Update)
        {
            return new BezierFollow(tweenObject.transform, resultValue, upOffset, rightOffset, forwardOffset).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(tweenType).StartTween();
        }

        /// <summary>
        /// Changes x position of object.
        /// </summary>
        public static TweenCase DOMoveX(this Component tweenObject, float resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod tweenType = UpdateMethod.Update)
        {
            return new PositionX(tweenObject.transform, resultValue).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(tweenType).StartTween();
        }

        /// <summary>
        /// Changes y position of object.
        /// </summary>
        public static TweenCase DOMoveY(this Component tweenObject, float resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod tweenType = UpdateMethod.Update)
        {
            return new PositionY(tweenObject.transform, resultValue).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(tweenType).StartTween();
        }

        /// <summary>
        /// Changes z position of object.
        /// </summary>
        public static TweenCase DOMoveZ(this Component tweenObject, float resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod tweenType = UpdateMethod.Update)
        {
            return new PositionZ(tweenObject.transform, resultValue).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(tweenType).StartTween();
        }

        /// <summary>
        /// Changes x,z positions of object.
        /// </summary>
        public static TweenCase DOMoveXZ(this Component tweenObject, float resultValueX, float resultValueZ, float time, float delay = 0, bool unscaledTime = false, UpdateMethod tweenType = UpdateMethod.Update)
        {
            return new PositionXZ(tweenObject.transform, resultValueX, resultValueZ).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(tweenType).StartTween();
        }

        /// <summary>
        /// Changes local scale of object.
        /// </summary>
        public static TweenCase DOScale(this Component tweenObject, Vector3 resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod tweenType = UpdateMethod.Update)
        {
            return new Scale(tweenObject.transform, resultValue).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(tweenType).StartTween();
        }

        /// <summary>
        /// Changes local scale of object.
        /// </summary>
        public static TweenCase DOScale(this Component tweenObject, float resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod tweenType = UpdateMethod.Update)
        {
            return new Scale(tweenObject.transform, new Vector3(resultValue, resultValue, resultValue)).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(tweenType).StartTween();
        }

        /// <summary>
        /// Changes local scale of object twice.
        /// </summary>
        public static TweenCase DOPushScale(this Component tweenObject, Vector3 firstScale, Vector3 secondScale, float firstScaleTime, float secondScaleTime, Ease.Type firstScaleEasing = Ease.Type.Linear, Ease.Type secondScaleEasing = Ease.Type.Linear, float delay = 0, bool unscaledTime = false, UpdateMethod tweenType = UpdateMethod.Update)
        {
            return new PushScale(tweenObject.transform, firstScale, secondScale, firstScaleTime, secondScaleTime, firstScaleEasing, secondScaleEasing).SetDelay(delay).SetDuration(firstScaleTime + secondScaleTime).SetUnscaledMode(unscaledTime).SetUpdateMethod(tweenType).StartTween();
        }

        /// <summary>
        /// Changes local scale of object twice.
        /// </summary>
        public static TweenCase DOPushScale(this Component tweenObject, float firstScale, float secondScale, float firstScaleTime, float secondScaleTime, Ease.Type firstScaleEasing = Ease.Type.Linear, Ease.Type secondScaleEasing = Ease.Type.Linear, float delay = 0, bool unscaledTime = false, UpdateMethod tweenType = UpdateMethod.Update)
        {
            return new PushScale(tweenObject.transform, firstScale.ToVector3(), secondScale.ToVector3(), firstScaleTime, secondScaleTime, firstScaleEasing, secondScaleEasing).SetDelay(delay).SetDuration(firstScaleTime + secondScaleTime).SetUnscaledMode(unscaledTime).SetUpdateMethod(tweenType).StartTween();
        }

        /// <summary>
        /// Changes x scale of object.
        /// </summary>
        public static TweenCase DOScaleX(this Component tweenObject, float resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod tweenType = UpdateMethod.Update)
        {
            return new ScaleX(tweenObject.transform, resultValue).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(tweenType).StartTween();
        }

        /// <summary>
        /// Changes y scale of object.
        /// </summary>
        public static TweenCase DOScaleY(this Component tweenObject, float resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod tweenType = UpdateMethod.Update)
        {
            return new ScaleY(tweenObject.transform, resultValue).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(tweenType).StartTween();
        }

        /// <summary>
        /// Changes z scale of object.
        /// </summary>
        public static TweenCase DOScaleZ(this Component tweenObject, float resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod tweenType = UpdateMethod.Update)
        {
            return new ScaleZ(tweenObject.transform, resultValue).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(tweenType).StartTween();
        }

        /// <summary>
        /// Scale transform up and down.
        /// </summary>
        public static TweenCase DOPingPongScale(this Component tweenObject, float minValue, float maxValue, float time, Ease.Type positiveScaleEasing, Ease.Type negativeScaleEasing, float delay = 0, bool unscaledTime = false)
        {
            return new PingPongScale(tweenObject.transform, minValue, maxValue, time, positiveScaleEasing, negativeScaleEasing).SetDelay(delay).SetUnscaledMode(unscaledTime).StartTween();
        }

        /// <summary>
        /// Changes local position of object.
        /// </summary>
        public static TweenCase DOLocalMove(this Component tweenObject, Vector3 resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod tweenType = UpdateMethod.Update)
        {
            return new LocalMove(tweenObject.transform, resultValue).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(tweenType).StartTween();
        }

        /// <summary>
        /// Changes x local position of object.
        /// </summary>
        public static TweenCase DOLocalMoveX(this Component tweenObject, float resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod tweenType = UpdateMethod.Update)
        {
            return new LocalPositionX(tweenObject.transform, resultValue).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(tweenType).StartTween();
        }

        /// <summary>
        /// Changes y local position of object.
        /// </summary>
        public static TweenCase DOLocalMoveY(this Component tweenObject, float resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod tweenType = UpdateMethod.Update)
        {
            return new LocalPositionY(tweenObject.transform, resultValue).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(tweenType).StartTween();
        }

        /// <summary>
        /// Changes z local position of object.
        /// </summary>
        public static TweenCase DOLocalMoveZ(this Component tweenObject, float resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod tweenType = UpdateMethod.Update)
        {
            return new LocalPositionZ(tweenObject.transform, resultValue).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(tweenType).StartTween();
        }

        /// <summary>
        /// Rotates object face to position.
        /// </summary>
        public static TweenCase DOLookAt(this Component tweenObject, Vector3 resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod tweenType = UpdateMethod.Update)
        {
            return new LookAt(tweenObject.transform, resultValue).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(tweenType).StartTween();
        }

        /// <summary>
        /// Rotates 2D object face to position.
        /// </summary>
        public static TweenCase DOLookAt2D(this Component tweenObject, Vector3 resultValue, TransformTweenCases.LookAt2D.LookAtType type, float time, float delay = 0, bool unscaledTime = false, UpdateMethod tweenType = UpdateMethod.Update)
        {
            return new LookAt2D(tweenObject.transform, resultValue, type).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(tweenType).StartTween();
        }

        /// <summary>
        /// Shake object in 3D space
        /// </summary>
        public static TweenCase DOShake(this Component tweenObject, float magnitude, float time, float delay = 0, bool unscaledTime = false, UpdateMethod tweenType = UpdateMethod.Update)
        {
            return new Shake(tweenObject.transform, magnitude).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(tweenType).StartTween();
        }
        #endregion

        public class RotateAngle : TweenCaseFunction<Transform, Vector3>
        {
            public RotateAngle(Transform tweenObject, Vector3 resultValue) : base(tweenObject, resultValue)
            {
                var startRotation = tweenObject.eulerAngles;

                if (resultValue.x - startRotation.x > 180)
                    resultValue.x -= 360;
                if (resultValue.y - startRotation.y > 180)
                    resultValue.y -= 360;
                if (resultValue.z - startRotation.z > 180)
                    resultValue.z -= 360;

                if (resultValue.x - startRotation.x < -180)
                    resultValue.x += 360;
                if (resultValue.y - startRotation.y < -180)
                    resultValue.y += 360;
                if (resultValue.z - startRotation.z < -180)
                    resultValue.z += 360;

                parentObject = tweenObject.gameObject;

                startValue = tweenObject.eulerAngles;
            }

            public override bool Validate()
            {
                return parentObject != null;
            }

            public override void DefaultComplete()
            {
                tweenObject.eulerAngles = resultValue;
            }

            public override void Invoke(float deltaTime)
            {
                tweenObject.eulerAngles = Vector3.LerpUnclamped(startValue, resultValue, Interpolate(state));
            }
        }

        public class RotateQuaternion : TweenCaseFunction<Transform, Quaternion>
        {
            public RotateQuaternion(Transform tweenObject, Quaternion resultValue) : base(tweenObject, resultValue)
            {
                parentObject = tweenObject.gameObject;

                startValue = tweenObject.rotation;
            }

            public override bool Validate()
            {
                return parentObject != null;
            }

            public override void DefaultComplete()
            {
                tweenObject.rotation = resultValue;
            }

            public override void Invoke(float deltaTime)
            {
                tweenObject.rotation = Quaternion.LerpUnclamped(startValue, resultValue, Interpolate(state));
            }
        }

        public class LocalRotate : TweenCaseFunction<Transform, Quaternion>
        {
            public LocalRotate(Transform tweenObject, Quaternion resultValue) : base(tweenObject, resultValue)
            {
                parentObject = tweenObject.gameObject;

                startValue = tweenObject.localRotation;
            }

            public override bool Validate()
            {
                return parentObject != null;
            }

            public override void DefaultComplete()
            {
                tweenObject.localRotation = resultValue;
            }

            public override void Invoke(float deltaTime)
            {
                tweenObject.localRotation = Quaternion.LerpUnclamped(startValue, resultValue, Interpolate(state));
            }
        }

        public class LocalRotateAngle : TweenCaseFunction<Transform, Vector3>
        {
            public LocalRotateAngle(Transform tweenObject, Vector3 resultValue) : base(tweenObject, resultValue)
            {
                parentObject = tweenObject.gameObject;

                startValue = tweenObject.localEulerAngles;
            }

            public override bool Validate()
            {
                return (parentObject != null && parentObject.activeSelf);
            }

            public override void DefaultComplete()
            {
                tweenObject.localEulerAngles = resultValue;
            }

            public override void Invoke(float deltaTime)
            {
                tweenObject.localEulerAngles = Vector3.LerpUnclamped(startValue, resultValue, Interpolate(state));
            }
        }

        public class RotateConstant : TweenCase
        {
            private Transform objectTransform;
            private Vector3 rotationVector;
            private float elapsed;

            public RotateConstant(Transform tweenObject, Vector3 rotationVector)
            {
                objectTransform = tweenObject;
                parentObject = tweenObject.gameObject;
                this.rotationVector = rotationVector;
            }

            public override bool Validate()
            {
                return parentObject != null;
            }

            public override void DefaultComplete()
            {
                Invoke(Mathf.Max(0, duration - elapsed));
            }

            public override void Invoke(float deltaTime)
            {
                objectTransform.Rotate(rotationVector * deltaTime);
                elapsed += deltaTime;
            }
        }

        public class Position : TweenCaseFunction<Transform, Vector3>
        {
            public Position(Transform tweenObject, Vector3 resultValue) : base(tweenObject, resultValue)
            {
                parentObject = tweenObject.gameObject;

                startValue = tweenObject.position;
            }

            public override bool Validate()
            {
                return parentObject != null;
            }

            public override void DefaultComplete()
            {
                tweenObject.position = resultValue;
            }

            public override void Invoke(float deltaTime)
            {
                tweenObject.position = Vector3.LerpUnclamped(startValue, resultValue, Interpolate(state));
            }
        }

        public class PositionTransform : TweenCaseFunction<Transform, Transform>
        {
            private Vector3 startPosition;

            public PositionTransform(Transform tweenObject, Transform resultValue) : base(tweenObject, resultValue)
            {
                parentObject = tweenObject.gameObject;

                startPosition = tweenObject.position;
            }

            public override bool Validate()
            {
                return tweenObject != null && resultValue != null;
            }

            public override void DefaultComplete()
            {
                tweenObject.position = resultValue.position;
            }

            public override void Invoke(float deltaTime)
            {
                tweenObject.position = Vector3.LerpUnclamped(startPosition, resultValue.position, Interpolate(state));
            }
        }

        public class BezierPosition : TweenCaseFunction<Transform, Vector3>
        {
            private Vector3 controlPoint1;
            private Vector3 controlPoint2;

            private bool isQuadratic;

            public BezierPosition(Transform tweenObject, Vector3 resultValue, float upOffset, float rightOffset, float forwardOffset) : base(tweenObject, resultValue)
            {
                parentObject = tweenObject.gameObject;

                startValue = tweenObject.position;

                var direction = resultValue - startValue;

                var rotation = Quaternion.FromToRotation(Vector3.forward, direction);

                var right = rotation * Vector3.right;

                isQuadratic = true;

                controlPoint1 = startValue + direction * 0.5f + Vector3.up * upOffset + right * rightOffset + direction.normalized * forwardOffset;
            }

            public BezierPosition(Transform tweenObject, Vector3 resultValue, Vector3 controlPoint) : base(tweenObject, resultValue)
            {
                parentObject = tweenObject.gameObject;
                startValue = tweenObject.position;

                isQuadratic = true;

                controlPoint1 = controlPoint;
            }

            public BezierPosition(Transform tweenObject, Vector3 resultValue, Vector3 controlPoint1, Vector3 controlPoint2) : base(tweenObject, resultValue)
            {
                parentObject = tweenObject.gameObject;
                startValue = tweenObject.position;

                isQuadratic = false;

                this.controlPoint1 = controlPoint1;
                this.controlPoint2 = controlPoint2;
            }

            public override bool Validate()
            {
                return parentObject != null;
            }

            public override void DefaultComplete()
            {
                tweenObject.position = resultValue;
            }

            public override void Invoke(float deltaTime)
            {
                if (isQuadratic)
                {
                    tweenObject.position = Bezier.EvaluateQuadratic(startValue, controlPoint1, resultValue, Interpolate(state));
                }
                else
                {
                    tweenObject.position = Bezier.EvaluateCubic(startValue, controlPoint1, controlPoint2, resultValue, Interpolate(state));
                }
            }
        }

        public class BezierFollow : TweenCase
        {
            private Vector3 startPosition;

            private Transform fromTransform;
            private Transform toTransform;

            private Vector3 keyPoint1;
            private Vector3 keyPoint2;

            private float upOffset;
            private float rightOffset;
            private float forwardOffset;

            private bool isQuadratic;

            public BezierFollow(Transform tweenObject, Transform followTarget, float upOffset, float rightOffset, float forwardOffset)
            {
                parentObject = tweenObject.gameObject;

                startPosition = tweenObject.position;

                fromTransform = tweenObject;
                toTransform = followTarget;

                this.upOffset = upOffset;
                this.rightOffset = rightOffset;
                this.forwardOffset = forwardOffset;

                isQuadratic = true;

                if (toTransform != null) RecalculateKeyPoints();
            }

            private void RecalculateKeyPoints()
            {
                var direction = toTransform.position - fromTransform.position;

                var rotation = Quaternion.FromToRotation(Vector3.forward, direction);

                var right = rotation * Vector3.right;

                keyPoint1 = fromTransform.position + direction * 0.5f + Vector3.up * upOffset + right * rightOffset + direction.normalized * forwardOffset;
            }

            public override bool Validate()
            {
                return fromTransform != null && toTransform != null;
            }

            public override void DefaultComplete()
            {
                fromTransform.position = toTransform.position;
            }

            public override void Invoke(float deltaTime)
            {
                if (isQuadratic)
                {
                    fromTransform.position = Bezier.EvaluateQuadratic(startPosition, keyPoint1, toTransform.position, Interpolate(state));
                }
                else
                {
                    fromTransform.position = Bezier.EvaluateCubic(startPosition, keyPoint1, keyPoint2, toTransform.position, Interpolate(state));
                }
            }
        }

        public class Follow : TweenCase
        {
            private Transform tweenObject;
            private Transform target;

            private float minimumDistanceSqr;
            private float speed;

            public Follow(Transform tweenObject, Transform target, float speed, float minimumDistance)
            {
                parentObject = tweenObject.gameObject;

                this.tweenObject = tweenObject;
                this.target = target;
                this.speed = speed;

                minimumDistanceSqr = Mathf.Pow(minimumDistance, 2);
            }

            public override bool Validate()
            {
                return parentObject != null && target != null;
            }

            public override void DefaultComplete()
            {
                tweenObject.position = target.position;
            }

            public override void Invoke(float deltaTime)
            {
                tweenObject.position = Vector3.MoveTowards(tweenObject.position, target.position, deltaTime * speed);

                if (Vector3.SqrMagnitude(tweenObject.position - target.position) <= minimumDistanceSqr)
                    Complete();
            }
        }

        public class PositionX : TweenCaseFunction<Transform, float>
        {
            public PositionX(Transform tweenObject, float resultValue) : base(tweenObject, resultValue)
            {
                parentObject = tweenObject.gameObject;

                startValue = tweenObject.position.x;
            }

            public override bool Validate()
            {
                return parentObject != null;
            }

            public override void DefaultComplete()
            {
                tweenObject.position = new Vector3(resultValue, tweenObject.position.y, tweenObject.position.z);
            }

            public override void Invoke(float deltaTime)
            {
                tweenObject.position = new Vector3(Mathf.LerpUnclamped(startValue, resultValue, Interpolate(state)), tweenObject.position.y, tweenObject.position.z);
            }
        }

        public class PositionY : TweenCaseFunction<Transform, float>
        {
            public PositionY(Transform tweenObject, float resultValue) : base(tweenObject, resultValue)
            {
                parentObject = tweenObject.gameObject;

                startValue = tweenObject.position.y;
            }

            public override bool Validate()
            {
                return parentObject != null;
            }

            public override void DefaultComplete()
            {
                tweenObject.position = new Vector3(tweenObject.position.x, resultValue, tweenObject.position.z);
            }

            public override void Invoke(float deltaTime)
            {
                tweenObject.position = new Vector3(tweenObject.position.x, Mathf.LerpUnclamped(startValue, resultValue, Interpolate(state)), tweenObject.position.z);
            }
        }

        public class PositionXZ : TweenCase
        {
            private Transform tweenObject;

            private float resultValueX;
            private float resultValueZ;

            private float startValueX;
            private float startValueZ;

            private float intepolatedState;

            public PositionXZ(Transform tweenObject, float resultValueX, float resultValueZ)
            {
                this.tweenObject = tweenObject;

                this.resultValueX = resultValueX;
                this.resultValueZ = resultValueZ;

                parentObject = tweenObject.gameObject;

                startValueX = tweenObject.position.x;
                startValueZ = tweenObject.position.z;
            }

            public override bool Validate()
            {
                return parentObject != null;
            }

            public override void DefaultComplete()
            {
                tweenObject.position = new Vector3(resultValueX, tweenObject.position.y, resultValueZ);
            }

            public override void Invoke(float deltaTime)
            {
                intepolatedState = Interpolate(state);

                tweenObject.position = new Vector3(Mathf.LerpUnclamped(startValueX, resultValueX, intepolatedState), tweenObject.position.y, Mathf.LerpUnclamped(startValueZ, resultValueZ, intepolatedState));
            }
        }

        public class PositionZ : TweenCaseFunction<Transform, float>
        {
            public PositionZ(Transform tweenObject, float resultValue) : base(tweenObject, resultValue)
            {
                parentObject = tweenObject.gameObject;

                startValue = tweenObject.position.z;
            }

            public override bool Validate()
            {
                return parentObject != null;
            }

            public override void DefaultComplete()
            {
                tweenObject.position = new Vector3(tweenObject.position.x, tweenObject.position.y, resultValue);
            }

            public override void Invoke(float deltaTime)
            {
                tweenObject.position = new Vector3(tweenObject.position.x, tweenObject.position.y, Mathf.LerpUnclamped(startValue, resultValue, Interpolate(state)));
            }
        }

        public class Scale : TweenCaseFunction<Transform, Vector3>
        {
            public Scale(Transform tweenObject, Vector3 resultValue) : base(tweenObject, resultValue)
            {
                parentObject = tweenObject.gameObject;

                startValue = tweenObject.localScale;
            }

            public override bool Validate()
            {
                return parentObject != null;
            }

            public override void DefaultComplete()
            {
                tweenObject.localScale = resultValue;
            }

            public override void Invoke(float deltaTime)
            {
                tweenObject.localScale = Vector3.LerpUnclamped(startValue, resultValue, Interpolate(state));
            }
        }

        public class PushScale : TweenCase
        {
            public Transform tweenObject;

            public Vector3 startValue;
            public Vector3 firstScaleValue;
            public Vector3 secondScaleValue;

            public float firstTime;
            public float secondTime;

            private Ease.Type firstScaleEasing;
            private Ease.Type secondScaleEasing;

            private float relativeState;

            public PushScale(Transform tweenObject, Vector3 firstScaleValue, Vector3 secondScaleValue, float firstTime, float secondTime, Ease.Type firstScaleEasing, Ease.Type secondScaleEasing)
            {
                this.tweenObject = tweenObject;

                this.startValue = tweenObject.localScale;

                this.firstScaleValue = firstScaleValue;
                this.secondScaleValue = secondScaleValue;

                this.firstTime = firstTime;
                this.secondTime = secondTime;

                this.firstScaleEasing = firstScaleEasing;
                this.secondScaleEasing = secondScaleEasing;

                parentObject = tweenObject.gameObject;
            }

            public override bool Validate()
            {
                return parentObject != null;
            }

            public override void DefaultComplete()
            {
                tweenObject.localScale = secondScaleValue;
            }

            public override void Invoke(float deltaTime)
            {
                relativeState = duration * state;

                if (relativeState <= firstTime)
                {
                    tweenObject.localScale = Vector3.LerpUnclamped(startValue, firstScaleValue, Ease.Interpolate(Mathf.InverseLerp(0, firstTime, relativeState), firstScaleEasing));
                }
                else
                {
                    tweenObject.localScale = Vector3.LerpUnclamped(firstScaleValue, secondScaleValue, Ease.Interpolate(Mathf.InverseLerp(firstTime, duration, relativeState), secondScaleEasing));
                }
            }
        }

        public class ScaleX : TweenCaseFunction<Transform, float>
        {
            public ScaleX(Transform tweenObject, float resultValue) : base(tweenObject, resultValue)
            {
                parentObject = tweenObject.gameObject;

                startValue = tweenObject.localScale.x;
            }

            public override bool Validate()
            {
                return parentObject != null;
            }

            public override void DefaultComplete()
            {
                tweenObject.localScale = new Vector3(resultValue, tweenObject.localScale.y, tweenObject.localScale.z);
            }

            public override void Invoke(float deltaTime)
            {
                tweenObject.localScale = new Vector3(Mathf.LerpUnclamped(startValue, resultValue, Interpolate(state)), tweenObject.localScale.y, tweenObject.localScale.z);
            }
        }

        public class ScaleY : TweenCaseFunction<Transform, float>
        {
            public ScaleY(Transform tweenObject, float resultValue) : base(tweenObject, resultValue)
            {
                parentObject = tweenObject.gameObject;

                startValue = tweenObject.localScale.y;
            }

            public override bool Validate()
            {
                return parentObject != null;
            }

            public override void DefaultComplete()
            {
                tweenObject.localScale = new Vector3(tweenObject.localScale.x, resultValue, tweenObject.localScale.z);
            }

            public override void Invoke(float deltaTime)
            {
                tweenObject.localScale = new Vector3(tweenObject.localScale.x, Mathf.LerpUnclamped(startValue, resultValue, Interpolate(state)), tweenObject.localScale.z);
            }
        }

        public class ScaleZ : TweenCaseFunction<Transform, float>
        {
            public ScaleZ(Transform tweenObject, float resultValue) : base(tweenObject, resultValue)
            {
                parentObject = tweenObject.gameObject;

                startValue = tweenObject.localScale.z;
            }

            public override bool Validate()
            {
                return parentObject != null;
            }

            public override void DefaultComplete()
            {
                tweenObject.localScale = new Vector3(tweenObject.localScale.x, tweenObject.localScale.y, resultValue);
            }

            public override void Invoke(float deltaTime)
            {
                tweenObject.localScale = new Vector3(tweenObject.localScale.x, tweenObject.localScale.y, Mathf.LerpUnclamped(startValue, resultValue, Interpolate(state)));
            }
        }

        public class PingPongScale : TweenCase
        {
            private Transform tweenObject;

            private float minValue;
            private float maxValue;

            private Ease.IEasingFunction negativeEaseFunction;

            public PingPongScale(Transform tweenObject, float minValue, float maxValue, float duration, Ease.Type positiveScaleEasing, Ease.Type negativeScaleEasing)
            {
                this.tweenObject = tweenObject;

                this.minValue = minValue;
                this.maxValue = maxValue;

                this.duration = duration;

                parentObject = tweenObject.gameObject;

                easeFunction = Ease.GetFunction(positiveScaleEasing);
                negativeEaseFunction = Ease.GetFunction(negativeScaleEasing);
            }

            public override bool Validate()
            {
                return parentObject != null;
            }

            public override void DefaultComplete()
            {
                tweenObject.localScale = new Vector3(minValue, minValue, minValue);
            }

            public override void Invoke(float deltaTime)
            {
                float phase = state * 2;
                float scale = phase <= 1
                    ? Mathf.LerpUnclamped(minValue, maxValue, easeFunction.Interpolate(phase))
                    : Mathf.LerpUnclamped(maxValue, minValue, negativeEaseFunction.Interpolate(phase - 1));
                tweenObject.localScale = Vector3.one * scale;
            }
        }

        public class LocalMove : TweenCaseFunction<Transform, Vector3>
        {
            public LocalMove(Transform tweenObject, Vector3 resultValue) : base(tweenObject, resultValue)
            {
                parentObject = tweenObject.gameObject;

                startValue = tweenObject.localPosition;
            }

            public override bool Validate()
            {
                return parentObject != null;
            }

            public override void DefaultComplete()
            {
                tweenObject.localPosition = resultValue;
            }

            public override void Invoke(float deltaTime)
            {
                tweenObject.localPosition = Vector3.LerpUnclamped(startValue, resultValue, Interpolate(state));
            }
        }

        public class LocalPositionX : TweenCaseFunction<Transform, float>
        {
            public LocalPositionX(Transform tweenObject, float resultValue) : base(tweenObject, resultValue)
            {
                parentObject = tweenObject.gameObject;

                startValue = tweenObject.localPosition.x;
            }

            public override bool Validate()
            {
                return parentObject != null;
            }

            public override void DefaultComplete()
            {
                tweenObject.localPosition = new Vector3(resultValue, tweenObject.localPosition.y, tweenObject.localPosition.z);
            }

            public override void Invoke(float deltaTime)
            {
                tweenObject.localPosition = new Vector3(Mathf.LerpUnclamped(startValue, resultValue, Interpolate(state)), tweenObject.localPosition.y, tweenObject.localPosition.z);
            }
        }

        public class LocalPositionY : TweenCaseFunction<Transform, float>
        {
            public LocalPositionY(Transform tweenObject, float resultValue) : base(tweenObject, resultValue)
            {
                parentObject = tweenObject.gameObject;

                startValue = tweenObject.localPosition.y;
            }

            public override bool Validate()
            {
                return parentObject != null;
            }

            public override void DefaultComplete()
            {
                tweenObject.localPosition = new Vector3(tweenObject.localPosition.x, resultValue, tweenObject.localPosition.z);
            }

            public override void Invoke(float deltaTime)
            {
                tweenObject.localPosition = new Vector3(tweenObject.localPosition.x, Mathf.LerpUnclamped(startValue, resultValue, Interpolate(state)), tweenObject.localPosition.z);
            }
        }

        public class LocalPositionZ : TweenCaseFunction<Transform, float>
        {
            public LocalPositionZ(Transform tweenObject, float resultValue) : base(tweenObject, resultValue)
            {
                parentObject = tweenObject.gameObject;

                startValue = tweenObject.localPosition.z;
            }

            public override bool Validate()
            {
                return parentObject != null;
            }

            public override void DefaultComplete()
            {
                tweenObject.localPosition = new Vector3(tweenObject.localPosition.x, tweenObject.localPosition.y, resultValue);
            }

            public override void Invoke(float deltaTime)
            {
                tweenObject.localPosition = new Vector3(tweenObject.localPosition.x, tweenObject.localPosition.y, Mathf.LerpUnclamped(startValue, resultValue, Interpolate(state)));
            }
        }

        public class LookAt : TweenCaseFunction<Transform, Vector3>
        {
            private Quaternion startRotation;

            public LookAt(Transform tweenObject, Vector3 resultValue) : base(tweenObject, resultValue)
            {
                parentObject = tweenObject.gameObject;

                startValue = tweenObject.position;
                startRotation = tweenObject.rotation;
            }

            public override bool Validate()
            {
                return parentObject != null;
            }

            public override void DefaultComplete()
            {
                tweenObject.LookAt(resultValue);
            }

            public override void Invoke(float deltaTime)
            {
                var targetRotation = Quaternion.LookRotation(resultValue - startValue);

                // Smoothly rotate towards the target point.
                tweenObject.rotation = Quaternion.Slerp(startRotation, targetRotation, Interpolate(state));
            }
        }

        public class LookAt2D : TweenCaseFunction<Transform, Vector3>
        {
            public LookAtType type;
            private readonly float angleDelta;

            public LookAt2D(Transform tweenObject, Vector3 resultValue, LookAtType type) : base(tweenObject, resultValue)
            {
                parentObject = tweenObject.gameObject;

                this.type = type;

                startValue = tweenObject.eulerAngles;

                Vector2 direction = resultValue - tweenObject.position;
                if (direction.sqrMagnitude > 0)
                {
                    float rotationZ = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                    if (type == LookAtType.Up) rotationZ -= 90;
                    angleDelta = Mathf.DeltaAngle(startValue.z, rotationZ);
                }
            }

            public override bool Validate()
            {
                return parentObject != null;
            }

            public override void DefaultComplete()
            {
                tweenObject.rotation = Quaternion.Euler(0, 0, startValue.z + angleDelta);
            }

            public override void Invoke(float deltaTime)
            {
                tweenObject.rotation = Quaternion.Euler(0, 0, startValue.z + angleDelta * Interpolate(state));
            }

            public enum LookAtType
            {
                Up,
                Right,
                Forward
            }
        }

        public class Shake : TweenCase
        {
            private Transform tweenObject;
            private Vector3 startPosition;
            private float magnitude;

            public Shake(Transform tweenObject, float magnitude)
            {
                this.tweenObject = tweenObject;
                this.magnitude = magnitude;

                parentObject = tweenObject.gameObject;

                startPosition = tweenObject.position;
            }

            public override bool Validate()
            {
                return parentObject != null;
            }

            public override void DefaultComplete()
            {
                tweenObject.position = startPosition;
            }

            public override void Invoke(float timeDelta)
            {
                tweenObject.position = startPosition + Random.onUnitSphere * magnitude * Interpolate(state);
            }
        }
    }
}
