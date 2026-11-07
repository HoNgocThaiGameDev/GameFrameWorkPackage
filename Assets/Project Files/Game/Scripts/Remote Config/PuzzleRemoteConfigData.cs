namespace NebulaSoft
{
    [UnityEngine.Scripting.Preserve]
    public sealed class PuzzleRemoteConfigData : RemoteConfigData
    {
        public const string ConfigKey = "puzzle";
        public override string Key => ConfigKey;

        public bool? rewardedReviveEnabled;
        public int? reviveOfferSeconds;
        public int? maxRewardedRevivesPerAttempt;
        public bool? interstitialOnFinalFailure;

        public override bool Validate()
        {
            return reviveOfferSeconds is not (< 1 or > 30) &&
                maxRewardedRevivesPerAttempt is not (< 0 or > 5);
        }
    }
}
