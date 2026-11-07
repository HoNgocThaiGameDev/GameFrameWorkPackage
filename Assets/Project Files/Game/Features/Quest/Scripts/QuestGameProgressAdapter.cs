using UnityEngine;

namespace NebulaSoft
{
    /// <summary>The sample game's mapping from framework events to quest goals.</summary>
    public sealed class QuestGameProgressAdapter : MonoBehaviour
    {
        private void Start()
        {
            if (CurrencyController.Currencies != null) CurrencyController.SubscribeGlobalCallback(OnCurrencyChanged);
#if MODULE_MONETIZATION
            AdsManager.RewardedVideoRewarded += OnRewardedVideoRewarded;
#endif
        }

        private void OnCurrencyChanged(Currency currency, int difference)
        {
            if (currency.ID == "Coins" && difference < 0) QuestService.ReportProgress(QuestGoalType.SpendCoins, -difference);
        }

        private void OnRewardedVideoRewarded() => QuestService.ReportProgress(QuestGoalType.WatchRewardedAds);

        private void OnDestroy()
        {
            CurrencyController.UnsubscribeGlobalCallback(OnCurrencyChanged);
#if MODULE_MONETIZATION
            AdsManager.RewardedVideoRewarded -= OnRewardedVideoRewarded;
#endif
        }
    }
}
