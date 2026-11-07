using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NebulaSoft
{
    [System.Serializable]
    public class UIFadeAnimation
    {
        [SerializeField] CanvasGroup fadeCanvasGroup;

        private TweenCase fadeTweenCase;

        public UIFadeAnimation(GameObject gameObject)
        {
            fadeCanvasGroup = gameObject.GetComponent<CanvasGroup>();
            if(fadeCanvasGroup == null)
                fadeCanvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        public UIFadeAnimation(Transform transform) : this(transform.gameObject) { }
        public UIFadeAnimation(MaskableGraphic graphics) : this(graphics.gameObject) { }
        public UIFadeAnimation(UIBehaviour uiBehavior) : this(uiBehavior.gameObject) { }

        public void Show(float duration = 0.4f, float delay = 0f, bool immediately = false, SimpleCallback onCompleted = null)
        {
            if (fadeCanvasGroup == null)
            {
                Debug.LogError("CanvasGroup value cannot be null. Please ensure it is assigned in the Inspector or passed through the constructor.");

                onCompleted?.Invoke();

                return;
            }

            fadeTweenCase.KillActive();

            if (immediately)
            {
                fadeCanvasGroup.alpha = 1f;
                onCompleted?.Invoke();

                return;
            }

            fadeCanvasGroup.alpha = 0f;
            fadeTweenCase = fadeCanvasGroup.DOFade(1, duration, delay, unscaledTime: true).OnComplete(() =>
            {
                onCompleted?.Invoke();
            });
        }

        public void Hide(float duration = 0.4f, float delay = 0f, bool immediately = false, SimpleCallback onCompleted = null)
        {
            if (fadeCanvasGroup == null)
            {
                Debug.LogError("CanvasGroup value cannot be null. Please ensure it is assigned in the Inspector or passed through the constructor.");

                onCompleted?.Invoke();

                return;
            }

            fadeTweenCase.KillActive();

            if (immediately)
            {
                fadeCanvasGroup.alpha = 0f;
                onCompleted?.Invoke();

                return;
            }

            fadeCanvasGroup.alpha = 1f;
            fadeTweenCase = fadeCanvasGroup.DOFade(0, duration, delay, unscaledTime: true).OnComplete(() =>
            {
                onCompleted?.Invoke();
            });
        }
    }
}
