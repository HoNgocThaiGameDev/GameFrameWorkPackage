using UnityEngine;
using UnityEngine.UI;
using NebulaSoft.IAPStore;

namespace NebulaSoft
{
    public class UIMainMenu : UIPage
    {
        public readonly float BUTTONS_RIGHT_OFFSET_X = 300F;

        [BoxGroup("References", "References")]
        [SerializeField] RectTransform safeAreaRectTransform;
        [BoxGroup("References")]
        [SerializeField] RectTransform tapToPlayRect;

        [BoxGroup("Top Panel", "Top Panel")]
        [SerializeField] CurrencyUIPanelSimple coinsPanel;

        [BoxGroup("Side Buttons", "Side Buttons")]
        [SerializeField] UIMainMenuButton noAdsButton;
        [BoxGroup("Side Buttons")]
        [SerializeField] UIMainMenuButton storeButton;
        [BoxGroup("Side Buttons")]
        [SerializeField] Button dailyRewardButton;

        [BoxGroup("Static Map", "Static Map")]
        [SerializeField] StaticMapPanel staticMapPanel;
                
        private UIScaleAnimation coinsLabelScalable;
        private bool dailyRewardAutoShowAttempted;
        private DailyRewardPopupView dailyRewardPopup;

        private void OnEnable()
        {
            AdsManager.NoAdsEntitlementChanged += OnNoAdsEntitlementChanged;
        }

        private void OnDisable()
        {
            AdsManager.NoAdsEntitlementChanged -= OnNoAdsEntitlementChanged;
        }

        public override void Init()
        {
            coinsLabelScalable = new UIScaleAnimation(coinsPanel);
            coinsPanel.Init();

            staticMapPanel.Init();

            if (noAdsButton != null && noAdsButton.IsConfigured)
            {
                noAdsButton.Button.onClick.AddListener(NoAdButton);
            }

            if (storeButton != null && storeButton.IsConfigured)
            {
                storeButton.Init(BUTTONS_RIGHT_OFFSET_X);
                storeButton.Button.onClick.AddListener(StoreButton);
            }

            if (dailyRewardButton != null)
            {
                dailyRewardButton.onClick.AddListener(OpenDailyReward);
                UIAudioFeedback.RegisterButton(dailyRewardButton);
            }

            coinsPanel.AddButton.onClick.AddListener(AddCoinsButton);

            NotchSaveArea.RegisterRectTransform(safeAreaRectTransform);
        }

        #region Show/Hide

        public override void PlayShowAnimation()
        {
            // An SDK consent overlay can temporarily pause Unity. Avoid leaving the
            // currency panel at scale zero while that pause is active.
            coinsLabelScalable.Show(immediately: Time.timeScale <= 0f);
            SetNoAdsButtonActive(true);

            if (storeButton != null && storeButton.IsConfigured)
            {
                storeButton.Show();
            }

            UIController.OnPageOpened(this);

            if (!dailyRewardAutoShowAttempted)
            {
                dailyRewardAutoShowAttempted = true;
                // Temporarily disabled: Daily Reward is opened manually from DailyRewardButton.
                // GetDailyRewardPopup()?.TryShowIfClaimable();
            }
        }

        public override void PlayHideAnimation()
        {
            UIController.OnPageClosed(this);
        }

        #endregion

        #region Side Buttons

        private void SetNoAdsButtonActive(bool homeTabVisible)
        {
            if (noAdsButton == null || !noAdsButton.IsConfigured)
                return;

            // The prefab owns the No Ads button layout. Only its active state changes here.
            noAdsButton.SetActive(homeTabVisible && AdsManager.IsForcedAdEnabled());
        }

        private void OnNoAdsEntitlementChanged(bool hasNoAds)
        {
            SetNoAdsButtonActive(!hasNoAds && IsPageDisplayed);
        }

        public void SetHomeTabControlsVisible(bool visible)
        {
            SetNoAdsButtonActive(visible);
        }

        #endregion


        #region Buttons

        public void NoAdButton()
        {
            UIController.ShowPage<UINoAdsOffer>();
        }

        public void StoreButton()
        {
            UIController.ShowPage<UIStore>();
        }

        public void AddCoinsButton()
        {
            UIController.ShowPage<UIStore>();
        }

        public void OpenDailyReward()
        {
            GetDailyRewardPopup()?.Show();
        }

        private DailyRewardPopupView GetDailyRewardPopup()
        {
            if (dailyRewardPopup == null)
                dailyRewardPopup = FindFirstObjectByType<DailyRewardPopupView>(FindObjectsInactive.Include);

            return dailyRewardPopup;
        }

        #endregion
    }
}
