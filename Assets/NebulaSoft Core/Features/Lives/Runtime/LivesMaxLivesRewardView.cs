using TMPro;
using UnityEngine;

namespace NebulaSoft
{
    [System.Serializable]
    public sealed class LivesMaxLivesRewardView : RewardView
    {
        [SerializeField] TextMeshProUGUI maxLivesText;
        [SerializeField] string textFormat = "{0}";

        public LivesMaxLivesRewardView() { }
        public LivesMaxLivesRewardView(TextMeshProUGUI maxLivesText)
        {
            this.maxLivesText = maxLivesText;
        }

        protected override void OnInitialized()
        {
            LivesMaxLivesReward livesReward = (LivesMaxLivesReward)reward;
            if (livesReward != null)
            {
                maxLivesText.text = string.Format(textFormat, livesReward.MaxLivesAmount);
            }
        }

        public override void OnPurchased()
        {

        }
    }
}
