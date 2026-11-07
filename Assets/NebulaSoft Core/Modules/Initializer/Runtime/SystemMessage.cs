using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NebulaSoft
{
    [RequireComponent(typeof(Canvas), typeof(CanvasScaler))]
    public class SystemMessage : MonoBehaviour
    {
        private static SystemMessage floatingMessage;

        [Header("Messages")]
        [SerializeField] RectTransform messagePanelRectTransform;
        [SerializeField] TextMeshProUGUI messageText;

        [Header("Loading")]
        [SerializeField] GameObject loadingPanelObject;
        [SerializeField] TextMeshProUGUI loadingStatusText;
        [SerializeField] RectTransform loadingIconRectTransform;

        private TweenCase animationTweenCase;

        private CanvasGroup messagePanelCanvasGroup;

        private bool isLoadingActive;

        public void Init()
        {
            if (floatingMessage != null) return;

            floatingMessage = this;

            CanvasScaler canvasScaler = gameObject.GetComponent<CanvasScaler>();
            canvasScaler.MatchSize();

            messagePanelCanvasGroup = gameObject.AddComponent<CanvasGroup>();

            messageText.AddEvent(EventTriggerType.PointerClick, (data) => OnPanelClick());

            loadingPanelObject.SetActive(false);
            messagePanelRectTransform.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (isLoadingActive)
            {
                loadingIconRectTransform.Rotate(0, 0, -50 * Time.unscaledDeltaTime);
            }
        }

        private void OnPanelClick()
        {
            UIAudioFeedback.PlayButtonSound();

            if (floatingMessage.animationTweenCase != null && !floatingMessage.animationTweenCase.IsCompleted)
                floatingMessage.animationTweenCase.Kill();

            floatingMessage.animationTweenCase = floatingMessage.messagePanelCanvasGroup.DOFade(0, 0.3f, unscaledTime: true).SetEasing(Ease.Type.CircOut).OnComplete(delegate
            {
                floatingMessage.messagePanelRectTransform.gameObject.SetActive(false);
            });
        }

        public static void ShowMessage(string message, float duration = 2.5f)
        {
            if(floatingMessage != null)
            {
                if (floatingMessage.isLoadingActive) return;

                if (floatingMessage.animationTweenCase != null && !floatingMessage.animationTweenCase.IsCompleted)
                    floatingMessage.animationTweenCase.Kill();

                floatingMessage.messageText.text = message;

                floatingMessage.messagePanelRectTransform.gameObject.SetActive(true);

                floatingMessage.messagePanelCanvasGroup.alpha = 1.0f;
                floatingMessage.animationTweenCase = Tween.DelayedCall(duration, delegate
                {
                    floatingMessage.animationTweenCase = floatingMessage.messagePanelCanvasGroup.DOFade(0, 0.5f, unscaledTime: true).SetEasing(Ease.Type.CircOut).OnComplete(delegate
                    {
                        floatingMessage.messagePanelRectTransform.gameObject.SetActive(false);
                    });
                }, unscaledTime: true);
            }
            else
            {
                Debug.Log("[System Message]: " + message);
                Debug.LogError("[System Message]: ShowMessage() method has called, but module isn't initialized!");
            }
        }

        public static void ShowLoadingPanel()
        {
            if (floatingMessage == null) return;
            if (floatingMessage.isLoadingActive) return;

            // Disable message panel if it is active
            floatingMessage.animationTweenCase.KillActive();
            floatingMessage.messagePanelRectTransform.gameObject.SetActive(false);

            // Activate loading
            floatingMessage.isLoadingActive = true;
            floatingMessage.loadingPanelObject.SetActive(true);
        }

        public static void ChangeLoadingMessage(string message)
        {
            if (floatingMessage == null) return;

            floatingMessage.loadingStatusText.text = message;
        }

        public static void HideLoadingPanel()
        {
            if (floatingMessage == null) return;

            // Disable loading
            floatingMessage.isLoadingActive = false;
            floatingMessage.loadingPanelObject.SetActive(false);
        }
    }
}
