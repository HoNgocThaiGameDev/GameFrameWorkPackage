using System.Collections.Generic;
using UnityEngine;

namespace NebulaSoft
{
    [CreateAssetMenu(fileName = "Daily Reward Database", menuName = "Data/Daily Reward/Database")]
    public sealed class DailyRewardDatabase : ScriptableObject, IDailyRewardScheduleProvider
    {
        public const int CycleLength = 7;

        [SerializeField, Min(1)] int scheduleVersion = 1;
        [SerializeField] List<DailyRewardDayDefinition> days = new List<DailyRewardDayDefinition>();

        public int ScheduleVersion => Mathf.Max(1, scheduleVersion);
        public IReadOnlyList<DailyRewardDayDefinition> Days => days;
        public bool IsValid => days != null && days.Count == CycleLength;

        public DailyRewardDayDefinition GetDay(int dayIndex)
        {
            return !IsValid || dayIndex < 0 || dayIndex >= days.Count ? null : days[dayIndex];
        }

        public void ReplaceSchedule(int version, params DailyRewardDayDefinition[] schedule)
        {
            scheduleVersion = Mathf.Max(1, version);
            days = schedule == null
                ? new List<DailyRewardDayDefinition>()
                : new List<DailyRewardDayDefinition>(schedule);
        }
    }
}
