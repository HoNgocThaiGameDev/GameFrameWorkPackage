using System;
using System.Collections;
using UnityEngine;

namespace NebulaSoft
{
    [StaticUnload]
    public static class LivesSystem
    {
        public const string TEXT_FULL = "FULL!";
        public static IClock Clock { get; set; } = SystemClock.Instance;
        public const string TEXT_TIMESPAN_FORMAT = "{0:mm\\:ss}";
        public const string TEXT_LONG_TIMESPAN_FORMAT = "{0:hh\\:mm\\:ss}";

        private static LivesSave save;

        public static LivesStatus Status { get; private set; }

        public static int Lives { get => Status.LivesCount; private set => Status.SetLives(value); }
        public static int MaxLivesCount { get; private set; }

        public static bool InfiniteMode { get => Status.InfiniteMode; }

        public static bool IsFull { get => Lives >= MaxLivesCount; }
        public static bool IsLocked { get => save.LifeLocked; }

        public static TimeSpan OneLifeSpan { get; private set; }

        public static LivesRemoteConfigData RemoteConfigData { get; private set; }

        private static Coroutine infiniteModeCoroutine;
        private static Coroutine newLifeCoroutine;

        public static LivesData Data { get; private set; }

        public static event StatusChangedDelegate StatusChanged;

        public static void Init(LivesData livesData)
        {
            Data = livesData;

            RemoteConfigData = RemoteConfigController.TryGetConfig<LivesRemoteConfigData>("lives");

            int oneLifeRestorationDuration = livesData.OneLifeRestorationDuration;
            if(RemoteConfigData != null)
                oneLifeRestorationDuration = RemoteConfigData.resetTime;

            OneLifeSpan = TimeSpan.FromSeconds(oneLifeRestorationDuration);

            Status = new LivesStatus();

            save = SaveController.GetSaveObject<LivesSave>("Lives");
            save.Init(Status);

            if(save.MaxLivesCount == -1)
            {
                int maxLivesCount = livesData.MaxLivesCount;

                if(RemoteConfigData != null)
                    maxLivesCount = RemoteConfigData.maxCount;

                save.MaxLivesCount = maxLivesCount;
            }

            MaxLivesCount = save.MaxLivesCount;

            // Prepare save
            if (save.LivesCount == -1)
            {
                save.LivesCount = MaxLivesCount;
                save.LifeLocked = false;
            }

            // If game was left during the lock of live, decrease lives count
            if (save.LifeLocked)
            {
                save.LivesCount = Mathf.Clamp(save.LivesCount - 1, 0, int.MaxValue);
                save.LifeLocked = false;
            }

            Lives = save.LivesCount;

            if (save.InfiniteLives)
            {
                DateTime date = DateTime.FromBinary(save.InfiniteLivesDateBinary).ToUniversalTime();
                if (date > Clock.UtcNow)
                {
                    TimeSpan span = date - Clock.UtcNow;

                    EnableInfiniteMode(span.TotalSeconds);
                }
                else
                {
                    Status.SetInfiniteModeState(false);
                }
            }

            if (!Status.InfiniteMode && Lives < MaxLivesCount)
            {
                DateTime lastSavedDate = DateTime.FromBinary(save.NewLifeDateBinary).ToUniversalTime();
                TimeSpan offlineTime = Clock.UtcNow - lastSavedDate;

                // Check if the NewLifeDate is in the future
                if (offlineTime < TimeSpan.Zero)
                {
                    // NewLifeDate is in the future, set the new life date to lastSavedDate
                    Status.SetNewLifeDate(lastSavedDate);
                    Status.SetNewLifeTimerState(true);

                    // Start the coroutine to handle the life restoration process
                    newLifeCoroutine = Tween.InvokeCoroutine(LivesCoroutine());
                }
                else
                {
                    // NewLifeDate has already passed, add one life for reaching the NewLifeDate
                    Lives = Mathf.Clamp(Lives + 1, 0, MaxLivesCount);

                    // Check if lives are still not full
                    if (Lives < MaxLivesCount)
                    {
                        // Calculate additional lives that can be added based on the offline time
                        int additionalLives = Math.Max(0, (int)(offlineTime.TotalSeconds / OneLifeSpan.TotalSeconds));

                        if (additionalLives > 0)
                        {
                            // Add the additional lives
                            Lives = Mathf.Clamp(Lives + additionalLives, 0, MaxLivesCount);

                            // Check if lives are still not full
                            if (Lives < MaxLivesCount)
                            {
                                // Calculate the remaining time for the next life
                                TimeSpan remainingTime = TimeSpan.FromSeconds(OneLifeSpan.TotalSeconds - (offlineTime.TotalSeconds % OneLifeSpan.TotalSeconds));

                                // Set the new life date and start the timer
                                Status.SetNewLifeDate(Clock.UtcNow + remainingTime);
                                Status.SetNewLifeTimerState(true);

                                // Start the coroutine to handle the life restoration process
                                newLifeCoroutine = Tween.InvokeCoroutine(LivesCoroutine());
                            }
                        }
                        else
                        {
                            // If no additional lives can be added, set the new life date based on the remaining time
                            Status.SetNewLifeDate(Clock.UtcNow + TimeSpan.FromSeconds(OneLifeSpan.TotalSeconds - offlineTime.TotalSeconds));
                            Status.SetNewLifeTimerState(true);

                            // Start the coroutine to handle the life restoration process
                            newLifeCoroutine = Tween.InvokeCoroutine(LivesCoroutine());
                        }
                    }
                }
            }

            UpdateStatus();
        }

