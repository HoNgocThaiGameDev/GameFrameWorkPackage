using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace NebulaSoft
{
    [StaticUnload]
    public static class DailyRewardService
    {
        public const string SaveKey = "Daily Reward";
        private const string DateFormat = "yyyy-MM-dd";

        private static IDailyRewardScheduleProvider scheduleProvider;
        private static IDailyRewardClock clock;
        private static IDailyRewardStateStore stateStore;
        private static DailyRewardSave save;
        private static bool claimInProgress;

        public static bool IsInitialized => scheduleProvider != null && clock != null && stateStore != null && save != null;
        public static event Action StateChanged;

        public static void Init(DailyRewardDatabase database)
        {
            Init(database, new LocalDailyRewardClock(), new LocalDailyRewardStateStore());
        }

        public static void Init(IDailyRewardScheduleProvider provider, IDailyRewardClock rewardClock, IDailyRewardStateStore rewardStateStore)
        {
            scheduleProvider = provider;
            clock = rewardClock;
            stateStore = rewardStateStore;

            if (scheduleProvider == null || scheduleProvider.Days == null || scheduleProvider.Days.Count != DailyRewardDatabase.CycleLength)
            {
                Debug.LogError("[Daily Reward] A valid seven-day schedule is required.");
                return;
            }

            if (clock == null || stateStore == null || !SaveController.IsSaveLoaded)
            {
                Debug.LogError("[Daily Reward] Save and time services must be ready before initialization.");
                return;
            }

            save = stateStore.Load() ?? new DailyRewardSave();
            save.NextDayIndex = NormalizeDayIndex(save.NextDayIndex);
            save.ScheduleVersion = scheduleProvider.ScheduleVersion;
            NormalizeForCurrentDate();
        }

        public static DailyRewardState GetState()
        {
            if (!IsInitialized)
                return new DailyRewardState(0, -1, false, false);

            NormalizeForCurrentDate();

            string today = GetTodayKey();
            bool claimedToday = string.Equals(save.LastClaimedUtcDateKey, today, StringComparison.Ordinal);
            bool clockMovedBack = IsClockBeforeLastClaim(today);
            bool canClaim = !claimedToday && !clockMovedBack;
            int claimedDayIndex = claimedToday
                ? NormalizeDayIndex(save.NextDayIndex - 1)
                : -1;

            return new DailyRewardState(save.NextDayIndex, claimedDayIndex, canClaim, claimedToday);
        }

        public static DailyRewardDayDefinition GetDay(int dayIndex)
        {
            return !IsInitialized || dayIndex < 0 || dayIndex >= scheduleProvider.Days.Count
                ? null
                : scheduleProvider.Days[dayIndex];
        }

        public static bool TryClaimToday()
        {
            if (claimInProgress) return false;
            DailyRewardState state = GetState();
            if (!state.CanClaimToday)
                return false;

            DailyRewardDayDefinition day = GetDay(state.CurrentDayIndex);
            if (day == null || day.Rewards == null || day.Rewards.Count == 0)
            {
                Debug.LogWarning("[Daily Reward] Current reward day is not configured.");
                return false;
            }

            for (int i = 0; i < day.Rewards.Count; i++)
            {
                DailyRewardGrant reward = day.Rewards[i];
                if (reward == null || !reward.IsAvailable())
                {
                    Debug.LogWarning("[Daily Reward] A configured reward is not available yet.");
                    return false;
                }
            }

            claimInProgress = true;
            string previousDate = save.LastClaimedUtcDateKey;
            try
            {
                string today = GetTodayKey();
                if (save.PendingClaimUtcDateKey != today)
                {
                    save.PendingClaimUtcDateKey = today;
                    save.PendingGrantIndex = 0;
                }
                for (int i = save.PendingGrantIndex; i < day.Rewards.Count; i++)
                {
                    day.Rewards[i].Apply("DailyReward");
                    save.PendingGrantIndex = i + 1;
                }
                save.LastClaimedUtcDateKey = today;
                save.NextDayIndex = NormalizeDayIndex(state.CurrentDayIndex + 1);
                save.ScheduleVersion = scheduleProvider.ScheduleVersion;
                try { stateStore.Save(save); }
                catch (Exception exception)
                {
                    save.LastClaimedUtcDateKey = previousDate;
                    save.NextDayIndex = state.CurrentDayIndex;
                    Debug.LogException(exception);
                    return false;
                }
                AnalyticsController.Events.RewardClaimed("daily_reward", "day_" + (state.CurrentDayIndex + 1));
                NotifyStateChanged();
                return true;
            }
            finally { claimInProgress = false; }
        }

        /// <summary>
        /// Clears the local daily-reward progress so the next claim starts at Day 1.
        /// Intended for development and QA tooling only.
        /// </summary>
        public static bool ResetForTesting()
        {
            if (!IsInitialized || claimInProgress)
                return false;

            save.LastClaimedUtcDateKey = string.Empty;
            save.NextDayIndex = 0;
            save.ScheduleVersion = scheduleProvider.ScheduleVersion;
            save.PendingClaimUtcDateKey = string.Empty;
            save.PendingGrantIndex = 0;
            stateStore.Save(save);
            NotifyStateChanged();
            return true;
        }

        private static void NormalizeForCurrentDate()
        {
            if (!IsInitialized || string.IsNullOrEmpty(save.LastClaimedUtcDateKey))
                return;

            DateTime lastClaimedDate;
            if (!DateTime.TryParseExact(save.LastClaimedUtcDateKey, DateFormat, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out lastClaimedDate))
            {
                save.LastClaimedUtcDateKey = string.Empty;
                save.NextDayIndex = 0;
                return;
            }

            int elapsedDays = (clock.UtcNow.Date - lastClaimedDate.Date).Days;
            if (elapsedDays <= 1 || save.NextDayIndex == 0)
                return;

            save.NextDayIndex = 0;
        }

        private static bool IsClockBeforeLastClaim(string today)
        {
            if (string.IsNullOrEmpty(save.LastClaimedUtcDateKey))
                return false;

            DateTime todayDate;
            DateTime lastClaimedDate;
            return DateTime.TryParseExact(today, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out todayDate)
                && DateTime.TryParseExact(save.LastClaimedUtcDateKey, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out lastClaimedDate)
                && todayDate.Date < lastClaimedDate.Date;
        }

        private static string GetTodayKey()
        {
            return clock.UtcNow.ToString(DateFormat, CultureInfo.InvariantCulture);
        }

        private static int NormalizeDayIndex(int dayIndex)
        {
            int cycleLength = scheduleProvider != null && scheduleProvider.Days != null && scheduleProvider.Days.Count > 0
                ? scheduleProvider.Days.Count
                : DailyRewardDatabase.CycleLength;
            return ((dayIndex % cycleLength) + cycleLength) % cycleLength;
        }

        public static void Shutdown() => UnloadStatic();

        private static void UnloadStatic()
        {
            scheduleProvider = null;
            clock = null;
            stateStore = null;
            save = null;
            claimInProgress = false;
            StateChanged = null;
        }

        private static void NotifyStateChanged()
        {
            if (StateChanged == null) return;
            foreach (Action callback in StateChanged.GetInvocationList())
            {
                try { callback(); }
                catch (Exception exception) { Debug.LogException(exception); }
            }
        }
    }
}
