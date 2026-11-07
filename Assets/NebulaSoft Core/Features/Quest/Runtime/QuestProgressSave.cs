using System;
using System.Collections.Generic;

namespace NebulaSoft
{
    [Serializable]
    public sealed class QuestProgressEntry
    {
        public string QuestId;
        public string PeriodKey;
        public int Progress;
        public bool Claimed;
        public int RewardPowerUpType = -1;
        public float OnlineSeconds;
    }

    [Serializable]
    public sealed class QuestMilestoneClaimEntry
    {
        public string MilestoneId;
        public string PeriodKey;
    }

    [Serializable]
    public sealed class QuestPeriodSelectionEntry
    {
        public QuestCategory Category;
        public string PeriodKey;
        public List<string> QuestIds = new List<string>();
    }

    [Serializable]
    [SaveModel("Quest Progress", CloudSync = true)]
    public sealed class QuestProgressSave : ISaveObject
    {
        public List<QuestProgressEntry> Entries = new List<QuestProgressEntry>();
        public List<QuestMilestoneClaimEntry> MilestoneClaims = new List<QuestMilestoneClaimEntry>();
        public List<QuestPeriodSelectionEntry> PeriodSelections = new List<QuestPeriodSelectionEntry>();

        public void Flush()
        {
        }
    }

    public readonly struct QuestProgressState
    {
        public int Current { get; }
        public int Target { get; }
        public bool Claimed { get; }

        public bool IsComplete => Current >= Target;
        public float Normalized => Target > 0 ? (float)Current / Target : 0f;

        public QuestProgressState(int current, int target, bool claimed)
        {
            Current = Math.Max(0, current);
            Target = Math.Max(1, target);
            Claimed = claimed;
        }
    }

    public readonly struct QuestMilestoneState
    {
        public int CurrentPoints { get; }
        public int RequiredPoints { get; }
        public bool Claimed { get; }

        public bool IsUnlocked => CurrentPoints >= RequiredPoints;

        public QuestMilestoneState(int currentPoints, int requiredPoints, bool claimed)
        {
            CurrentPoints = Math.Max(0, currentPoints);
            RequiredPoints = Math.Max(1, requiredPoints);
            Claimed = claimed;
        }
    }
}