        public static void AddLife(int amount = 1, bool overrideMax = false)
        {
            if(overrideMax)
            {
                Lives += amount;
            }
            else
            {
                int livesDiff = (Lives + amount) - MaxLivesCount;
                if (livesDiff <= 0)
                {
                    Lives += amount;
                }
                else
                {
                    Lives += amount - livesDiff;
                }
            }

            SaveController.MarkAsSaveIsRequired();

            UpdateNewLife();
        }

        public static void RefillLifes()
        {
            Lives = MaxLivesCount;

            SaveController.MarkAsSaveIsRequired();

            UpdateNewLife();
        }

        public static void LockLife()
        {
            save.LifeLocked = true;

            SaveController.MarkAsSaveIsRequired();
        }

        public static void UnlockLife(bool decrease)
        {
            if (!save.LifeLocked) return;

            save.LifeLocked = false;

            SaveController.MarkAsSaveIsRequired();

            if (decrease)
                TakeLife();
        }

        private static void UpdateNewLife()
        {
            if (!InfiniteMode)
            {
                if (Lives >= MaxLivesCount)
                {
                    if (newLifeCoroutine != null)
                    {
                        Tween.StopCustomCoroutine(newLifeCoroutine);

                        newLifeCoroutine = null;
                    }

                    Status.SetNewLifeTimerState(false);
                }
                else
                {
                    if (newLifeCoroutine == null)
                    {
                        Status.SetNewLifeDate(Clock.UtcNow + OneLifeSpan);
                        Status.SetNewLifeTimerState(true);

                        newLifeCoroutine = Tween.InvokeCoroutine(LivesCoroutine());
                    }
                }
            }

            SaveController.MarkAsSaveIsRequired();

            UpdateStatus();
        }

        public static void TakeLife(int amount = 1)
        {
            if (InfiniteMode) return;

            Lives -= amount;

            if (Lives < 0)
                Lives = 0;

            SaveController.MarkAsSaveIsRequired();

            UpdateNewLife();
        }

        public static void OverrideMaxLivesCount(int livesCount, bool refillCurrentLives = false)
        {
            save.MaxLivesCount = livesCount;
            MaxLivesCount = livesCount;

            if(refillCurrentLives)
            {
                Lives = MaxLivesCount;
            }

            Status.RequestUpdate();

            UpdateStatus();
        }

