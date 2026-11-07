using System;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace NebulaSoft
{
    public sealed class DailyRewardElementView : MonoBehaviour
    {
        [SerializeField] Image defaultFrame;
        [SerializeField] Image activeFrame;
        [SerializeField] TextMeshProUGUI dayLabel;
        [SerializeField] Sprite rewardIconSprite;
        [SerializeField, HideInInspector, FormerlySerializedAs("rewardIcon")] Image rewardIconImage;
        [SerializeField] TextMeshProUGUI rewardAmount;
        [SerializeField] DailyRewardClaimButtonView claimButtonView;

        public void Bind(int dayNumber, DailyRewardDayDefinition definition, DailyRewardCellState state, Action onClaim)
        {
            if (dayLabel != null)
                dayLabel.text = "Day " + dayNumber;

            bool isHighlighted = state == DailyRewardCellState.Current || state == DailyRewardCellState.Claimed;
            if (defaultFrame != null)
                defaultFrame.gameObject.SetActive(!isHighlighted);
            if (activeFrame != null)
                activeFrame.gameObject.SetActive(isHighlighted);

            DailyRewardGrant reward = definition != null && definition.Rewards != null && definition.Rewards.Count > 0
                ? definition.Rewards[0]
                : null;
            ApplyReward(reward);

            if (claimButtonView == null)
                claimButtonView = GetComponentInChildren<DailyRewardClaimButtonView>(true);
            if (claimButtonView != null)
                claimButtonView.Bind(state, onClaim);
        }

        private void ApplyReward(DailyRewardGrant reward)
        {
            if (rewardAmount != null)
                rewardAmount.text = reward == null ? string.Empty : reward.GetAmountLabel();

            if (reward == null)
            {
                ApplyIcon(rewardIconSprite);
                return;
            }

            ApplyIcon(rewardIconSprite != null ? rewardIconSprite : reward.GetIcon());
        }

        private void OnValidate()
        {
            if (rewardIconSprite != null)
                ApplyIcon(rewardIconSprite);
        }

        private void ApplyIcon(Sprite icon)
        {
            if (rewardIconImage == null)
                return;

            rewardIconImage.sprite = icon;
            rewardIconImage.gameObject.SetActive(icon != null);
        }
    }
}
