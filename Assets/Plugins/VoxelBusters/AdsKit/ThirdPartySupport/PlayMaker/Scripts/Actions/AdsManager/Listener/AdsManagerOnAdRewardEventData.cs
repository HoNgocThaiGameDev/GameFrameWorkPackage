#if PLAYMAKER
using UnityEngine;
using HutongGames.PlayMaker;
using TooltipAttribute = HutongGames.PlayMaker.TooltipAttribute;

namespace VoxelBusters.AdsKit.PlayMaker
{
    [ActionCategory("VoxelBusters - AdsKit - Ads Manager (Listener)")]
    [Tooltip("Reads ad reward event data from the current PlayMaker event.")]
    public class AdsManagerOnAdRewardEventData : FsmStateAction
    {
        [ActionSection("Results")]
        [UIHint(UIHint.Variable)]
        public FsmString placement;

        [UIHint(UIHint.Variable)]
        public FsmEnum adType;

        [UIHint(UIHint.Variable)]
        public FsmString networkId;

        [UIHint(UIHint.Variable)]
        public FsmFloat amount;

        [UIHint(UIHint.Variable)]
        public FsmString rewardType;

        [UIHint(UIHint.Variable)]
        public FsmBool isAmountAvailable;

        [ActionSection("Options")]
        [Tooltip("Logs the event details received by this action.")]
        public bool logEventDetails;

        public override void Reset()
        {
            placement = new FsmString { UseVariable = true };
            adType = new FsmEnum { UseVariable = true, EnumType = typeof(AdType), Value = AdType.RewardedVideo };
            networkId = new FsmString { UseVariable = true };
            amount = new FsmFloat { UseVariable = true };
            rewardType = new FsmString { UseVariable = true };
            isAmountAvailable = new FsmBool { UseVariable = true };
            logEventDetails = false;
        }

        public override void OnEnter()
        {
            string resolvedPlacement = string.Empty;
            AdReward reward = null;

            var wrapper = Fsm.EventData.ObjectData as AdsManagerOnAdRewardEventDataWrapper;
            if (wrapper != null)
            {
                resolvedPlacement = wrapper.placement ?? string.Empty;
                reward = wrapper.reward;
            }

            if (reward == null)
            {
                ApplyDefaults(resolvedPlacement);
                AdsManagerEventDataLogUtility.LogIfEnabled(logEventDetails, nameof(AdsManagerOnAdRewardEventData), wrapper);
                Finish();
                return;
            }

            if (!placement.IsNone)
            {
                placement.Value = resolvedPlacement;
            }

            if (!adType.IsNone)
            {
                adType.Value = reward.AdType;
            }

            if (!networkId.IsNone)
            {
                networkId.Value = reward.NetworkId ?? string.Empty;
            }

            if (!amount.IsNone)
            {
                amount.Value = (float)reward.Amount;
            }

            if (!rewardType.IsNone)
            {
                rewardType.Value = reward.Type ?? string.Empty;
            }

            if (!isAmountAvailable.IsNone)
            {
                isAmountAvailable.Value = reward.IsRewardAmountAvailable();
            }

            AdsManagerEventDataLogUtility.LogIfEnabled(logEventDetails, nameof(AdsManagerOnAdRewardEventData), wrapper);
            Finish();
        }

        private void ApplyDefaults(string resolvedPlacement)
        {
            if (!placement.IsNone)
            {
                placement.Value = resolvedPlacement;
            }

            if (!adType.IsNone)
            {
                adType.Value = AdType.RewardedVideo;
            }

            if (!networkId.IsNone)
            {
                networkId.Value = string.Empty;
            }

            if (!amount.IsNone)
            {
                amount.Value = 0f;
            }

            if (!rewardType.IsNone)
            {
                rewardType.Value = string.Empty;
            }

            if (!isAmountAvailable.IsNone)
            {
                isAmountAvailable.Value = false;
            }
        }

    }
}
#endif
