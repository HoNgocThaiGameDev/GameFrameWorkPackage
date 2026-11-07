using UnityEngine;

namespace NebulaSoft
{
    [RequireComponent(typeof(Canvas))]
    public class FadeOverlayPanel : BaseOverlayPanel
    {
        [SerializeField] Ease.Type showEasingType;
        [SerializeField] Ease.Type hideEasingType;

        private CanvasGroup canvasGroup;
        private TweenCase tweenCase;

        public override void Init()
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0.0f;
        }

        public override void Show(float duration, SimpleCallback onCompleted)
        {
            tweenCase.KillActive();
            tweenCase = canvasGroup.DOFade(1.0f, duration, unscaledTime: true).SetEasing(showEasingType).OnComplete(onCompleted);
        }

        public override void Hide(float duration, SimpleCallback onCompleted)
        {
            tweenCase.KillActive();
            tweenCase = canvasGroup.DOFade(0.0f, duration, unscaledTime: true).SetEasing(hideEasingType).OnComplete(onCompleted);
        }

        public override void Clear()
        {
            tweenCase.KillActive();
        }
    }
}
