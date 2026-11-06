using System;

namespace NebulaSoft.Analytics
{
    /// <summary>One attempt from start until win, final failure or quit. A revive continues the same attempt.</summary>
    public sealed class LevelTracker
    {
        private readonly AnalyticsEvents events;
        private readonly Func<double> clock;
        private double startedAt, pausedAt, pausedSeconds;
        private bool paused;
        private int attempt, boosters, moves;
        public int Level { get; private set; }
        public bool IsActive { get; private set; }

        public LevelTracker(AnalyticsEvents events, Func<double> clock)
        { this.events = events; this.clock = clock; }

        public void Start(int level, int attempt = 1)
        {
            if (IsActive) return;
            Level = level;
            this.attempt = attempt;
            boosters = moves = 0;
            startedAt = clock();
            pausedSeconds = 0;
            paused = false;
            IsActive = true;
            events.LevelStart(level, attempt);
        }

        public void Move() { if (IsActive) moves++; }
        public void SetPaused(bool value)
        {
            if (!IsActive || paused == value) return;
            if (value) pausedAt = clock();
            else pausedSeconds += Math.Max(0, clock() - pausedAt);
            paused = value;
        }
        public void Booster(string id, int remaining = 0)
        { if (!IsActive) return; boosters++; events.BoosterUsed(id, Level, remaining); }
        public void Fail(string reason) { if (IsActive) events.LevelFail(Level, reason); }
        public void Revive(string source) { if (IsActive) events.LevelRevive(Level, source); }
        public void End(bool success, string reason = null)
        {
            if (!IsActive) return;
            IsActive = false;
            double endedAt = paused ? pausedAt : clock();
            events.LevelEnd(Level, success, Math.Max(0, endedAt - startedAt - pausedSeconds), attempt, moves, boosters, reason);
        }
    }
}
