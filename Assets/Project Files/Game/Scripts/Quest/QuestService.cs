using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace NebulaSoft
{
    [StaticUnload]
    public static class QuestService
    {
        public const string SaveKey = "Quest Progress";

        private static QuestDatabase database;
        private static QuestProgressSave save;
        private static QuestOnlineTimeTracker onlineTimeTracker;
        private static bool callbacksRegistered;

        public static bool IsInitialized => database != null && save != null;
        public static QuestDatabase Database => database;

        public static event Action DataChanged;
        public static event Action<QuestDefinition> QuestChanged;
        public static event Action<QuestDefinition> QuestClaimed;

        public static void Init(QuestDatabase questDatabase)
        {
            UnregisterRuntimeCallbacks();
            database = questDatabase;
            save = null;

            if (database == null)
            {
                Debug.LogError("QuestDatabase is not assigned in Project Init Settings.");
                return;
            }

            if (!SaveController.IsSaveLoaded)
            {
                Debug.LogError("QuestService must be initialized after SaveController.");
                return;
            }

            save = SaveController.GetSaveObject<QuestProgressSave>(SaveKey);
            EnsureSaveEntries();

            DateTime utcNow = DateTime.UtcNow;
            PruneExpiredEntries(utcNow);
            EnsureCurrentSelections(utcNow);

            RegisterRuntimeCallbacks();
            EnsureOnlineTimeTracker();
            DataChanged?.Invoke();
        }

        public static void GetDefinitions(QuestCategory category, List<QuestDefinition> results)
        {
            if (results == null)
                return;

            results.Clear();
            if (database == null)
                return;

            if (!IsInitialized || category == QuestCategory.Event)
            {
                database.GetDefinitions(category, results);
                return;
            }

            QuestPeriodSelectionEntry selection = GetOrCreatePeriodSelection(category, DateTime.UtcNow);
            if (selection == null || selection.QuestIds == null)
            {
                database.GetDefinitions(category, results);
                return;
            }

            for (int i = 0; i < selection.QuestIds.Count; i++)
            {
                QuestDefinition definition = database.GetDefinition(selection.QuestIds[i]);
                if (definition != null && definition.IsAvailable && definition.Category == category)
                    results.Add(definition);
            }

            IReadOnlyList<QuestDefinition> definitions = database.Definitions;
            for (int i = 0; i < definitions.Count; i++)
            {
                QuestDefinition definition = definitions[i];
                if (definition != null && definition.IsAvailable && definition.Category == category
                    && definition.RotationSlot < 0)
                    results.Add(definition);
            }

            results.Sort(CompareDefinitions);
        }

        public static void GetMilestones(QuestCategory category, List<QuestMilestoneDefinition> results)
        {
            if (results == null)
                return;

            results.Clear();
            if (database == null)
                return;

            database.GetMilestones(category, results);
        }

        public static QuestDefinition GetDefinition(string questId)
        {
            return database != null ? database.GetDefinition(questId) : null;
        }

        public static QuestReward GetReward(QuestDefinition definition)
        {
            QuestReward reward = definition != null ? definition.Reward : null;
            if (!IsInitialized || reward == null || reward.Type != QuestRewardType.RandomPowerUp)
                return reward;

            DateTime utcNow = DateTime.UtcNow;
            if (!IsSelectedForCurrentPeriod(definition, utcNow))
                return reward;

            QuestProgressEntry entry = GetOrCreateEntry(definition, utcNow);
            if (entry == null)
                return reward;

            if (entry.RewardPowerUpType < 0)
                return reward;

            return new QuestReward((PUType)entry.RewardPowerUpType, reward.Amount);
        }

        public static QuestProgressState GetProgress(QuestDefinition definition)
        {
            if (definition == null)
                return new QuestProgressState(0, 1, false);

            DateTime utcNow = DateTime.UtcNow;
            if (!IsSelectedForCurrentPeriod(definition, utcNow))
                return new QuestProgressState(0, definition.TargetValue, false);

            QuestProgressEntry entry = FindEntry(definition, utcNow);
            return new QuestProgressState(entry != null ? entry.Progress : 0, definition.TargetValue, entry != null && entry.Claimed);
        }

        public static QuestProgressState GetProgress(string questId)
        {
            return GetProgress(GetDefinition(questId));
        }

        public static int GetMilestonePoints(QuestCategory category)
        {
            return !IsInitialized ? 0 : GetMilestonePoints(category, DateTime.UtcNow);
        }

        public static QuestMilestoneState GetMilestoneState(QuestMilestoneDefinition milestone)
        {
            if (!IsInitialized || milestone == null || !milestone.IsAvailable)
                return new QuestMilestoneState(0, 1, false);

            DateTime utcNow = DateTime.UtcNow;
            int points = GetMilestonePoints(milestone.Category, utcNow);
            bool claimed = FindMilestoneClaim(milestone, utcNow) != null;
            return new QuestMilestoneState(points, milestone.RequiredPoints, claimed);
        }

        public static bool ReportProgress(QuestGoalType goalType, int amount = 1)
        {
            if (!IsInitialized || amount <= 0)
                return false;

            DateTime utcNow = DateTime.UtcNow;
            bool anyChanged = false;
            IReadOnlyList<QuestDefinition> definitions = database.Definitions;
            for (int i = 0; i < definitions.Count; i++)
            {
                QuestDefinition definition = definitions[i];
                if (definition == null || !definition.IsAvailable || definition.GoalType != goalType
                    || !IsSelectedForCurrentPeriod(definition, utcNow))
                    continue;

                anyChanged |= AddProgress(definition, amount, utcNow);
            }

            if (anyChanged)
                NotifyDataChanged();

            return anyChanged;
        }

        public static bool ReportProgress(string questId, int amount = 1)
        {
            if (!IsInitialized || amount <= 0)
                return false;

            QuestDefinition definition = GetDefinition(questId);
            DateTime utcNow = DateTime.UtcNow;
            if (definition == null || !definition.IsAvailable || !IsSelectedForCurrentPeriod(definition, utcNow))
                return false;

            bool changed = AddProgress(definition, amount, utcNow);
            if (changed)
                NotifyDataChanged();

            return changed;
        }

        public static bool ReportOnlineSeconds(float seconds)
        {
            if (!IsInitialized || seconds <= 0f)
                return false;

            DateTime utcNow = DateTime.UtcNow;
            bool anySaved = false;
            bool progressChanged = false;
            IReadOnlyList<QuestDefinition> definitions = database.Definitions;
            for (int i = 0; i < definitions.Count; i++)
            {
                QuestDefinition definition = definitions[i];
                if (definition == null || !definition.IsAvailable || definition.GoalType != QuestGoalType.OnlineMinutes
                    || !IsSelectedForCurrentPeriod(definition, utcNow))
                    continue;

                QuestProgressEntry entry = GetOrCreateEntry(definition, utcNow);
                if (entry == null || entry.Claimed || entry.Progress >= definition.TargetValue)
                    continue;

                entry.OnlineSeconds = Mathf.Max(entry.OnlineSeconds, entry.Progress * 60f);
                entry.OnlineSeconds = Mathf.Min(definition.TargetValue * 60f, entry.OnlineSeconds + seconds);

                int previousProgress = entry.Progress;
                entry.Progress = Mathf.Min(definition.TargetValue, Mathf.FloorToInt(entry.OnlineSeconds / 60f));
                SaveController.MarkAsSaveIsRequired();
                anySaved = true;

                if (entry.Progress != previousProgress)
                {
                    QuestChanged?.Invoke(definition);
                    progressChanged = true;
                }
            }

            if (progressChanged)
                NotifyDataChanged();

            return anySaved;
        }

        public static bool TryClaim(string questId)
        {
            return TryClaim(GetDefinition(questId));
        }

        public static bool TryClaim(QuestDefinition definition)
        {
            if (!IsInitialized || definition == null || !definition.IsAvailable)
                return false;

            DateTime utcNow = DateTime.UtcNow;
            PruneExpiredEntries(utcNow);

            if (!IsSelectedForCurrentPeriod(definition, utcNow))
                return false;

            QuestProgressEntry entry = GetOrCreateEntry(definition, utcNow);
            if (entry == null || entry.Claimed || entry.Progress < definition.TargetValue)
                return false;

            QuestReward reward = GetReward(definition);
            if (reward != null && !reward.TryApply("QuestClaim"))
                return false;

            entry.Claimed = true;
            SaveController.MarkAsSaveIsRequired();
            AnalyticsController.Events.RewardClaimed("quest", definition.Id);

            QuestClaimed?.Invoke(definition);
            QuestChanged?.Invoke(definition);
            DataChanged?.Invoke();
            return true;
        }

        public static bool TryCompleteWithRewardedAd(QuestDefinition definition)
        {
            if (!IsInitialized || definition == null || !definition.IsAvailable)
                return false;

            // A rewarded-ad quest must advance one successful view at a time. It cannot
            // use the generic ad skip, otherwise one view could bypass its target.
            if (definition.GoalType == QuestGoalType.WatchRewardedAds)
                return false;

            DateTime utcNow = DateTime.UtcNow;
            if (!IsSelectedForCurrentPeriod(definition, utcNow))
                return false;

            QuestProgressEntry entry = GetOrCreateEntry(definition, utcNow);
            if (entry == null || entry.Claimed || entry.Progress >= definition.TargetValue)
                return false;

            entry.Progress = definition.TargetValue;
            SaveController.MarkAsSaveIsRequired();
            QuestChanged?.Invoke(definition);
            DataChanged?.Invoke();
            return true;
        }

        public static int CompleteAllActiveForTesting()
        {
            if (!IsInitialized)
                return 0;

            DateTime utcNow = DateTime.UtcNow;
            int completedCount = 0;
            IReadOnlyList<QuestDefinition> definitions = database.Definitions;
            for (int i = 0; i < definitions.Count; i++)
            {
                QuestDefinition definition = definitions[i];
                if (definition == null || !definition.IsAvailable || definition.Category == QuestCategory.Event
                    || !IsSelectedForCurrentPeriod(definition, utcNow))
                    continue;

                QuestProgressEntry entry = GetOrCreateEntry(definition, utcNow);
                if (entry == null || entry.Claimed || entry.Progress >= definition.TargetValue)
                    continue;

                entry.Progress = definition.TargetValue;
                if (definition.GoalType == QuestGoalType.OnlineMinutes)
                    entry.OnlineSeconds = definition.TargetValue * 60f;

                completedCount++;
                QuestChanged?.Invoke(definition);
            }

            if (completedCount <= 0)
                return 0;

            SaveController.MarkAsSaveIsRequired();
            SaveController.Save(false, false);
            DataChanged?.Invoke();
            return completedCount;
        }

        public static bool TryClaimMilestone(QuestMilestoneDefinition milestone)
        {
            if (!IsInitialized || milestone == null || !milestone.IsAvailable)
                return false;

            DateTime utcNow = DateTime.UtcNow;
            PruneExpiredEntries(utcNow);

            if (FindMilestoneClaim(milestone, utcNow) != null)
                return false;

            int points = GetMilestonePoints(milestone.Category, utcNow);
            if (points < milestone.RequiredPoints)
                return false;

            QuestReward reward = milestone.Reward;
            if (reward != null && !reward.TryApply("QuestMilestoneClaim"))
                return false;

            save.MilestoneClaims.Add(new QuestMilestoneClaimEntry
            {
                MilestoneId = milestone.Id,
                PeriodKey = milestone.GetPeriodKey(utcNow)
            });
            SaveController.MarkAsSaveIsRequired();
            AnalyticsController.Events.RewardClaimed("quest_milestone", milestone.Id);

            DataChanged?.Invoke();
            return true;
        }

        public static void RefreshPeriods()
        {
            if (!IsInitialized)
                return;

            DateTime utcNow = DateTime.UtcNow;
            bool changed = PruneExpiredEntries(utcNow);
            changed |= EnsureCurrentSelections(utcNow);
            if (!changed)
                return;

            NotifyDataChanged();
        }

        public static TimeSpan GetTimeRemaining(QuestCategory category)
        {
            DateTime utcNow = DateTime.UtcNow;
            switch (category)
            {
                case QuestCategory.Daily:
                    return utcNow.Date.AddDays(1) - utcNow;

                case QuestCategory.Weekly:
                    int daysUntilMonday = (7 - ((int)utcNow.DayOfWeek + 6) % 7) % 7;
                    if (daysUntilMonday == 0)
                        daysUntilMonday = 7;
                    return utcNow.Date.AddDays(daysUntilMonday) - utcNow;

                default:
                    return TimeSpan.Zero;
            }
        }

        private static bool AddProgress(QuestDefinition definition, int amount, DateTime utcNow)
        {
            if (!IsSelectedForCurrentPeriod(definition, utcNow))
                return false;

            QuestProgressEntry entry = GetOrCreateEntry(definition, utcNow);
            if (entry == null || entry.Claimed || entry.Progress >= definition.TargetValue)
                return false;

            int newProgress = (int)Math.Min((long)definition.TargetValue, (long)entry.Progress + amount);
            if (newProgress == entry.Progress)
                return false;

            entry.Progress = newProgress;
            SaveController.MarkAsSaveIsRequired();
            QuestChanged?.Invoke(definition);
            return true;
        }

        private static int GetMilestonePoints(QuestCategory category, DateTime utcNow)
        {
            int points = 0;
            IReadOnlyList<QuestDefinition> definitions = database.Definitions;
            for (int i = 0; i < definitions.Count; i++)
            {
                QuestDefinition definition = definitions[i];
                if (definition == null || !definition.IsAvailable || definition.Category != category
                    || !IsSelectedForCurrentPeriod(definition, utcNow))
                    continue;

                QuestProgressEntry entry = FindEntry(definition, utcNow);
                if (entry != null && entry.Progress >= definition.TargetValue)
                    points += definition.MilestonePoints;
            }

            return points;
        }

        private static bool EnsureCurrentSelections(DateTime utcNow)
        {
            bool dailyChanged;
            bool weeklyChanged;
            GetOrCreatePeriodSelection(QuestCategory.Daily, utcNow, out dailyChanged);
            GetOrCreatePeriodSelection(QuestCategory.Weekly, utcNow, out weeklyChanged);
            return dailyChanged || weeklyChanged;
        }

        private static bool IsSelectedForCurrentPeriod(QuestDefinition definition, DateTime utcNow)
        {
            if (definition == null || definition.RotationSlot < 0 || !IsRotatingCategory(definition.Category))
                return true;

            QuestPeriodSelectionEntry selection = GetOrCreatePeriodSelection(definition.Category, utcNow);
            return selection != null && selection.QuestIds != null && selection.QuestIds.Contains(definition.Id);
        }

        private static QuestPeriodSelectionEntry GetOrCreatePeriodSelection(QuestCategory category, DateTime utcNow)
        {
            bool ignored;
            return GetOrCreatePeriodSelection(category, utcNow, out ignored);
        }

        private static QuestPeriodSelectionEntry GetOrCreatePeriodSelection(QuestCategory category, DateTime utcNow, out bool changed)
        {
            changed = false;
            if (!IsRotatingCategory(category) || database == null || save == null)
                return null;

            if (save.PeriodSelections == null)
            {
                save.PeriodSelections = new List<QuestPeriodSelectionEntry>();
                changed = true;
            }

            QuestPeriodSelectionEntry selection = FindPeriodSelection(category, utcNow);
            if (IsSelectionValid(selection, category))
                return selection;

            if (selection != null)
            {
                save.PeriodSelections.Remove(selection);
                changed = true;
            }

            QuestPeriodSelectionEntry created = CreatePeriodSelection(category, utcNow);
            if (created != null)
            {
                save.PeriodSelections.Add(created);
                changed = true;
            }

            if (changed)
                SaveController.MarkAsSaveIsRequired();

            return created;
        }

        private static QuestPeriodSelectionEntry CreatePeriodSelection(QuestCategory category, DateTime utcNow)
        {
            Dictionary<int, List<QuestDefinition>> candidatesBySlot = new Dictionary<int, List<QuestDefinition>>();
            IReadOnlyList<QuestDefinition> definitions = database.Definitions;
            for (int i = 0; i < definitions.Count; i++)
            {
                QuestDefinition definition = definitions[i];
                if (definition == null || !definition.IsAvailable || definition.Category != category || definition.RotationSlot < 0)
                    continue;

                List<QuestDefinition> candidates;
                if (!candidatesBySlot.TryGetValue(definition.RotationSlot, out candidates))
                {
                    candidates = new List<QuestDefinition>();
                    candidatesBySlot.Add(definition.RotationSlot, candidates);
                }

                candidates.Add(definition);
            }

            if (candidatesBySlot.Count == 0)
                return null;

            List<int> slots = new List<int>(candidatesBySlot.Keys);
            slots.Sort();

            QuestPeriodSelectionEntry selection = new QuestPeriodSelectionEntry
            {
                Category = category,
                PeriodKey = GetCategoryPeriodKey(category, utcNow)
            };

            for (int i = 0; i < slots.Count; i++)
            {
                List<QuestDefinition> candidates = candidatesBySlot[slots[i]];
                QuestDefinition selected = FindProgressedCandidate(candidates, utcNow);
                if (selected == null)
                    selected = SelectWeightedCandidate(candidates);

                selection.QuestIds.Add(selected.Id);
            }

            return selection;
        }

        private static QuestDefinition FindProgressedCandidate(List<QuestDefinition> candidates, DateTime utcNow)
        {
            QuestDefinition selected = null;
            int highestProgress = -1;
            bool selectedClaimed = false;

            for (int i = 0; i < candidates.Count; i++)
            {
                QuestProgressEntry entry = FindEntry(candidates[i], utcNow);
                if (entry == null || (!entry.Claimed && entry.Progress <= 0))
                    continue;

                if (selected == null || (entry.Claimed && !selectedClaimed)
                    || (entry.Claimed == selectedClaimed && entry.Progress > highestProgress))
                {
                    selected = candidates[i];
                    highestProgress = entry.Progress;
                    selectedClaimed = entry.Claimed;
                }
            }

            return selected;
        }

        private static QuestDefinition SelectWeightedCandidate(List<QuestDefinition> candidates)
        {
            if (candidates == null || candidates.Count == 0)
                return null;

            int totalWeight = 0;
            for (int i = 0; i < candidates.Count; i++)
            {
                QuestDefinition candidate = candidates[i];
                if (candidate == null)
                    continue;

                totalWeight = Mathf.Min(int.MaxValue, totalWeight + candidate.SelectionWeight);
            }

            if (totalWeight <= 0)
                return candidates[UnityEngine.Random.Range(0, candidates.Count)];

            int roll = UnityEngine.Random.Range(0, totalWeight);
            for (int i = 0; i < candidates.Count; i++)
            {
                QuestDefinition candidate = candidates[i];
                if (candidate == null)
                    continue;

                roll -= candidate.SelectionWeight;
                if (roll < 0)
                    return candidate;
            }

            return candidates[candidates.Count - 1];
        }

        private static QuestPeriodSelectionEntry FindPeriodSelection(QuestCategory category, DateTime utcNow)
        {
            if (save.PeriodSelections == null)
                return null;

            string periodKey = GetCategoryPeriodKey(category, utcNow);
            for (int i = 0; i < save.PeriodSelections.Count; i++)
            {
                QuestPeriodSelectionEntry entry = save.PeriodSelections[i];
                if (entry != null && entry.Category == category
                    && string.Equals(entry.PeriodKey, periodKey, StringComparison.Ordinal))
                    return entry;
            }

            return null;
        }

        private static bool IsSelectionValid(QuestPeriodSelectionEntry selection, QuestCategory category)
        {
            if (selection == null || selection.QuestIds == null)
                return false;

            HashSet<int> expectedSlots = new HashSet<int>();
            IReadOnlyList<QuestDefinition> definitions = database.Definitions;
            for (int i = 0; i < definitions.Count; i++)
            {
                QuestDefinition definition = definitions[i];
                if (definition != null && definition.IsAvailable && definition.Category == category && definition.RotationSlot >= 0)
                    expectedSlots.Add(definition.RotationSlot);
            }

            if (expectedSlots.Count == 0)
                return false;

            HashSet<int> selectedSlots = new HashSet<int>();
            HashSet<string> selectedIds = new HashSet<string>();
            for (int i = 0; i < selection.QuestIds.Count; i++)
            {
                QuestDefinition definition = database.GetDefinition(selection.QuestIds[i]);
                if (definition == null || !definition.IsAvailable || definition.Category != category
                    || definition.RotationSlot < 0 || !selectedIds.Add(definition.Id)
                    || !selectedSlots.Add(definition.RotationSlot))
                    return false;
            }

            return selectedSlots.SetEquals(expectedSlots);
        }

        private static bool IsRotatingCategory(QuestCategory category)
        {
            return category == QuestCategory.Daily || category == QuestCategory.Weekly;
        }

        private static string GetCategoryPeriodKey(QuestCategory category, DateTime utcNow)
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

        private static int CompareDefinitions(QuestDefinition first, QuestDefinition second)
        {
            int sortOrder = first.SortOrder.CompareTo(second.SortOrder);
            return sortOrder != 0 ? sortOrder : string.CompareOrdinal(first.Id, second.Id);
        }

        private static QuestProgressEntry FindEntry(QuestDefinition definition, DateTime utcNow)
        {
            if (!IsInitialized || definition == null || string.IsNullOrWhiteSpace(definition.Id))
                return null;

            string periodKey = definition.GetPeriodKey(utcNow);
            for (int i = 0; i < save.Entries.Count; i++)
            {
                QuestProgressEntry entry = save.Entries[i];
                if (entry != null
                    && string.Equals(entry.QuestId, definition.Id, StringComparison.Ordinal)
                    && string.Equals(entry.PeriodKey, periodKey, StringComparison.Ordinal))
                    return entry;
            }

            return null;
        }

        private static QuestProgressEntry GetOrCreateEntry(QuestDefinition definition, DateTime utcNow)
        {
            if (definition == null || string.IsNullOrWhiteSpace(definition.Id))
                return null;

            QuestProgressEntry entry = FindEntry(definition, utcNow);
            if (entry != null)
                return entry;

            entry = new QuestProgressEntry
            {
                QuestId = definition.Id,
                PeriodKey = definition.GetPeriodKey(utcNow),
                Progress = 0,
                Claimed = false
            };

            save.Entries.Add(entry);
            SaveController.MarkAsSaveIsRequired();
            return entry;
        }

        private static QuestMilestoneClaimEntry FindMilestoneClaim(QuestMilestoneDefinition milestone, DateTime utcNow)
        {
            if (milestone == null || save.MilestoneClaims == null)
                return null;

            string periodKey = milestone.GetPeriodKey(utcNow);
            for (int i = 0; i < save.MilestoneClaims.Count; i++)
            {
                QuestMilestoneClaimEntry entry = save.MilestoneClaims[i];
                if (entry != null
                    && string.Equals(entry.MilestoneId, milestone.Id, StringComparison.Ordinal)
                    && string.Equals(entry.PeriodKey, periodKey, StringComparison.Ordinal))
                    return entry;
            }

            return null;
        }

        private static void EnsureSaveEntries()
        {
            if (save.Entries == null)
            {
                save.Entries = new List<QuestProgressEntry>();
                SaveController.MarkAsSaveIsRequired();
            }

            if (save.MilestoneClaims == null)
            {
                save.MilestoneClaims = new List<QuestMilestoneClaimEntry>();
                SaveController.MarkAsSaveIsRequired();
            }

            if (save.PeriodSelections == null)
            {
                save.PeriodSelections = new List<QuestPeriodSelectionEntry>();
                SaveController.MarkAsSaveIsRequired();
            }
        }

        private static bool PruneExpiredEntries(DateTime utcNow)
        {
            if (!IsInitialized)
                return false;

            bool changed = false;
            for (int i = save.Entries.Count - 1; i >= 0; i--)
            {
                QuestProgressEntry entry = save.Entries[i];
                QuestDefinition definition = entry != null ? database.GetDefinition(entry.QuestId) : null;
                if (definition == null || !string.Equals(entry.PeriodKey, definition.GetPeriodKey(utcNow), StringComparison.Ordinal))
                {
                    save.Entries.RemoveAt(i);
                    changed = true;
                }
            }

            for (int i = save.MilestoneClaims.Count - 1; i >= 0; i--)
            {
                QuestMilestoneClaimEntry entry = save.MilestoneClaims[i];
                QuestMilestoneDefinition milestone = entry != null ? database.GetMilestone(entry.MilestoneId) : null;
                if (milestone == null || !milestone.IsAvailable
                    || !string.Equals(entry.PeriodKey, milestone.GetPeriodKey(utcNow), StringComparison.Ordinal))
                {
                    save.MilestoneClaims.RemoveAt(i);
                    changed = true;
                }
            }

            for (int i = save.PeriodSelections.Count - 1; i >= 0; i--)
            {
                QuestPeriodSelectionEntry entry = save.PeriodSelections[i];
                if (entry == null || !IsRotatingCategory(entry.Category)
                    || !string.Equals(entry.PeriodKey, GetCategoryPeriodKey(entry.Category, utcNow), StringComparison.Ordinal))
                {
                    save.PeriodSelections.RemoveAt(i);
                    changed = true;
                }
            }

            if (changed)
                SaveController.MarkAsSaveIsRequired();

            return changed;
        }

        private static void NotifyDataChanged()
        {
            DataChanged?.Invoke();
        }

        private static void RegisterRuntimeCallbacks()
        {
            if (callbacksRegistered || CurrencyController.Currencies == null)
                return;

            CurrencyController.SubscribeGlobalCallback(OnCurrencyChanged);
#if MODULE_MONETIZATION
            AdsManager.RewardedVideoRewarded += OnRewardedVideoRewarded;
#endif
            callbacksRegistered = true;
        }

        private static void EnsureOnlineTimeTracker()
        {
            if (onlineTimeTracker == null)
            {
                GameObject trackerObject = new GameObject("[QUEST ONLINE TIME]");
                UnityEngine.Object.DontDestroyOnLoad(trackerObject);
                onlineTimeTracker = trackerObject.AddComponent<QuestOnlineTimeTracker>();
            }

            onlineTimeTracker.Initialise();
        }

        private static void UnregisterRuntimeCallbacks()
        {
            if (!callbacksRegistered)
                return;

            CurrencyController.UnsubscribeGlobalCallback(OnCurrencyChanged);
#if MODULE_MONETIZATION
            AdsManager.RewardedVideoRewarded -= OnRewardedVideoRewarded;
#endif
            callbacksRegistered = false;
        }

        private static void OnCurrencyChanged(Currency currency, int difference)
        {
            if (currency != null && currency.CurrencyType == CurrencyType.Coins && difference < 0)
                ReportProgress(QuestGoalType.SpendCoins, -difference);
        }

        private static void OnPowerUpUsed(PUType unusedPowerUpType)
        {
            ReportProgress(QuestGoalType.UsePowerUp);
        }

#if MODULE_MONETIZATION
        private static void OnRewardedVideoRewarded()
        {
            ReportProgress(QuestGoalType.WatchRewardedAds);
        }
#endif

        private static void UnloadStatic()
        {
            UnregisterRuntimeCallbacks();
            if (onlineTimeTracker != null)
                UnityEngine.Object.Destroy(onlineTimeTracker.gameObject);
            onlineTimeTracker = null;
            database = null;
            save = null;
            DataChanged = null;
            QuestChanged = null;
            QuestClaimed = null;
        }
    }
}
