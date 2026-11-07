using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NebulaSoft
{
    public class FloatingTextHitBehavior : FloatingTextBaseBehavior
    {
        [SerializeField] TextMeshProUGUI floatingText;

        [Space]
        [SerializeField] float delay;
        [SerializeField] float disableDelay;
        [SerializeField] float startScale;
        [SerializeField] float time;
        [SerializeField] Ease.Type easing;

        [Space]
        [SerializeField] float scaleTime;
        [SerializeField] Ease.Type scaleEasing;

        private Vector3 defaultScale;

        private void Awake()
        {
            defaultScale = transform.localScale;
        }

        public override void Activate(string text, float scaleMultiplier, Color color)
        {
            floatingText.text = text;
            floatingText.color = color;

            int sign = Random.value >= 0.5f ? 1 : -1;

            transform.localScale = defaultScale * startScale * scaleMultiplier;
            transform.localRotation = Quaternion.Euler(70, 0, 18 * sign);

            Tween.DelayedCall(delay, delegate
            {
                transform.DOLocalRotate(Quaternion.Euler(70, 0, 0), time).SetEasing(easing).OnComplete(delegate
                {
                    Tween.DelayedCall(disableDelay, delegate
                    {
                        gameObject.SetActive(false);

                        InvokeCompleteEvent();
                    });
                });

                transform.DOScale(defaultScale, scaleTime).SetEasing(scaleEasing);
            });
        }
    }
}