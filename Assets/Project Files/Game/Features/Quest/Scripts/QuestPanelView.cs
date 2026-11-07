using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace NebulaSoft
{
    [RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
    public sealed class QuestPanelView : MonoBehaviour
    {
        private const float TimerRefreshInterval = 1f;
        private static readonly Color LockedChestColor = new Color(0.34f, 0.34f, 0.34f, 0.65f);
        private static readonly Color ClaimableChestColor = Color.white;
        private static readonly Color ClaimedChestColor = new Color(0.58f, 0.58f, 0.58f, 0.62f);

        [Header("Category tabs")]
        [SerializeField] Button dailyButton;
        [SerializeField] Button weeklyButton;
        [SerializeField] Button eventButton;
        [SerializeField] Image tabStripImage;
        [SerializeField] Sprite dailyTabSprite;
        [SerializeField] Sprite weeklyTabSprite;
        [SerializeField] Sprite eventTabSprite;
        QuestTabTransitionAnimation tabTransitionAnimation;

        [Header("Header")]
        [SerializeField] RectTransform safeAreaTransform;
        [SerializeField] TextMeshProUGUI titleText;
        [SerializeField] TextMeshProUGUI timerText;
        [SerializeField] Button closeButton;

        [Header("Quest list")]
        [SerializeField] RectTransform questContent;
        [SerializeField] QuestElementView questElementPrefab;
        [SerializeField] GameObject emptyState;

        [Header("Milestone progress")]
        [SerializeField] GameObject milestonePanel;
        [SerializeField] TextMeshProUGUI milestoneText;
        [SerializeField] Slider milestoneSlider;
        [SerializeField] Image milestoneFill;
        [SerializeField] Button[] milestoneChestButtons;
        [SerializeField] Image[] milestoneChestImages;

        [Header("Reward popup")]
        [SerializeField] QuestRewardClaimPopup rewardClaimPopupPrefab;

        private readonly List<QuestDefinition> definitions = new List<QuestDefinition>();
        private readonly List<QuestMilestoneDefinition> milestones = new List<QuestMilestoneDefinition>();
        private readonly List<QuestElementView> itemViews = new List<QuestElementView>();
        private UnityAction[] milestoneChestCallbacks;
        private QuestRewardClaimPopup rewardClaimPopup;
        private QuestCategory activeCategory = QuestCategory.Daily;
        private bool initialized;
        private bool subscribed;
        private bool animateQuestListOnEnable;
        private float timerRefreshElapsed;

        public bool IsVisible => gameObject.activeSelf;

        private void Awake()
        {
            tabTransitionAnimation = GetComponent<QuestTabTransitionAnimation>();
            Initialize();
        }

        private void OnEnable()
        {
            Subscribe();
            Refresh(animateQuestListOnEnable);
            animateQuestListOnEnable = false;
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnDestroy()
        {
            if (dailyButton != null)
                dailyButton.onClick.RemoveListener(SelectDaily);
            if (weeklyButton != null)
                weeklyButton.onClick.RemoveListener(SelectWeekly);
            if (eventButton != null)
                eventButton.onClick.RemoveListener(SelectEvents);
            if (closeButton != null)
                closeButton.onClick.RemoveListener(GoHome);
            RemoveMilestoneButtonListeners();
            tabTransitionAnimation?.Stop();
        }

        private void Update()
        {
            if (!IsVisible)
                return;

            timerRefreshElapsed += Time.unscaledDeltaTime;
            if (timerRefreshElapsed < TimerRefreshInterval)
                return;

            timerRefreshElapsed = 0f;
            QuestService.RefreshPeriods();
            RefreshTimer();
        }

        public void Show()
        {
            Initialize();
            timerRefreshElapsed = 0f;
            if (!gameObject.activeSelf)
            {
                animateQuestListOnEnable = true;
                gameObject.SetActive(true);
                return;
            }

            Refresh(animateQuestList: true);
        }

        public void Hide(SimpleCallback onComplete = null)
        {
            if (gameObject.activeSelf)
                gameObject.SetActive(false);

            onComplete?.Invoke();
        }

        public void SelectDaily()
        {
            SelectCategory(QuestCategory.Daily);
        }

        public void SelectWeekly()
        {
            SelectCategory(QuestCategory.Weekly);
        }

        public void SelectEvents()
        {
            SelectCategory(QuestCategory.Event);
        }

        private void Initialize()
        {
            if (initialized)
                return;

            initialized = true;
            if (safeAreaTransform != null)
                NotchSaveArea.RegisterRectTransform(safeAreaTransform);

            if (dailyButton != null)
                dailyButton.onClick.AddListener(SelectDaily);
            if (weeklyButton != null)
                weeklyButton.onClick.AddListener(SelectWeekly);
            if (eventButton != null)
                eventButton.onClick.AddListener(SelectEvents);
            if (closeButton != null)
                closeButton.onClick.AddListener(GoHome);
            InitializeMilestoneButtons();
            UIAudioFeedback.RegisterButtons(transform);
        }

        private void Subscribe()
        {
            if (subscribed)
                return;

            QuestService.DataChanged += Refresh;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed)
                return;

            QuestService.DataChanged -= Refresh;
            subscribed = false;
        }

        private void SelectCategory(QuestCategory category)
        {
            bool categoryChanged = activeCategory != category;
            activeCategory = category;
            Refresh(categoryChanged);
        }

        private void Refresh()
        {
            Refresh(animateQuestList: false);
        }

        private void Refresh(bool animateQuestList)
        {
            if (!initialized)
                return;

            RefreshHeader();
            RefreshQuestList(animateQuestList);
            RefreshMilestoneProgress();
        }

        private void RefreshHeader()
        {
            if (titleText != null)
            {
                switch (activeCategory)
                {
                    case QuestCategory.Weekly:
                        titleText.text = "Weekly Tasks";
                        break;
                    case QuestCategory.Event:
                        titleText.text = "Events";
                        break;
                    default:
                        titleText.text = "Daily Tasks";
                        break;
                }
            }

            if (tabStripImage != null)
            {
                switch (activeCategory)
                {
                    case QuestCategory.Weekly:
                        tabStripImage.sprite = weeklyTabSprite;
                        break;
                    case QuestCategory.Event:
                        tabStripImage.sprite = eventTabSprite;
                        break;
                    default:
                        tabStripImage.sprite = dailyTabSprite;
                        break;
                }
            }

            RefreshTimer();
        }

        private void RefreshTimer()
        {
            if (timerText == null)
                return;

            if (activeCategory == QuestCategory.Event)
            {
                timerText.text = "COMING SOON";
                return;
            }

            System.TimeSpan remaining = QuestService.GetTimeRemaining(activeCategory);
            if (remaining.TotalDays >= 1d)
                timerText.text = string.Format("{0}d {1:D2}h", (int)remaining.TotalDays, remaining.Hours);
            else
                timerText.text = string.Format("{0:D2}h {1:D2}m", remaining.Hours, remaining.Minutes);
        }

        private void RefreshQuestList(bool animateQuestList)
        {
            ClearQuestItems();

            if (!QuestService.IsInitialized || questContent == null || questElementPrefab == null)
            {
                SetEmptyState(true);
                PlayQuestListTransition(animateQuestList);
                return;
            }

            QuestService.GetDefinitions(activeCategory, definitions);
            SetEmptyState(definitions.Count == 0);

            for (int i = 0; i < definitions.Count; i++)
            {
                QuestElementView item = Instantiate(questElementPrefab, questContent);
                item.gameObject.SetActive(true);
                item.Bind(definitions[i], OnQuestGoRequested);
                itemViews.Add(item);
            }

            PlayQuestListTransition(animateQuestList);
        }

        private void RefreshMilestoneProgress()
        {
            bool showMilestones = activeCategory != QuestCategory.Event;
            if (milestonePanel != null)
                milestonePanel.SetActive(showMilestones);

            if (!showMilestones)
            {
                milestones.Clear();
                return;
            }

            if (!QuestService.IsInitialized)
            {
                milestones.Clear();
                SetMilestoneProgress(0, 0);
                RefreshMilestoneChests();
                return;
            }

            QuestService.GetMilestones(activeCategory, milestones);

            int currentPoints = QuestService.GetMilestonePoints(activeCategory);
            int targetPoints = milestones.Count > 0 ? milestones[milestones.Count - 1].RequiredPoints : 0;

            SetMilestoneProgress(currentPoints, targetPoints);
            RefreshMilestoneChests();
        }

        private void SetMilestoneProgress(int current, int target)
        {
            if (milestoneText != null)
                milestoneText.text = target > 0 ? string.Format("{0}/{1}", current, target) : "0/0";

            if (milestoneSlider != null)
            {
                milestoneSlider.minValue = 0f;
                milestoneSlider.maxValue = Mathf.Max(1, target);
                milestoneSlider.SetValueWithoutNotify(Mathf.Clamp(current, 0, target));
            }

            if (milestoneFill != null)
                milestoneFill.fillAmount = target > 0 ? Mathf.Clamp01((float)current / target) : 0f;
        }

        private void InitializeMilestoneButtons()
        {
            if (milestoneChestButtons == null)
                return;

            milestoneChestCallbacks = new UnityAction[milestoneChestButtons.Length];
            for (int i = 0; i < milestoneChestButtons.Length; i++)
            {
                Button chestButton = milestoneChestButtons[i];
                if (chestButton == null)
                    continue;

                int chestIndex = i;
                UnityAction callback = () => ClaimMilestone(chestIndex);
                milestoneChestCallbacks[i] = callback;
                chestButton.onClick.AddListener(callback);
            }
        }

        private void RemoveMilestoneButtonListeners()
        {
            if (milestoneChestButtons == null || milestoneChestCallbacks == null)
                return;

            int count = Mathf.Min(milestoneChestButtons.Length, milestoneChestCallbacks.Length);
            for (int i = 0; i < count; i++)
            {
                Button chestButton = milestoneChestButtons[i];
                if (chestButton != null && milestoneChestCallbacks[i] != null)
                    chestButton.onClick.RemoveListener(milestoneChestCallbacks[i]);
            }

            milestoneChestCallbacks = null;
        }

        private void RefreshMilestoneChests()
        {
            if (milestoneChestButtons == null)
                return;

            for (int i = 0; i < milestoneChestButtons.Length; i++)
            {
                Button chestButton = milestoneChestButtons[i];
                if (chestButton == null)
                    continue;

                bool hasMilestone = i < milestones.Count;
                chestButton.gameObject.SetActive(hasMilestone);
                if (!hasMilestone)
                    continue;

                QuestMilestoneState state = QuestService.GetMilestoneState(milestones[i]);
                chestButton.interactable = state.IsUnlocked && !state.Claimed;

                Image chestImage = milestoneChestImages != null && i < milestoneChestImages.Length
                    ? milestoneChestImages[i]
                    : chestButton.targetGraphic as Image;
                if (chestImage != null)
                    chestImage.color = state.Claimed ? ClaimedChestColor : state.IsUnlocked ? ClaimableChestColor : LockedChestColor;
            }
        }

        private void ClaimMilestone(int chestIndex)
        {
            if (chestIndex < 0 || chestIndex >= milestones.Count)
                return;

            QuestMilestoneDefinition milestone = milestones[chestIndex];
            QuestMilestoneState state = QuestService.GetMilestoneState(milestone);
            QuestReward reward = milestone.Reward;
            if (!state.IsUnlocked || state.Claimed || reward == null || !reward.IsConfigured)
                return;

            if (rewardClaimPopup == null)
                rewardClaimPopup = FindFirstObjectByType<QuestRewardClaimPopup>(FindObjectsInactive.Include);
            if (rewardClaimPopup == null && rewardClaimPopupPrefab != null)
                rewardClaimPopup = Instantiate(rewardClaimPopupPrefab);

            if (rewardClaimPopup == null || rewardClaimPopup.IsShowing)
                return;

            rewardClaimPopup.Show(reward, () => QuestService.TryClaimMilestone(milestone));
        }

        private void SetEmptyState(bool visible)
        {
            if (emptyState != null)
                emptyState.SetActive(visible);
        }

        private void ClearQuestItems()
        {
            tabTransitionAnimation?.Stop();

            for (int i = 0; i < itemViews.Count; i++)
            {
                if (itemViews[i] != null)
                {
                    itemViews[i].gameObject.SetActive(false);
                    Destroy(itemViews[i].gameObject);
                }
            }

            itemViews.Clear();
        }

        private void PlayQuestListTransition(bool animateQuestList)
        {
            if (!animateQuestList || tabTransitionAnimation == null)
                return;

            tabTransitionAnimation.Play(GetActiveTabButton(), itemViews);
        }

        private Button GetActiveTabButton()
        {
            switch (activeCategory)
            {
                case QuestCategory.Weekly:
                    return weeklyButton;
                case QuestCategory.Event:
                    return eventButton;
                default:
                    return dailyButton;
            }
        }

        private void OnQuestGoRequested(QuestDefinition definition)
        {
            if (definition == null)
                return;

            LeaderboardBottomNavigationController navigation = FindFirstObjectByType<LeaderboardBottomNavigationController>(FindObjectsInactive.Include);
            if (navigation == null)
                return;

            switch (definition.GoTarget)
            {
                case QuestGoTarget.Store:
                    navigation.SelectStore();
                    break;

                case QuestGoTarget.Home:
                case QuestGoTarget.PowerUp:
                    navigation.SelectHome();
                    break;
            }
        }

        private void GoHome()
        {
            LeaderboardBottomNavigationController navigation = FindFirstObjectByType<LeaderboardBottomNavigationController>(FindObjectsInactive.Include);
            navigation?.SelectHome();
        }

    }
}
