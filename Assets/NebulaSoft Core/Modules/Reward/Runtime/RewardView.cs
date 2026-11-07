using System;

namespace NebulaSoft
{
    [Serializable]
    public abstract class RewardView
    {
        protected Reward reward;
        public Reward Reward => reward;

        protected IRewardHolder holder;

        public void Init(Reward reward, IRewardHolder holder)
        {
            this.reward = reward;
            this.holder = holder;

            OnInitialized();
        }

        protected abstract void OnInitialized();

        public virtual void OnPurchased() { }
        public virtual void OnDestroy() { }

        public virtual void Fill(Reward reward) { }

        public void MarkAsDirty()
        {
            holder?.MarkAsDirty();
        }
    }
}
