namespace NebulaSoft
{
    [SaveModel("Daily Reward", CloudSync = true)]
    public sealed class DailyRewardSave : ISaveObject
    {
        public string LastClaimedUtcDateKey = string.Empty;
        public int NextDayIndex;
        public int ScheduleVersion = 1;
        public string PendingClaimUtcDateKey = string.Empty;
        public int PendingGrantIndex;

        public void Flush() { }
    }
}
