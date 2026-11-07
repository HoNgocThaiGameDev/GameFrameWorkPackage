using System;
using UnityEngine;

namespace NebulaSoft
{
    /// <summary>
    /// Represents the status of lives in the game, including the count of lives, infinite mode state, and new life timer state.
    /// This class provides methods to update the lives count, enable/disable infinite mode, and manage the new life timer.
    /// </summary>
    public class LivesStatus
    {
        public int LivesCount { get; private set; }

        public bool InfiniteMode { get; private set; }
        public DateTime InfiniteModeDate { get; private set; }
        public TimeSpan InfiniteModeTime { get; private set; }

        public bool NewLifeTimerEnabled { get; private set; }
        public DateTime NewLifeDate { get; private set; }
        public TimeSpan NewLifeTime { get; private set; }

        public bool RequireUpdate { get; private set; }

        public void SetLives(int lives)
        {
            LivesCount = lives;
            RequireUpdate = true;
        }

        public void SetInfiniteModeState(bool state)
        {
            InfiniteMode = state;
            RequireUpdate = true;
        }

        public void SetInfiniteModeDate(DateTime date)
        {
            InfiniteModeDate = date;
            RequireUpdate = true;
        }

        public void SetInfiniteModeTime(TimeSpan time)
        {
            InfiniteModeTime = time;
            RequireUpdate = true;
        }

        public void SetNewLifeTimerState(bool state)
        {
            NewLifeTimerEnabled = state;
            RequireUpdate = true;
        }

        public void SetNewLifeDate(DateTime date)
        {
            NewLifeDate = date;
            RequireUpdate = true;
        }

        public void SetNewLifeTime(TimeSpan time)
        {
            NewLifeTime = time;
            RequireUpdate = true;
        }

        public void RequestUpdate()
        {
            RequireUpdate = true;
        }

        public void MarkAsUpdated()
        {
            RequireUpdate = false;
        }
    }
}