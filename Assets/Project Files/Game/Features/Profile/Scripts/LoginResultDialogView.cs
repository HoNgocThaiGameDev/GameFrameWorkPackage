using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NebulaSoft
{
    public sealed class LoginResultDialogView : MonoBehaviour
    {
        private static readonly Vector2 FailedPanelSize = new Vector2(680f, 1020f);
        private static readonly Vector2 SuccessPanelSize = new Vector2(680f, 970f);

        [Header("Layout")]
        [SerializeField] RectTransform panel;
        [SerializeField] RectTransform titleRibbon;
        [SerializeField] RectTransform title;
        [SerializeField] RectTransform closeButtonRect;

        [Header("Content")]
        [SerializeField] Image panelImage;
        [SerializeField] TextMeshProUGUI titleText;
        [SerializeField] TextMeshProUGUI messageText;
        [SerializeField] Image statusBadge;
        [SerializeField] Sprite successStatusSprite;
        [SerializeField] Sprite failedStatusSprite;

        [Header("Buttons")]
        [SerializeField] Button closeButton;
        [SerializeField] Button retryButton;

        private Action closeRequested;
        private Action retryRequested;

        public Sprite PanelSprite => panelImage != null ? panelImage.sprite : null;
        public Sprite CloseButtonSprite => GetButtonSprite(closeButton);
        public Sprite RetryButtonSprite => GetButtonSprite(retryButton);

        public void Init(Action onCloseRequested, Action onRetryRequested)
        {
            closeRequested = onCloseRequested;
            retryRequested = onRetryRequested;

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(OnCloseClicked);
                closeButton.onClick.AddListener(OnCloseClicked);
            }

            if (retryButton != null)
            {
                retryButton.onClick.RemoveListener(OnRetryClicked);
                retryButton.onClick.AddListener(OnRetryClicked);
            }
        }

        public void ShowSuccess()
        {
            ApplyState(
                "LOGIN SUCCESSFUL",
                "Your progress is now\nsaved",
                successStatusSprite,
                showRetry: false);
        }

        public void ShowSyncSuccess()
        {
            ApplyState(
                "SYNC SUCCESSFUL",
                "Your progress is now\nsynced",
                successStatusSprite,
                showRetry: false);
        }

        public void ShowFailed(string message = null)
        {
            ApplyState(
                "LOGIN FAILED",
                string.IsNullOrWhiteSpace(message)
                    ? "Facebook sign-in wasn't\nsuccessful"
                    : message,
                failedStatusSprite,
                showRetry: true);
        }

        public void ShowConnectionFailed()
        {
            ApplyState(
                "NO CONNECTION",
                "Your device progress is safe.\nPlease retry.",
                failedStatusSprite,
                showRetry: true);
        }

        public void ShowSyncFailed(string message = null)
        {
            ApplyState(
                "SYNC FAILED",
                string.IsNullOrWhiteSpace(message)
                    ? "Your device progress is safe.\nPlease retry."
                    : message,
                failedStatusSprite,
                showRetry: true);
        }

        public void HideImmediate()
        {
            gameObject.SetActive(false);
        }

        public void SetRetryInteractable(bool interactable)
        {
            if (retryButton != null)
                retryButton.interactable = interactable;
        }

        private void ApplyState(string dialogTitle, string message, Sprite statusSprite, bool showRetry)
        {
            gameObject.SetActive(true);

            if (titleText != null)
                titleText.text = dialogTitle;

            if (messageText != null)
                messageText.text = message;

            if (statusBadge != null)
                statusBadge.sprite = statusSprite;

            if (retryButton != null)
                retryButton.gameObject.SetActive(showRetry);

            ApplyLayout(showRetry);
        }

        private void ApplyLayout(bool showRetry)
        {
            bool failed = showRetry;

            if (panel != null)
                panel.sizeDelta = failed ? FailedPanelSize : SuccessPanelSize;

            SetAnchoredY(titleRibbon, failed ? 510f : 485f);
            SetAnchoredY(title, failed ? 515f : 490f);
            SetAnchoredY(closeButtonRect, failed ? 470f : 445f);
        }

        private void OnCloseClicked()
        {
            closeRequested?.Invoke();
        }

        private void OnRetryClicked()
        {
            retryRequested?.Invoke();
        }

        private static void SetAnchoredY(RectTransform target, float y)
        {
            if (target == null)
                return;

            Vector2 position = target.anchoredPosition;
            position.y = y;
            target.anchoredPosition = position;
        }

        private static Sprite GetButtonSprite(Button button)
        {
            if (button == null)
                return null;

            Image image = button.targetGraphic as Image ?? button.GetComponent<Image>();
            return image != null ? image.sprite : null;
        }
    }
}
