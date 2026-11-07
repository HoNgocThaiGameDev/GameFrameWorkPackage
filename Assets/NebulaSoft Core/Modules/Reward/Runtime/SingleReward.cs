using UnityEngine;

namespace NebulaSoft
{
    [System.Serializable]
    public sealed class SingleReward : IRewardHolder
    {
        [SerializeReference] Reward reward;
        public Reward Reward => reward;

        [SerializeReference] RewardView rewardView;
        public RewardView RewardView => rewardView;

        private bool isDirty;
        public bool IsDirty => isDirty;

        public void Init()
        {
            if (reward == null || rewardView == null) return;

            rewardView.Init(reward, this);
        }

        public void ApplyReward()
        {
            reward.ApplyReward();
            rewardView.OnPurchased();
        }

        public void MarkAsDirty()
        {
            isDirty = true;
        }
    }
}
