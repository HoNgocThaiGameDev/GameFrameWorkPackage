using System;
using System.Collections.Generic;
using UnityEngine;

namespace NebulaSoft
{
    [Serializable]
    [RegisterReward(typeof(NoAdsRewardView))]
    public sealed class NoAdsReward : Reward
    {
        private const int PREVIEW_SORTING_ORDER = 15;

        [SerializeField] bool disableAdsForever = true;
        public bool DisableAdsForever => disableAdsForever;

        [HideIf("disableAdsForever")]
        [SerializeField] int disableSeconds = 0;
        public int DisableSeconds => disableSeconds;

        [Space]
        [SerializeField] bool disableOfferIfNoAdsPurchased;
        public bool DisableOfferIfNoAdsPurchased => disableOfferIfNoAdsPurchased;

        public NoAdsReward() { }
        public NoAdsReward(bool disableAdsForever, int disableSeconds = 0, bool disableOfferIfNoAdsPurchased = false)
        {
            this.disableAdsForever = disableAdsForever;
            this.disableSeconds = disableSeconds;
            this.disableOfferIfNoAdsPurchased = disableOfferIfNoAdsPurchased;
        }

        public override void ApplyReward()
        {
            if (disableAdsForever)
            {
                AdsManager.DisableForcedAdForever();
            }
            else
            {
                AdsManager.DisableForcedAd(disableSeconds);
            }
        }

        public override void RestoreReward()
        {
            if (disableAdsForever)
            {
                AdsManager.DisableForcedAdForever();
            }
        }

        public override List<IRewardPreview> GetRewardPreviews()
        {
            string text = "";
            if (disableAdsForever)
            {
                text = "Ads Disabled";
            }
            else
            {
                int durationInMinutes = disableSeconds / 60;

                string durationFormat = "{mm}mins";
                if (durationInMinutes > 60)
                    durationFormat = "{hh}hrs"; 

                text = TimeUtils.GetFormatedTime(durationInMinutes, durationFormat);
            }

            return new List<IRewardPreview>()
            {
                new RewardPreview(AdsManager.Settings.NoAdsRewardSprite, text, PREVIEW_SORTING_ORDER)
            };
        }

        public override bool CheckDisableState()
        {
            if (disableOfferIfNoAdsPurchased)
                return !AdsManager.IsForcedAdEnabled();

            return false;
        }
    }
}
