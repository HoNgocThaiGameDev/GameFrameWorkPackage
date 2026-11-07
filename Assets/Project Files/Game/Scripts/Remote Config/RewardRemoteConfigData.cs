namespace NebulaSoft
{
    [System.Serializable]
    [UnityEngine.Scripting.Preserve]
    public class RewardRemoteConfigData : RemoteConfigData
    {
        public override string Key => "reward";

        public int dReward = 20;
        public string dCurrency = "Coins";

        public int bReward = 25;
        public string bCurrency = "Coins";

        public int rvMult = 2;

        public int streakBonusPerWin = -1;
        public int streakBonusCap = -1;

        public override bool Validate()
        {
            return dReward >= 0 && bReward >= 0 && rvMult > 0 &&
                streakBonusPerWin >= -1 && streakBonusCap >= -1;
        }
    }
}
