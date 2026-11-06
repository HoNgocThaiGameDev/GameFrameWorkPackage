#if PLAYMAKER
using HutongGames.PlayMaker;
using TooltipAttribute = HutongGames.PlayMaker.TooltipAttribute;

namespace VoxelBusters.AdsKit.PlayMaker
{
    [ActionCategory("VoxelBusters - AdsKit - Ads Manager (Load, show, hide ads)")]
    [Tooltip("Reads ad reward data from current PlayMaker event payload (Fsm.EventData).")]
    public class AdsManagerGetAdReward : FsmStateAction
    {
        [ActionSection("Results")]
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

        public override void Reset()
        {
            adType = new FsmEnum { UseVariable = true, EnumType = typeof(AdType), Value = AdType.RewardedVideo };
            networkId = new FsmString { UseVariable = true };
            amount = new FsmFloat { UseVariable = true };
            rewardType = new FsmString { UseVariable = true };
            isAmountAvailable = new FsmBool { UseVariable = true };
        }

        public override void OnEnter()
        {
            var wrapper = Fsm.EventData.ObjectData as AdsManagerOnAdRewardEventDataWrapper;
            AdReward reward = wrapper != null ? wrapper.reward : null;

            if (reward == null)
            {
                LogWarning("AdsManagerGetAdReward: No ad reward payload found in Fsm.EventData.ObjectData.");
            }

            if (!adType.IsNone && reward != null)
            {
                adType.Value = reward.AdType;
            }

            if (!networkId.IsNone)
            {
                networkId.Value = reward != null ? (reward.NetworkId ?? string.Empty) : string.Empty;
            }

            if (!amount.IsNone)
            {
                amount.Value = reward != null ? (float)reward.Amount : 0f;
            }

            if (!rewardType.IsNone)
            {
                rewardType.Value = reward != null ? (reward.Type ?? string.Empty) : string.Empty;
            }

            if (!isAmountAvailable.IsNone)
            {
                isAmountAvailable.Value = reward != null && reward.IsRewardAmountAvailable();
            }

            Finish();
        }
    }
}
#endif
