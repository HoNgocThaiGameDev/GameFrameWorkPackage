using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using NebulaSoft.IAPStore;

namespace NebulaSoft
{
    public class UIAddLivesPanel : UIPage, IPopupWindow, IPausePopup
    {
        [SerializeField] RectTransform panel;
        [SerializeField] Vector3 hidePos;

        [SerializeField] Image backgroundImage;

        [SerializeField] Button rvButton;
        [SerializeField] Button coinsButton;
        [SerializeField] Button closeButton;

        [Space]
        [SerializeField] CurrencyAmount refillPrice;
        [SerializeField] Image currencyImage;
        [SerializeField] TMP_Text coinsRefillPriceText;

        [Space]
        [SerializeField] GameObject timerGameObject;
        [SerializeField] GameObject fullGameObject;
        [SerializeField] TMP_Text livesAmountText;
        [SerializeField] TMP_Text timeText;
        [SerializeField] AudioClip lifeRecievedAudio;

        private Vector3 showPos;
        private Color backColor;

        public bool IsOpened => canvas.enabled;

        private SimpleBoolCallback panelClosed;

        private void OnEnable()
        {
            LivesSystem.StatusChanged += OnStatusChanged;
        }

        private void OnDisable()
        {
            LivesSystem.StatusChanged -= OnStatusChanged;
        }

        public override void Init()
        {
            backColor = backgroundImage.color;
            showPos = panel.anchoredPosition;

            rvButton.onClick.AddListener(OnRvButtonClick);
            coinsButton.onClick.AddListener(OnCoinsButtonClick);
            closeButton.onClick.AddListener(OnCloseButtonClicked);

            LivesRemoteConfigData overrideData = LivesSystem.RemoteConfigData;
            if(overrideData != null)
            {
                CurrencyType currencyType = CurrencyType.Coins;
                if(System.Enum.TryParse(overrideData.currency, out currencyType))
                {
                    refillPrice = new CurrencyAmount(currencyType, overrideData.price);
                }
                else
                {
                    refillPrice = new CurrencyAmount(refillPrice.CurrencyType, overrideData.price);
                }
            }

            currencyImage.sprite = refillPrice.Currency.Icon;
            coinsRefillPriceText.text = refillPrice.Amount.ToString();

            OnStatusChanged(LivesSystem.Status);

            panelClosed = null;
        }

        public override void PlayShowAnimation()
        {
            backgroundImage.color = Color.clear;
            backgroundImage.DOColor(backColor, 0.3f, unscaledTime: true);

            panel.anchoredPosition = hidePos;
            panel.DOAnchoredPosition(showPos, 0.3f, unscaledTime: true).SetEasing(Ease.Type.SineOut);

            UIController.OnPageOpened(this);
        }

        public override void PlayHideAnimation()
        {
            backgroundImage.DOColor(Color.clear, 0.3f, unscaledTime: true);
            panel.DOAnchoredPosition(hidePos, 0.3f, unscaledTime: true).SetEasing(Ease.Type.SineIn).OnComplete(() =>
            {
                UIController.OnPageClosed(this);
            });
        }

        private void OnStatusChanged(LivesStatus status)
        {
            livesAmountText.text = status.LivesCount.ToString();

            if (status.NewLifeTimerEnabled)
            {
                timerGameObject.SetActive(true);
                fullGameObject.SetActive(false);

                timeText.text = LivesSystem.GetFormatedTime(status.NewLifeTime);
            }
            else
            {
                timerGameObject.SetActive(false);
                fullGameObject.SetActive(true);
            }
        }

        public void OnCloseButtonClicked()
        {
            UIController.HidePage<UIAddLivesPanel>();

            panelClosed?.Invoke(false);
        }

        public void OnRvButtonClick()
        {
            rvButton.interactable = false;

            AdsManager.ShowRewardBasedVideo(success =>
            {
                UIController.HidePage<UIAddLivesPanel>();

                if (success)
                {
                    LivesSystem.AddLife(1, true);

                    if (lifeRecievedAudio != null)
                        AudioController.PlaySound(lifeRecievedAudio);

                    panelClosed?.Invoke(true);
                }

                rvButton.interactable = true;
            }, "RestoreLife");

        }

        public void OnCoinsButtonClick()
        {
            if (CurrencyController.HasAmount(refillPrice.CurrencyType, refillPrice.Amount))
            {
                CurrencyController.Substract(refillPrice.CurrencyType, refillPrice.Amount, "RefillLives");

                LivesSystem.RefillLifes();

                UIController.HidePage<UIAddLivesPanel>();

                if (lifeRecievedAudio != null)
                    AudioController.PlaySound(lifeRecievedAudio);

                panelClosed?.Invoke(true);
            }
            else
            {
                UIStore.OpenAsOverlay();
            }

        }

        public static void Show(SimpleBoolCallback onPanelClosed = null)
        {
            UIAddLivesPanel addLivesPanel = UIController.GetPage<UIAddLivesPanel>();
            if (addLivesPanel != null)
            {
                addLivesPanel.panelClosed = onPanelClosed;

                UIController.ShowPage<UIAddLivesPanel>();
            }
            else
            {
                onPanelClosed?.Invoke(false);
            }
        }

        public static bool Exists()
        {
            return UIController.HasPage<UIAddLivesPanel>();
        }
    }
}
