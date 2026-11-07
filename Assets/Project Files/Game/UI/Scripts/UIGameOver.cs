using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NebulaSoft
{
    public class UIGameOver : UIPage
    {
        [BoxGroup("References", "References")]
        [SerializeField] CanvasGroup backgroundFade;
        [BoxGroup("References")]
        [SerializeField] RectTransform safeAreaRectTransform;

        [BoxGroup("Content", "Content")]
        [SerializeField] GameObject gameOverPanel;
        [BoxGroup("Content")]
        [SerializeField] UIScaleAnimation levelFailed;

        [BoxGroup("Revive", "Revive")]
        [SerializeField] GameObject revivePanel;
        [BoxGroup("Revive")]
        [SerializeField] TextMeshProUGUI reviveTimerText;
        [BoxGroup("Revive")]
        [SerializeField] Image reviveFillbarImage;
        [BoxGroup("Revive")]
        [SerializeField] Button reviveButton;
        [BoxGroup("Revive")]
        [SerializeField] Button reviveSkipButton;

        [BoxGroup("Buttons", "Buttons")]
        [SerializeField] Button replayButton;

        private bool showRevive;
        private bool resetWinStreakOnFinalFailure;
        private TweenCase floatTweenCase;

        public override void Init()
        {
            NotchSaveArea.RegisterRectTransform(safeAreaRectTransform);

            replayButton.onClick.AddListener(OnReplayButtonClicked);
            reviveButton.onClick.AddListener(OnReviveButtonClicked);
            reviveSkipButton.onClick.AddListener(OnReviveSkipButtonClicked);

            showRevive = false;
        }

        public override void PlayShowAnimation()
        {
            backgroundFade.alpha = 0.0f;

            if (showRevive)
            {
                backgroundFade.DOFade(0.7f, 0.3f);

                revivePanel.SetActive(true);
                gameOverPanel.SetActive(false);

                int reviveDuration = GameData.Data != null ? GameData.Data.ReviveOfferSeconds : 5;
                reviveTimerText.text = reviveDuration.ToString();
                reviveFillbarImage.fillAmount = 1f;
                reviveButton.interactable = true;
                reviveSkipButton.interactable = true;
                floatTweenCase = Tween.DoFloat(reviveDuration, 0, reviveDuration, remaining =>
                {
                    reviveFillbarImage.fillAmount = remaining / reviveDuration;
                    reviveTimerText.text = Mathf.CeilToInt(remaining).ToString();
                }, unscaledTime: true).OnComplete(ShowLevelFailUI);
            }
            else
            {
                ShowLevelFailUI();
            }

            UIController.OnPageOpened(this);
        }

        public override void PlayHideAnimation()
        {
            floatTweenCase.KillActive();
            UIController.OnPageClosed(this);
        }

        private void ShowLevelFailUI()
        {
            if (resetWinStreakOnFinalFailure)
            {
                resetWinStreakOnFinalFailure = false;
                WinStreakProgress.Reset();
            }

#if MODULE_MONETIZATION
            if (GameData.Data == null || GameData.Data.InterstitialOnFinalFailure)
            {
                AdsManager.ShowInterstitial(_ => ApplyFinalFailure());
                return;
            }
#endif
            ApplyFinalFailure();
        }

        private void ApplyFinalFailure()
        {
            LivesSystem.TakeLife();
            backgroundFade.DOFade(1f, 0.3f);
            revivePanel.SetActive(false);
            gameOverPanel.SetActive(true);
        }

        private void OnDestroy()
        {
            floatTweenCase.KillActive();
        }

        public static void Show(bool showRevive, bool resetWinStreakOnFinalFailure = false)
        {
            UIGameOver gameOverUI = UIController.GetPage<UIGameOver>();
            gameOverUI.showRevive = showRevive && GameController.CanOfferRewardedRevive();
            gameOverUI.resetWinStreakOnFinalFailure = resetWinStreakOnFinalFailure;

            UIController.ShowPage<UIGameOver>();
        }

        #region Buttons 
        public void OnReplayButtonClicked()
        {
            GameController.Replay();
        }

        private void OnReviveButtonClicked()
        {
            var offer = floatTweenCase;
            if (offer == null || !offer.IsActive || offer.IsPaused) return;

            offer.Pause();
            reviveButton.interactable = false;
            reviveSkipButton.interactable = false;

            AdsManager.ShowRewardBasedVideo(reward =>
            {
                if (this == null || floatTweenCase != offer || !offer.IsActive) return;
                if (reward)
                {
                    offer.Kill();
                    GameController.ReviveFromRewardedAd();
                }
                else
                {
                    offer.CompleteActive();
                }
            });
        }

        private void OnReviveSkipButtonClicked()
        {
            floatTweenCase.CompleteActive();
        }
        #endregion
    }
}