        public static void EnableInfiniteMode(double seconds)
        {
            if (InfiniteMode)
            {
                TimeSpan time = TimeSpan.FromSeconds(seconds) + Status.InfiniteModeTime;

                Status.SetInfiniteModeTime(time);
                Status.SetInfiniteModeDate(Clock.UtcNow + time);
            }
            else
            {
                TimeSpan time = TimeSpan.FromSeconds(seconds);

                Status.SetInfiniteModeState(true);
                Status.SetInfiniteModeTime(time);
                Status.SetInfiniteModeDate(Clock.UtcNow + time);

                Status.SetNewLifeTimerState(false);

                infiniteModeCoroutine = Tween.InvokeCoroutine(InfiniteLivesCoroutine());
            }
        }

        public static void DisableInfiniteMode()
        {
            if (!InfiniteMode) return;

            Status.SetInfiniteModeState(false);

            if(infiniteModeCoroutine != null)
            {
                Tween.StopCustomCoroutine(infiniteModeCoroutine);

                infiniteModeCoroutine = null;
            }

            UpdateNewLife();
        }

        private static IEnumerator InfiniteLivesCoroutine()
        {
            WaitForSecondsRealtime wait = new WaitForSecondsRealtime(0.25f);
            while (Clock.UtcNow < Status.InfiniteModeDate)
            {
                TimeSpan time = Status.InfiniteModeDate - Clock.UtcNow;

                Status.SetInfiniteModeTime(time);

                UpdateStatus();

                SaveController.MarkAsSaveIsRequired();

                yield return wait;
            }

            Status.SetInfiniteModeState(false);

            if(Lives < MaxLivesCount)
                Status.SetLives(MaxLivesCount);

            UpdateStatus();

            SaveController.MarkAsSaveIsRequired();

            infiniteModeCoroutine = null;
        }

        private static IEnumerator LivesCoroutine()
        {
            WaitForSecondsRealtime wait = new WaitForSecondsRealtime(0.25f);

            while (Lives < MaxLivesCount)
            {
                TimeSpan timespan = Clock.UtcNow - Status.NewLifeDate;

                Status.SetNewLifeTime(timespan);

                if (timespan.Ticks > 0)
                {
                    Lives++;

                    if(Lives < MaxLivesCount)
                    {
                        Status.SetNewLifeDate(Clock.UtcNow + OneLifeSpan);

                        UpdateStatus();

                        continue;
                    }
                    else
                    {
                        break;
                    }
                }

                UpdateStatus();

                SaveController.MarkAsSaveIsRequired();

                yield return wait;
            }

            Status.SetNewLifeTimerState(false);

            UpdateStatus();

            SaveController.MarkAsSaveIsRequired();

            newLifeCoroutine = null;
        }

        public static string GetFormatedTime(TimeSpan time)
        {
            if (time.Hours > 0)
                return string.Format(TEXT_LONG_TIMESPAN_FORMAT, time);

            return string.Format(TEXT_TIMESPAN_FORMAT, time);
        }

        private static void UpdateStatus()
        {
            if (!Status.RequireUpdate) return;

            Status.MarkAsUpdated();

            StatusChanged?.Invoke(Status);
        }

        public static void Shutdown()
        {
            if (infiniteModeCoroutine != null) Tween.StopCustomCoroutine(infiniteModeCoroutine);
            if (newLifeCoroutine != null) Tween.StopCustomCoroutine(newLifeCoroutine);
            UnloadStatic();
        }

        private static void UnloadStatic()
        {
            Clock = SystemClock.Instance;
            StatusChanged = null;
            Status = null;
            save = null;

            infiniteModeCoroutine = null;
            newLifeCoroutine = null;
        }

        public delegate void StatusChangedDelegate(LivesStatus status);
    }
}
