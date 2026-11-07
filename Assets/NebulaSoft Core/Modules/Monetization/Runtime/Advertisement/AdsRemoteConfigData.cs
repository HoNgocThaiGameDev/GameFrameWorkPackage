namespace NebulaSoft
{
    [System.Serializable]
    [UnityEngine.Scripting.Preserve]
    public class AdsRemoteConfigData : RemoteConfigData
    {
        public override string Key => "ads";

        public int minLevel = 3;
        public int intDelay = 30;
        public int intSDelay = 40;
        public int intFSDelay = 40;

        public bool useBanner = true;
        public bool useInterstitials = true;

        public override bool Validate() => minLevel >= 0 && intDelay >= 0 && intSDelay >= 0 && intFSDelay >= 0;
    }
}
