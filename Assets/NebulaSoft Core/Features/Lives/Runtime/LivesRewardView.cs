using TMPro;
using UnityEngine;

namespace NebulaSoft
{
    [System.Serializable]
    public sealed class LivesRewardView : RewardView
    {
        [SerializeField] TextMeshProUGUI amountText;

        public LivesRewardView() { }
        public LivesRewardView(TextMeshProUGUI amountText)
        {
            this.amountText = amountText;
        }

        protected override void OnInitialized()
        {
            LivesReward livesReward = (LivesReward)reward;
            if (livesReward != null)
            {
                amountText.text = $"x{livesReward.LivesAmount}";
            }
        }

        public override void OnPurchased()
        {

        }
    }
}
