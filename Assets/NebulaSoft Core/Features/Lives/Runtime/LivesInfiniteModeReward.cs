using System;
using System.Collections.Generic;
using UnityEngine;

namespace NebulaSoft
{
    [Serializable]
    [RegisterReward(typeof(LivesInfiniteModeRewardView))]
    public sealed class LivesInfiniteModeReward : Reward
    {
        private const int PREVIEW_SORTING_ORDER = 0;

        [SerializeField] float durationInMinutes = 60;
        public float DurationInMinutes => durationInMinutes;

        public LivesInfiniteModeReward() { }
        public LivesInfiniteModeReward(float durationInMinutes)
        {
            this.durationInMinutes = durationInMinutes;
        }

        public override void ApplyReward()
        {
            LivesSystem.EnableInfiniteMode(durationInMinutes * 60);
        }

        public override List<IRewardPreview> GetRewardPreviews()
        {
            string durationFormat = "{mm} min";
            if (durationInMinutes > 60)
                durationFormat = "{hh} hr";

            return new List<IRewardPreview>()
            {
                new RewardPreview(LivesSystem.Data.RewardPreviewSprite, TimeUtils.GetFormatedTime(durationInMinutes, durationFormat), PREVIEW_SORTING_ORDER, LivesSystem.Data.RewardInfiniteModePreviewPrefab)
            };
        }
    }
}
