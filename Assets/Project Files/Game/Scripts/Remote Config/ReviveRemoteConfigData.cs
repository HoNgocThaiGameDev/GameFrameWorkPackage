namespace NebulaSoft
{
    [System.Serializable]
    [UnityEngine.Scripting.Preserve]
    public class ReviveRemoteConfigData : RemoteConfigData
    {
        public override string Key => "revive";

        public int sec = 20;
        public int iapSec = 20;

        public string currency = "Coins";
        public int[] prices = new int[] { 900, 1200, 1500 };

        public override bool Validate()
        {
            return sec >= 0 && iapSec >= 0 && prices != null && prices.Length > 0 &&
                System.Array.TrueForAll(prices, price => price >= 0);
        }
    }
}

