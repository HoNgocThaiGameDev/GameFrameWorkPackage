using UnityEngine;

namespace NebulaSoft
{
    [System.Serializable]
    public class CustomEasingFunction : Ease.IEasingFunction
    {
        [SerializeField] string name;
        public string Name => name;

        [SerializeField] AnimationCurve easingCurve;

        private float totalEasingTime;

        public CustomEasingFunction(string name, AnimationCurve easingCurve)
        {
            this.name = name;
            this.easingCurve = easingCurve;

            Init();
        }

        public void Init()
        {
            totalEasingTime = easingCurve.keys[easingCurve.keys.Length - 1].time;
        }

        public float Interpolate(float p)
        {
            return easingCurve.Evaluate(p * totalEasingTime);
        }
    }
}