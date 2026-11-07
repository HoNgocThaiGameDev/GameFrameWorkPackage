using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace NebulaSoft
{
    public class LoadingGraphics : MonoBehaviour
    {
        [SerializeField] Camera loadingCamera;
        [SerializeField] CanvasScaler canvasScaler;
        [SerializeField] CanvasGroup canvasGroup;

        [Space]
        [SerializeField] Image backgroundImage;
        [SerializeField] Image loadingImage;
        [SerializeField] TextMeshProUGUI loadingMessageText;
        [SerializeField] TextMeshProUGUI loadingPercentageText;
        [SerializeField] GameObject loadingbarObject;
        [SerializeField] Button retryButton;

        private GameLoading loadingController;

        public void Init(GameLoading loadingController)
        {
            this.loadingController = loadingController;

            DontDestroyOnLoad(gameObject);

            canvasScaler.MatchSize();

            retryButton.onClick.AddListener(OnRetryButtonClicked);
            retryButton.gameObject.SetActive(false);

            loadingbarObject.SetActive(true);

            SetLoadingState(0.0f, "Loading..");
        }

        public void ShowErrorMessage(string message)
        {
            loadingbarObject.SetActive(false);
            retryButton.gameObject.SetActive(true);

            loadingMessageText.text = message;
        }

        public void HideErrorMessage()
        {
            loadingbarObject.SetActive(true);
            retryButton.gameObject.SetActive(false);

            loadingMessageText.text = "Loading..";
        }

        private void OnRetryButtonClicked()
        {
            loadingController.RetryConnection();
        }

        public void SetLoadingState(float state, string message)
        {
            loadingImage.fillAmount = state;
            loadingPercentageText.text = string.Format("{0}%", (state * 100).ToString("0"));
            loadingMessageText.text = message;
        }

        public void OnLoadingFinished()
        {
            canvasGroup.DOFade(0.0f, 0.6f, unscaledTime: true).OnComplete(delegate
            {
                gameObject.SetActive(false);
                Destroy(gameObject);
                AdsManager.EnableBanner();
            });
        }
    }
}
