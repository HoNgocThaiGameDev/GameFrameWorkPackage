namespace NebulaSoft
{
    /// <summary>
    /// A presentation model for the framework's offline leaderboard sample.
    /// A production game may replace the data provider without changing the UI.
    /// </summary>
    public sealed class LeaderboardEntry
    {
        public string Uid;
        public int Rank;
        public string PlayerName;
        public int AvatarIndex;
        public int FrameIndex;
        public int Score;
        public int MaxLevel;
        public bool IsCurrentPlayer;
        public string SeasonId;
        public string LeagueTier;
        public string LeagueGroupId;
    }
}
