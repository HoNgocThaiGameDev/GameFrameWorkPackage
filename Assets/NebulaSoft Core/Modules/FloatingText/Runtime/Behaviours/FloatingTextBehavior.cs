#pragma warning disable 0618

using UnityEngine;

namespace NebulaSoft
{
    public class FloatingTextBehavior : FloatingTextBaseBehavior
    {
        [Space]
        [SerializeField] Vector3 offset;
        [SerializeField] float time;
        [SerializeField] Ease.Type easing;

        [Space]
        [SerializeField] float scaleTime;
        [SerializeField] AnimationCurve scaleAnimationCurve;

        private Vector3 defaultScale;

        private TweenCase scaleTween;
        private TweenCase moveTween;

        private void Awake()
        {
            defaultScale = transform.localScale;
        }

        public override void Activate(string text, float scaleMultiplier, Color color)
        {
            textRef.text = text;
            textRef.color = color;

            transform.localScale = Vector3.zero;
            scaleTween = transform.DOScale(defaultScale * scaleMultiplier, scaleTime).SetCurveEasing(scaleAnimationCurve);
            moveTween = transform.DOMove(transform.position + offset, time).SetEasing(easing).OnComplete(delegate
             {
                 gameObject.SetActive(false);

                 InvokeCompleteEvent();
             });
        }

        public void AddOnTimeReached(float time, SimpleCallback callback)
        {
            if (moveTween.ExistsAndActive())
            {
                moveTween.OnTimeReached(time, callback);
            }
        }

        public void SetText(string text)
        {
            textRef.text = text;
        }

        public void Reset()
        {
            scaleTween.KillActive();
            moveTween.KillActive();
        }
    }
}