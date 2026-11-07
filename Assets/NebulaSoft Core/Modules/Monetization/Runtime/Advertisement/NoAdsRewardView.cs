using TMPro;
using UnityEngine;

namespace NebulaSoft
{
    [System.Serializable]
    public sealed class NoAdsRewardView : RewardView
    {
        [SerializeField] TextMeshProUGUI durationText;

        public NoAdsRewardView() { }
        public NoAdsRewardView(TextMeshProUGUI durationText)
        {
            this.durationText = durationText;
        }

        protected override void OnInitialized()
        {
            NoAdsReward noAdsReward = (NoAdsReward)reward;
            if (noAdsReward != null)
            {
                if(durationText != null)
                {
                    if (!noAdsReward.DisableAdsForever)
                    {
                        int durationInMinutes = noAdsReward.DisableSeconds / 60;

                        string durationFormat = "{mm}mins";
                        if (durationInMinutes > 60)
                            durationFormat = "{hh}hrs";

                        durationText.text = TimeUtils.GetFormatedTime(durationInMinutes, durationFormat);
                    }
                    else
                    {
                        durationText.text = "Disable Ads";
                    }
                }
            }

            AdsManager.ForcedAdDisabled += OnForcedAdDisabled;
        }

        public override void OnDestroy()
        {
            AdsManager.ForcedAdDisabled -= OnForcedAdDisabled;
        }

        private void OnForcedAdDisabled()
        {
            MarkAsDirty();
        }

        public override void OnPurchased()
        {

        }
    }
}
