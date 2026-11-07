using UnityEngine;

namespace NebulaSoft
{
    public class ConstantRotation : MonoBehaviour, IUIPageElement
    {
        [SerializeField] Vector3 rotationSpeed = new Vector3(0, 0, 100);

        [SerializeField] bool scaleAnimation;
        [SerializeField] DuoVector3 minMaxScale;

        private TweenCase scaleTween;

        public void Init(UIPage page)
        {

        }

        public void OnPageStateChanged(bool state)
        {
            enabled = state;
        }

        private void Update()
        {
            transform.Rotate(rotationSpeed * Time.unscaledDeltaTime);

            if(scaleAnimation)
            {
                if(scaleTween == null || !scaleTween.IsActive)
                {
                    Vector3 targetValue = transform.localScale == minMaxScale.firstValue ? minMaxScale.secondValue : minMaxScale.firstValue;

                    scaleTween = transform.DOScale(targetValue, 4f).SetEasing(Ease.Type.SineInOut);
                }    
            }
        }
    }
}
