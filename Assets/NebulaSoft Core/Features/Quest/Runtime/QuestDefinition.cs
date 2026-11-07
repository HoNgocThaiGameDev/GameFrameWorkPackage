using System;
using System.Globalization;
using UnityEngine;

namespace NebulaSoft
{
    [Serializable]
    public sealed class QuestDefinition
    {
        [SerializeField] string id;
        [SerializeField] QuestCategory category = QuestCategory.Daily;
        [SerializeField] QuestGoalType goalType = QuestGoalType.CompleteLevels;
        [SerializeField] string title;
        [SerializeField] int targetValue = 1;
        [SerializeField] int sortOrder;
        [Tooltip("One available Daily or Weekly quest is selected from each non-negative rotation slot every reset.")]
        [SerializeField] int rotationSlot = -1;
        [Min(1), Tooltip("Relative chance of being selected among quests in the same rotation slot.")]
        [SerializeField] int selectionWeight = 1;
        [SerializeField] bool enabled = true;
        // Kept for existing Quest Database assets. New data is written to rewardData.
        [SerializeField, HideInInspector] CurrencyAmount reward = new CurrencyAmount(CurrencyType.Coins, 0);
        [SerializeField] QuestReward rewardData = new QuestReward();
        [SerializeField] int milestonePoints;
        [SerializeField] QuestGoTarget goTarget = QuestGoTarget.None;

        [Header("Event")]
        [SerializeField] bool eventActive;
        [Tooltip("Change this value for every new run of the same event to reset its progress.")]
        [SerializeField] string eventPeriodKey;

        public string Id => id != null ? id.Trim() : string.Empty;
        public QuestCategory Category => category;
        public QuestGoalType GoalType => goalType;
        public string Title => title;
        public int TargetValue => Mathf.Max(1, targetValue);
        public int SortOrder => sortOrder;
        public int RotationSlot => rotationSlot;
        public int SelectionWeight => Mathf.Max(1, selectionWeight);
        public bool Enabled => enabled;
        public QuestReward Reward => rewardData != null && rewardData.IsConfigured
            ? rewardData
            : QuestReward.FromLegacy(reward);
        public int MilestonePoints => Mathf.Max(0, milestonePoints);
        public QuestGoTarget GoTarget => goTarget;
        public bool EventActive => eventActive;

        public bool IsAvailable
        {
            get
            {
                if (!enabled || (category == QuestCategory.Event && !eventActive))
                    return false;

#if !MODULE_MONETIZATION
                if (goalType == QuestGoalType.WatchRewardedAds)
                    return false;
#endif

                return true;
            }
        }

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

                case QuestCategory.Event:
                    string key = string.IsNullOrWhiteSpace(eventPeriodKey) ? Id : eventPeriodKey.Trim();
                    return "event:" + key;

                default:
                    return string.Empty;
            }
        }
    }
}
