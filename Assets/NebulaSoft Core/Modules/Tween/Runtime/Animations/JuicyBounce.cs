using UnityEngine;

namespace NebulaSoft
{
    [System.Serializable]
    public class JuicyBounce
    {
        [SerializeField] float bounceDuration;
        [SerializeField] float maxBounceDepth = 2f;

        [Space]
        [SerializeField] AnimationCurve bounceScaleX;
        [SerializeField] AnimationCurve bounceScaleY;
        [SerializeField] AnimationCurve bounceScaleZ;

        private float bounceValue;
        private float bounceDepth;

        private TweenCase bounceCase;

        private Transform transform;

        public void Init(Transform transform)
        {
            this.transform = transform;

            bounceValue = 0;
            bounceDepth = 1;
        }

        public void Bounce()
        {
            if (bounceCase != null && bounceCase.IsActive)
            {
                bounceCase.Kill();

                bounceDepth += 0.05f;

                if (bounceDepth > maxBounceDepth) bounceDepth = maxBounceDepth;
            }

            bounceCase = new TweenCaseJuicyBounce(this, GetStartBounceTime(), 1).SetDuration(bounceDuration).OnComplete(delegate
            {
                bounceValue = 0;
                bounceDepth = 1;
            }).StartTween();
        }

        private void BounceTween(float t)
        {
            bounceValue = t;

            transform.localScale = new Vector3(Mathf.Pow(bounceScaleX.Evaluate(t), bounceDepth), Mathf.Pow(bounceScaleY.Evaluate(t), bounceDepth), Mathf.Pow(bounceScaleZ.Evaluate(t), bounceDepth));
        }

        private float GetStartBounceTime()
        {
            var target = bounceScaleY.Evaluate(bounceValue);
            const int sampleCount = 100;
            const float tolerance = 0.01f;
            for (int i = 0; i <= sampleCount; i++)
            {
                float sampleTime = (float)i / sampleCount;
                if (Mathf.Abs(bounceScaleY.Evaluate(sampleTime) - target) < tolerance) return sampleTime;
            }

            // The current time is an exact match even when the curve is too steep to sample.
            return bounceValue;
        }

        public void Clear()
        {
            if (bounceCase != null)
            {
                bounceCase.Kill();

                bounceValue = 0;
                bounceDepth = 1;

                bounceCase = null;
            }
        }

        public class TweenCaseJuicyBounce : TweenCase
        {
            private float startValue;
            private float resultValue;

            private JuicyBounce juicyBounce;

            public TweenCaseJuicyBounce(JuicyBounce juicyBounce, float startValue, float resultValue)
            {
                this.startValue = startValue;
                this.resultValue = resultValue;

                this.juicyBounce = juicyBounce;

                // Get parent object
                parentObject = juicyBounce.transform.gameObject;
            }

            public override bool Validate()
            {
                return parentObject != null;
            }

            public override void DefaultComplete()
            {
                juicyBounce.BounceTween(resultValue);
            }

            public override void Invoke(float deltaTime)
            {
                juicyBounce.BounceTween(startValue + (resultValue - startValue) * Interpolate(State));
            }
        }
    }
}
