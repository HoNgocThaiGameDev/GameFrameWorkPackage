using UnityEngine;
using UnityEngine.UI;

namespace NebulaSoft
{
    public class DummyOverlayPanel : BaseOverlayPanel
    {
        private Image image;
        private TweenCase fadeTweenCase;

        public override void Init()
        {
            GameObject overlayObject = new GameObject("Overlay Image");
            overlayObject.transform.SetParent(canvas.transform);
            overlayObject.transform.ResetLocal();

            RectTransform overlayRectTransform = overlayObject.AddComponent<RectTransform>();
            overlayRectTransform.anchorMin = new Vector2(0, 0);
            overlayRectTransform.anchorMax = new Vector2(1, 1);
            overlayRectTransform.sizeDelta = Vector2.zero;

            image = overlayObject.AddComponent<Image>();
            image.color = new Color(0, 0, 0, 0);
            image.raycastTarget = true;
        }

        public override void Show(float duration, SimpleCallback onCompleted)
        {
            fadeTweenCase.KillActive();
            fadeTweenCase = image.DOFade(1.0f, duration, unscaledTime: true).SetEasing(Ease.Type.Linear).OnComplete(onCompleted);
        }

        public override void Hide(float duration, SimpleCallback onCompleted)
        {
            fadeTweenCase.KillActive();
            fadeTweenCase = image.DOFade(0.0f, duration, unscaledTime: true).SetEasing(Ease.Type.Linear).OnComplete(onCompleted);
        }

        public override void Clear()
        {
            fadeTweenCase.KillActive();

            Object.Destroy(gameObject);
        }
    }
}
