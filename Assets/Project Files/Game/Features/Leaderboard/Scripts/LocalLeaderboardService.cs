using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace NebulaSoft
{
    /// <summary>
    /// Offline sample data for the leaderboard popup. It deliberately has no
    /// network, authentication, cloud-save, or platform-service dependency.
    /// </summary>
    public static class LocalLeaderboardService
    {
        private const string LocalScoreKey = "Framework.LocalLeaderboard.Score";
        private const string LocalLevelKey = "Framework.LocalLeaderboard.MaxLevel";
        private const string DemoLeagueTier = "Bronze";
        private const string DemoLeagueGroup = "Demo-1";

        private static readonly string[] DemoNames =
        {
            "Luna", "Milo", "Ava", "Theo", "Nora", "Leo", "Mia", "You",
            "Ivy", "Max", "Zoe", "Kai", "Ruby", "Owen", "Ella", "Noah",
            "Sofia", "Finn", "Lily", "Jack", "Emma", "Eli", "Chloe", "Ryan"
        };

        private static List<LeaderboardEntry> globalEntries;
        private static List<LeaderboardEntry> leagueEntries;

        public static string CurrentSeasonId => "Offline-Demo";
        public static DateTime CurrentSeasonEndUtc => GetNextMondayUtc(DateTime.UtcNow);
        public static bool HasCurrentSeason => true;

        public static Task<bool> PreloadLeaderboardsAsync(int globalLimit = 100, int leagueLimit = 30, bool forceRefresh = false)
        {
            EnsureEntries();
            return Task.FromResult(true);
        }

        public static bool TryGetCachedGlobalPlayers(out List<LeaderboardEntry> entries)
        {
            EnsureEntries();
            entries = CloneEntries(globalEntries);
            return true;
        }

        public static bool TryGetCachedLeaguePlayers(out List<LeaderboardEntry> entries)
        {
            EnsureEntries();
            entries = CloneEntries(leagueEntries);
            return true;
        }

        public static Task<List<LeaderboardEntry>> GetTopPlayersAsync(int limit)
        {
            EnsureEntries();
            return Task.FromResult(CloneEntries(globalEntries.Take(Mathf.Max(0, limit)).ToList()));
        }

        public static Task<List<LeaderboardEntry>> GetGlobalPlayersAsync(int limit)
        {
            return GetTopPlayersAsync(limit);
        }

        public static Task<List<LeaderboardEntry>> GetCurrentLeaguePlayersAsync(int limit)
        {
            EnsureEntries();
            return Task.FromResult(CloneEntries(leagueEntries.Take(Mathf.Max(0, limit)).ToList()));
        }

        public static Task<bool> RecordCurrentLevelCompletionAsync()
        {
            EnsureEntries();

            LeaderboardEntry currentPlayer = globalEntries.Find(entry => entry.IsCurrentPlayer);
            if (currentPlayer == null)
                return Task.FromResult(false);

            currentPlayer.Score += 100;
            currentPlayer.MaxLevel++;
            PlayerPrefs.SetInt(LocalScoreKey, currentPlayer.Score);
            PlayerPrefs.SetInt(LocalLevelKey, currentPlayer.MaxLevel);
            PlayerPrefs.Save();
            RefreshRanks(globalEntries);
            UpdateLeagueCurrentPlayer(currentPlayer);
            return Task.FromResult(true);
        }

        public static TimeSpan GetCurrentSeasonRemaining()
        {
            TimeSpan remaining = CurrentSeasonEndUtc - DateTime.UtcNow;
            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }

        private static void EnsureEntries()
        {
            if (globalEntries != null && leagueEntries != null)
                return;

            globalEntries = new List<LeaderboardEntry>();
            for (int i = 0; i < DemoNames.Length; i++)
            {
                bool isCurrentPlayer = DemoNames[i] == "You";
                int score = isCurrentPlayer
                    ? Mathf.Max(850, PlayerPrefs.GetInt(LocalScoreKey, 1200))
                    : 4200 - i * 135;
                int maxLevel = isCurrentPlayer
                    ? Mathf.Max(1, PlayerPrefs.GetInt(LocalLevelKey, 12))
                    : 48 - i;

                globalEntries.Add(new LeaderboardEntry
                {
                    Uid = isCurrentPlayer ? "local-player" : "demo-" + i,
                    PlayerName = DemoNames[i],
                    AvatarIndex = i % 6,
                    FrameIndex = i % 4,
                    Score = score,
                    MaxLevel = maxLevel,
                    IsCurrentPlayer = isCurrentPlayer,
                    SeasonId = CurrentSeasonId,
                    LeagueTier = DemoLeagueTier,
                    LeagueGroupId = DemoLeagueGroup
                });
            }

            RefreshRanks(globalEntries);
            leagueEntries = CloneEntries(globalEntries.Take(12).ToList());
            UpdateLeagueCurrentPlayer(globalEntries.Find(entry => entry.IsCurrentPlayer));
            RefreshRanks(leagueEntries);
        }

        private static void UpdateLeagueCurrentPlayer(LeaderboardEntry currentPlayer)
        {
            if (currentPlayer == null)
                return;

            int index = leagueEntries.FindIndex(entry => entry.IsCurrentPlayer);
            LeaderboardEntry copy = CloneEntry(currentPlayer);
            if (index >= 0)
                leagueEntries[index] = copy;
            else
                leagueEntries.Add(copy);

            RefreshRanks(leagueEntries);
        }

        private static void RefreshRanks(List<LeaderboardEntry> entries)
        {
            entries.Sort((left, right) => right.Score.CompareTo(left.Score));
            for (int i = 0; i < entries.Count; i++)
                entries[i].Rank = i + 1;
        }

        private static List<LeaderboardEntry> CloneEntries(List<LeaderboardEntry> entries)
        {
            return entries.Select(CloneEntry).ToList();
        }

        private static LeaderboardEntry CloneEntry(LeaderboardEntry entry)
        {
            return new LeaderboardEntry
            {
                Uid = entry.Uid,
                Rank = entry.Rank,
                PlayerName = entry.PlayerName,
                AvatarIndex = entry.AvatarIndex,
                FrameIndex = entry.FrameIndex,
                Score = entry.Score,
                MaxLevel = entry.MaxLevel,
                IsCurrentPlayer = entry.IsCurrentPlayer,
                SeasonId = entry.SeasonId,
                LeagueTier = entry.LeagueTier,
                LeagueGroupId = entry.LeagueGroupId
            };
        }

        private static DateTime GetNextMondayUtc(DateTime now)
        {
            int daysUntilMonday = ((int)DayOfWeek.Monday - (int)now.DayOfWeek + 7) % 7;
            if (daysUntilMonday == 0)
                daysUntilMonday = 7;

            return now.Date.AddDays(daysUntilMonday);
        }
    }
}
