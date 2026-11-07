using System;
using System.Globalization;
using UnityEngine;

namespace NebulaSoft
{
    [Serializable]
    public sealed class QuestMilestoneDefinition
    {
        [SerializeField] string id;
        [SerializeField] QuestCategory category = QuestCategory.Daily;
        [SerializeField] int requiredPoints = 1;
        [SerializeField] int sortOrder;
        // Kept for existing Quest Database assets. New data is written to rewardData.
        [SerializeField, HideInInspector] CurrencyAmount reward = new CurrencyAmount(CurrencyType.Coins, 0);
        [SerializeField] QuestReward rewardData = new QuestReward();

        public string Id => id != null ? id.Trim() : string.Empty;
        public QuestCategory Category => category;
        public int RequiredPoints => Mathf.Max(1, requiredPoints);
        public int SortOrder => sortOrder;
        public QuestReward Reward => rewardData != null && rewardData.IsConfigured
            ? rewardData
            : QuestReward.FromLegacy(reward);
        public bool IsAvailable => !string.IsNullOrWhiteSpace(Id) && requiredPoints > 0;

        public string GetPeriodKey(DateTime utcNow)
        {
            switch (category)
            {
                case QuestCategory.Daily:
                    return "daily:" + utcNow.ToString("yyyyMMdd", CultureInfo.InvariantCulture);

                case QuestCategory.Weekly:
                    int daysSinceMonday = ((int)utcNow.DayOfWeek + 6) % 7;
                    DateTime weekStart = utcNow.Date.AddDays(-daysSinceMonday);
                    return "weekly:" + weekStart.ToString("yyyyMMdd", CultureInfo.InvariantCulture);

                default:
                    return string.Empty;
            }
        }
    }
}
