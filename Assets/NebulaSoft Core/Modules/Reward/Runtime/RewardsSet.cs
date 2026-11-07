using System.Collections.Generic;
using UnityEngine;

namespace NebulaSoft
{
    [CreateAssetMenu(fileName = "Rewards Set", menuName = "Data/Rewards/Rewards Set")]
    public class RewardsSet : ScriptableObject
    {
        [Group("System")]
        [SerializeReference] List<Reward> rewards = new List<Reward>();
        public List<Reward> Rewards => rewards;

        [Group("System")]
        [SerializeField] string notes;

        public event SimpleCallback RewardRecieved;

        public void ApplyReward()
        {
            ApplyRewardSteps(0, null);
            NotifyRewardReceived();
        }

        internal void ApplyRewardSteps(int startIndex, System.Action<int> stepCompleted)
        {
            if (rewards == null) return;
            for (int i = startIndex; i < rewards.Count; i++) rewards[i]?.Validate();
            for (int i = startIndex; i < rewards.Count; i++)
            {
                rewards[i]?.ApplyReward();
                stepCompleted?.Invoke(i + 1);
            }
        }

        internal void NotifyRewardReceived()
        {
            // UI observers must not interrupt a purchase after rewards were applied.
            if (RewardRecieved != null)
                foreach (SimpleCallback callback in RewardRecieved.GetInvocationList())
                {
                    if (callback.Target is UnityEngine.Object target && target == null) continue;
                    try { callback(); }
                    catch (System.Exception exception) { Debug.LogException(exception); }
                }
        }

        public void RestoreReward()
        {
            if (!rewards.IsNullOrEmpty())
            {
                for (int i = 0; i < rewards.Count; i++)
                    rewards[i]?.RestoreReward();
            }
        }

        public List<IRewardPreview> GetPreviews()
        {
            List<IRewardPreview> preview = new List<IRewardPreview>();
            foreach (Reward reward in rewards)
            {
                List<IRewardPreview> rewardPreview = reward.GetRewardPreviews();
                if (!rewardPreview.IsNullOrEmpty())
                {
                    preview.AddRange(rewardPreview);
                }
            }

            return preview;
        }
    }
}
