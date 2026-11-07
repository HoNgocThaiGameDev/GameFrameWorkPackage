using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace NebulaSoft
{
    public abstract class RewardsHolder : MonoBehaviour, IUIPageElement, IRewardHolder
    {
        [Group("System")]
        [SerializeField, CreateScriptableObject] 
        protected RewardsSet rewardSet;

        [SerializeReference] List<RewardView> rewardsView = new List<RewardView>();

        [Group("Events")]
        [SerializeField] UnityEvent rewardReceived;
        public UnityEvent RewardReceived => rewardReceived;

        [Group("Settings")]
        [SerializeField, Space]
        private bool showPopup = true;

        protected bool isPageActive;
        protected bool isInitialized;

        protected bool isDirty;
        public bool IsDirty => isDirty;

        protected void InitializeComponents()
        {
            if (isInitialized) return;

            if(rewardSet == null)
            {
                Debug.LogError("Rewards Set isn't linked!");

                return;
            }

            isInitialized = true;

            List<Reward> rewards = rewardSet.Rewards;
            if(!rewards.IsNullOrEmpty())
            {
                foreach (Reward reward in rewards)
                {
                    if (reward == null) continue;

                    Type viewType = RewardsUtils.GetViewTypeFor(reward.GetType());
                    if (viewType == null) continue;

                    RewardView view = GetView(viewType);
                    if (view == null) continue;

                    view.Init(reward, this);
                }
            }

            if (CheckDisableState())
                gameObject.SetActive(false);

            rewardSet.RewardRecieved += OnRewardRecieved;
        }

        public void Init(UIPage page)
        {
            isPageActive = page.IsPageDisplayed;
        }

        private void Update()
        {
            if (!isInitialized) return;
            if (!isPageActive) return;
            if (!isDirty) return;

            List<Reward> rewards = rewardSet.Rewards;
            if (!rewards.IsNullOrEmpty())
            {
                foreach (Reward reward in rewards)
                {
                    if (reward == null) continue;
                    if (reward.CheckDisableState())
                    {
                        Debug.Log(reward.ToString());

                        gameObject.SetActive(false);

                        break;
                    }
                }

                isDirty = false;
            }
        }

        private void OnDestroy()
        {
            if (!rewardsView.IsNullOrEmpty())
            {
                for (int i = 0; i < rewardsView.Count; i++)
                {
                    rewardsView[i]?.OnDestroy();
                }
            }

            if(rewardSet != null)
            {
                rewardSet.RewardRecieved -= OnRewardRecieved;
            }
        }

        public bool CheckDisableState()
        {
            if (!isInitialized) return false;

            List<Reward> rewards = rewardSet.Rewards;
            if (!rewards.IsNullOrEmpty())
            {
                foreach (Reward reward in rewards)
                {
                    if (reward == null) continue;
                    if (reward.CheckDisableState())
                        return true;
                }
            }

            return false;
        }

        protected virtual bool ShouldPresentReward => true;

        private void OnRewardRecieved()
        {
            if (!isInitialized || !ShouldPresentReward) return;

            void InvokeRewardCallback()
            {
                rewardReceived?.Invoke();

                if (!rewardsView.IsNullOrEmpty())
                {
                    for (int i = 0; i < rewardsView.Count; i++)
                    {
                        rewardsView[i].OnPurchased();
                    }
                }
            }

            if (showPopup)
            {
                if(!UIRewardsConfirmation.Display(rewardSet.GetPreviews(), () => InvokeRewardCallback()))
                {
                    InvokeRewardCallback();
                }
            }
            else
            {
                InvokeRewardCallback();
            }
        }

        public void OnPageStateChanged(bool state)
        {
            isPageActive = state;

            if (!isInitialized) return;

            if (state)
            {
                List<Reward> rewards = rewardSet.Rewards;
                if (!rewards.IsNullOrEmpty())
                {
                    foreach (Reward reward in rewards)
                    {
                        if (reward == null) continue;
                        if (reward.CheckDisableState())
                        {
                            gameObject.SetActive(false);

                            break;
                        }
                    }

                    isDirty = false;
                }
            }
        }

        public void MarkAsDirty()
        {
            isDirty = true;
        }

        public RewardView GetView(Type type)
        {
            foreach(RewardView view in rewardsView)
            {
                if(view != null && view.GetType() == type) return view;
            }

            return null;
        }
    }
}
