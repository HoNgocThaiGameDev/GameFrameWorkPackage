using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NebulaSoft
{
    public sealed class DailyRewardClaimPopup : MonoBehaviour
    {
        private const float RewardIconSize = 240f;
        private const float RewardAmountFontSize = 52f;
        private const float BounceDuration = 0.32f;
        private const int SortingOrder = 1200;

        [SerializeField] GameObject rewardPreviewPrefab;

        private readonly List<GameObject> spawnedRewardPreviews = new List<GameObject>();
        private Button tapToClaimButton;
        private GameObject safeBox;
        private GameObject door;
        private GameObject rewardsContainer;
        private GameObject rewardShine;
        private Func<bool> claimAction;

        public bool IsShowing => gameObject.activeSelf;

        private void Awake()
        {
            Canvas popupCanvas = GetComponent<Canvas>();
            if (popupCanvas != null)
            {
                popupCanvas.overrideSorting = true;
                popupCanvas.sortingOrder = SortingOrder;
            }

            tapToClaimButton = FindChild("Tap To Claim Area")?.GetComponent<Button>();
            safeBox = FindChild("Safe Box")?.gameObject;
            door = FindChild("Door")?.gameObject;
            rewardsContainer = FindChild("Rewards")?.gameObject;
            rewardShine = FindChild("Shine")?.gameObject;

            if (tapToClaimButton != null)
            {
                tapToClaimButton.onClick.AddListener(ClaimReward);
                UIAudioFeedback.RegisterButton(tapToClaimButton);
            }
        }

        private void OnDisable()
        {
            StopAllCoroutines();
        }

        private void OnDestroy()
        {
            if (tapToClaimButton != null)
                tapToClaimButton.onClick.RemoveListener(ClaimReward);
        }

        public void Show(IReadOnlyList<DailyRewardGrant> rewards, Func<bool> onClaim)
        {
            if (rewards == null || rewards.Count == 0)
                return;

            claimAction = onClaim;
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            if (safeBox != null)
                safeBox.SetActive(false);
            if (door != null)
                door.SetActive(false);
            if (rewardsContainer != null)
                rewardsContainer.SetActive(true);
            if (rewardShine != null)
                rewardShine.SetActive(true);

            AudioClips audioClips = AudioController.AudioClips;
            if (audioClips != null && audioClips.brightest_star != null)
                AudioController.PlaySound(audioClips.brightest_star);

            StopAllCoroutines();
            ClearRewardPreviews();

            if (rewardsContainer == null || rewardPreviewPrefab == null)
                return;

            for (int i = 0; i < rewards.Count; i++)
            {
                DailyRewardGrant reward = rewards[i];
                if (reward != null && reward.Amount > 0)
                    CreateRewardPreview(reward);
            }
        }

        private void CreateRewardPreview(DailyRewardGrant reward)
        {
            GameObject spawnedRewardPreview = Instantiate(rewardPreviewPrefab, rewardsContainer.transform);
            spawnedRewardPreviews.Add(spawnedRewardPreview);

            UIRewardPreviewBehavior rewardPreview = spawnedRewardPreview.GetComponent<UIRewardPreviewBehavior>();
            if (rewardPreview == null)
                return;

            rewardPreview.Init(new RewardPreview(reward.GetIcon(), "x" + reward.Amount), null);

            RectTransform iconTransform = rewardPreview.Image != null ? rewardPreview.Image.rectTransform : null;
            if (iconTransform != null)
            {
                iconTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, RewardIconSize);
                iconTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, RewardIconSize);
                StartCoroutine(PlayIconBounce(iconTransform));
            }

            if (rewardPreview.Text != null)
            {
                rewardPreview.Text.fontSize = RewardAmountFontSize;
                rewardPreview.Text.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, RewardIconSize);
                rewardPreview.Text.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
                    RewardAmountFontSize * 1.5f);
            }
        }

        private void ClaimReward()
        {
            if (claimAction == null || !claimAction())
                return;

            claimAction = null;
            gameObject.SetActive(false);
        }

        private void ClearRewardPreviews()
        {
            for (int i = 0; i < spawnedRewardPreviews.Count; i++)
            {
                if (spawnedRewardPreviews[i] != null)
                    Destroy(spawnedRewardPreviews[i]);
            }

            spawnedRewardPreviews.Clear();
        }

        private IEnumerator PlayIconBounce(RectTransform iconTransform)
        {
            Vector3 finalScale = iconTransform.localScale;
            float elapsed = 0f;

            while (elapsed < BounceDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / BounceDuration);
                float scale = progress < 0.7f
                    ? Mathf.Lerp(0.65f, 1.16f, progress / 0.7f)
                    : Mathf.Lerp(1.16f, 1f, (progress - 0.7f) / 0.3f);
                iconTransform.localScale = finalScale * scale;
                yield return null;
            }

            iconTransform.localScale = finalScale;
        }

        private Transform FindChild(string childName)
        {
            Transform[] transforms = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                if (transforms[i].name == childName)
                    return transforms[i];
            }

            return null;
        }
    }
}
