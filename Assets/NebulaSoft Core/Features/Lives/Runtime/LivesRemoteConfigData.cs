namespace NebulaSoft
{
    [System.Serializable]
    [UnityEngine.Scripting.Preserve]
    public class LivesRemoteConfigData : RemoteConfigData
    {
        public override string Key => "lives";

        public int maxCount = 5;
        public int resetTime = 1200;

        public string currency = "Coins";
        public int price = 900;

        public override bool Validate() => maxCount > 0 && resetTime > 0 && price >= 0;
    }
}

