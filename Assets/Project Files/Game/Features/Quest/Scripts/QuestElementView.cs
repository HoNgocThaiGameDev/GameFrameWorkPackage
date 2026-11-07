using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NebulaSoft
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class QuestElementView : MonoBehaviour
    {
        private static bool rewardedSkipInProgress;

        [Header("Required quest fields")]
        [SerializeField] TextMeshProUGUI informationText;
        [SerializeField] Button claimButton;
        [SerializeField] Slider progressSlider;

        [Header("Actions")]
        [SerializeField] Button skipButton;
        [SerializeField] Button goButton;
        [SerializeField] GameObject claimButtonRoot;
        [SerializeField] GameObject skipButtonRoot;
        [SerializeField] GameObject goButtonRoot;

        [Header("Visuals")]
        [SerializeField] TextMeshProUGUI progressText;
        [SerializeField] TextMeshProUGUI rewardAmountText;
        [SerializeField] Image rewardIcon;
        [SerializeField] TextMeshProUGUI claimLabel;
        [SerializeField] Sprite claimButtonSprite;
        [SerializeField] Sprite claimedButtonSprite;
        [SerializeField] QuestRewardClaimPopup rewardClaimPopupPrefab;

        private QuestDefinition definition;
        private Action<QuestDefinition> goRequested;
        private bool skipAdInProgress;
        private static QuestRewardClaimPopup rewardClaimPopup;

        public QuestDefinition Definition => definition;

        private void Awake()
        {
            if (claimButton != null)
                claimButton.onClick.AddListener(OnClaimButtonClicked);
            if (skipButton != null)
                skipButton.onClick.AddListener(OnSkipButtonClicked);
            if (goButton != null)
                goButton.onClick.AddListener(OnGoButtonClicked);

            UIAudioFeedback.RegisterButtons(transform);
        }

        private void OnDestroy()
        {
            if (claimButton != null)
                claimButton.onClick.RemoveListener(OnClaimButtonClicked);
            if (skipButton != null)
                skipButton.onClick.RemoveListener(OnSkipButtonClicked);
            if (goButton != null)
                goButton.onClick.RemoveListener(OnGoButtonClicked);
        }

        public void Bind(QuestDefinition questDefinition, Action<QuestDefinition> onGoRequested)
        {
            definition = questDefinition;
            goRequested = onGoRequested;
            skipAdInProgress = false;
            Redraw();
        }

        public void Redraw()
        {
            if (definition == null)
                return;

            QuestProgressState progress = QuestService.GetProgress(definition);
            int current = Mathf.Min(progress.Current, progress.Target);

            if (informationText != null)
                informationText.text = definition.Title;

            if (progressText != null)
                progressText.text = string.Format("{0}/{1}", current, progress.Target);

            if (progressSlider != null)
            {
                progressSlider.minValue = 0f;
                progressSlider.maxValue = 1f;
                progressSlider.SetValueWithoutNotify(progress.Normalized);
            }

            QuestReward reward = QuestService.GetReward(definition);
            if (rewardAmountText != null)
                rewardAmountText.text = reward != null ? reward.Amount.ToString() : "0";

            if (rewardIcon != null)
            {
                Sprite icon = reward != null ? reward.GetIcon() : null;
                rewardIcon.sprite = icon;
                rewardIcon.enabled = icon != null;
            }

            bool canClaim = progress.IsComplete && !progress.Claimed;
            bool isClaimed = progress.Claimed;
            bool canSkipOrGo = !progress.IsComplete && !isClaimed;
            bool hasGoTarget = definition.GoTarget != QuestGoTarget.None;

            if (claimButtonRoot != null)
                claimButtonRoot.SetActive(canClaim || isClaimed);
            if (skipButtonRoot != null)
                skipButtonRoot.SetActive(canSkipOrGo);
            if (goButtonRoot != null)
                goButtonRoot.SetActive(canSkipOrGo && hasGoTarget);

            if (claimButton != null)
            {
                claimButton.interactable = canClaim;
                Image claimButtonImage = claimButton.targetGraphic as Image;
                if (claimButtonImage != null)
                {
                    Sprite buttonSprite = isClaimed ? claimedButtonSprite : claimButtonSprite;
                    if (buttonSprite != null)
                        claimButtonImage.sprite = buttonSprite;

                    claimButtonImage.color = Color.white;
                }
            }
            if (skipButton != null)
                skipButton.interactable = canSkipOrGo && !skipAdInProgress;
            if (goButton != null)
                goButton.interactable = canSkipOrGo && hasGoTarget;

            if (claimLabel != null)
                claimLabel.text = isClaimed ? "CLAIMED" : "CLAIM";
        }

        private void OnClaimButtonClicked()
        {
            if (definition == null || rewardClaimPopup != null && rewardClaimPopup.IsShowing)
                return;

            QuestProgressState progress = QuestService.GetProgress(definition);
            if (!progress.IsComplete || progress.Claimed || rewardClaimPopupPrefab == null)
                return;

            QuestReward reward = QuestService.GetReward(definition);
            if (reward == null || !reward.IsConfigured)
                return;

            if (rewardClaimPopup == null)
                rewardClaimPopup = Instantiate(rewardClaimPopupPrefab);

            rewardClaimPopup.Show(reward, () => QuestService.TryClaim(definition));
        }

        private void OnSkipButtonClicked()
        {
            if (definition == null || skipAdInProgress || rewardedSkipInProgress)
                return;

            QuestProgressState progress = QuestService.GetProgress(definition);
            if (progress.IsComplete || progress.Claimed)
                return;

            if (LeaderboardBottomNavigationController.IsNoConnectionActive())
            {
                LeaderboardBottomNavigationController.TryShowNoConnectionPopup();
                return;
            }

#if MODULE_MONETIZATION
            skipAdInProgress = true;
            rewardedSkipInProgress = true;
            Redraw();

            QuestDefinition questToComplete = definition;
            if (questToComplete.GoalType == QuestGoalType.WatchRewardedAds)
            {
                AdsManager.ShowRewardBasedVideo(OnWatchRewardedVideoClosed, "QuestWatch_" + questToComplete.Id);
                return;
            }

            AdsManager.ShowRewardBasedVideo(rewarded => OnSkipRewardedVideoClosed(questToComplete, rewarded),
                "QuestSkip_" + questToComplete.Id);
#else
            Debug.LogWarning("[Quest] Rewarded ads are not available, so this quest cannot be skipped.");
#endif
        }

#if MODULE_MONETIZATION
        private void OnSkipRewardedVideoClosed(QuestDefinition questToComplete, bool rewarded)
        {
            skipAdInProgress = false;
            rewardedSkipInProgress = false;
            if (rewarded)
                QuestService.TryCompleteWithRewardedAd(questToComplete);

            if (this != null)
                Redraw();
        }

        private void OnWatchRewardedVideoClosed(bool rewarded)
        {
            skipAdInProgress = false;
            rewardedSkipInProgress = false;

            // QuestService receives the global rewarded-video event and increments this
            // quest once on a successful ad view.
            if (this != null)
                Redraw();
        }
#endif

        private void OnGoButtonClicked()
        {
            if (definition == null || definition.GoTarget == QuestGoTarget.None)
                return;

            goRequested?.Invoke(definition);
        }
    }
}
