using TMPro;
using UnityEngine;

namespace NebulaSoft
{
    [System.Serializable]
    public sealed class LivesInfiniteModeRewardView : RewardView
    {
        [SerializeField] TextMeshProUGUI durationText;
        [SerializeField] string durationFormat = "{hh}hrs";

        public LivesInfiniteModeRewardView() { }
        public LivesInfiniteModeRewardView(TextMeshProUGUI durationText, string durationFormat = "{hh}hrs")
        {
            this.durationText = durationText;
            this.durationFormat = durationFormat;
        }

        protected override void OnInitialized()
        {
            LivesInfiniteModeReward livesReward = (LivesInfiniteModeReward)reward;
            if (livesReward != null)
            {
                if (durationText != null)
                {
                    durationText.text = TimeUtils.GetFormatedTime(livesReward.DurationInMinutes, durationFormat);
                }
            }
        }

        public override void OnPurchased()
        {

        }
    }
}
