using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace NebulaSoft
{
    public sealed class QuestRewardClaimPopup : MonoBehaviour
    {
        private const float RewardIconSize = 240f;
        private const float RewardAmountFontSize = 52f;
        private const float BounceDuration = 0.32f;

        [SerializeField] GameObject rewardPreviewPrefab;

        private Button tapToClaimButton;
        private GameObject safeBox;
        private GameObject door;
        private GameObject rewardsContainer;
        private GameObject rewardShine;
        private GameObject spawnedRewardPreview;
        private Func<bool> claimAction;
        private Coroutine iconBounceCoroutine;

        public bool IsShowing => gameObject.activeSelf;

        private void Awake()
        {
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

        private void OnDestroy()
        {
            if (tapToClaimButton != null)
                tapToClaimButton.onClick.RemoveListener(ClaimReward);
        }

        public void Show(QuestReward reward, Func<bool> onClaim)
        {
            if (reward == null || !reward.IsConfigured)
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

            if (spawnedRewardPreview != null)
                Destroy(spawnedRewardPreview);

            if (rewardsContainer == null || rewardPreviewPrefab == null)
                return;

            spawnedRewardPreview = Instantiate(rewardPreviewPrefab, rewardsContainer.transform);
            UIRewardPreviewBehavior rewardPreview = spawnedRewardPreview.GetComponent<UIRewardPreviewBehavior>();
            if (rewardPreview == null)
                return;

            rewardPreview.Init(new RewardPreview(reward.GetIcon(), "x" + reward.Amount), null);

            RectTransform iconTransform = rewardPreview.Image != null ? rewardPreview.Image.rectTransform : null;
            if (iconTransform != null)
            {
                iconTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, RewardIconSize);
                iconTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, RewardIconSize);

                if (iconBounceCoroutine != null)
                    StopCoroutine(iconBounceCoroutine);

                iconBounceCoroutine = StartCoroutine(PlayIconBounce(iconTransform));
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
            iconBounceCoroutine = null;
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
