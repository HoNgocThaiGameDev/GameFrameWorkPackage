using System.Collections.Generic;
using UnityEngine;

namespace NebulaSoft
{
    [System.Serializable]
    public sealed class SimpleReward
    {
        [SerializeReference] Reward reward;
        public Reward Reward => reward;

        public void ApplyReward()
        {
            reward?.ApplyReward();
        }

        public IRewardPreview GetPreview()
        {
            if (reward == null) return null;

            List<IRewardPreview> previews = reward.GetRewardPreviews();
            if(previews.IsNullOrEmpty()) return null;

            return previews[0];
        }
    }
}
