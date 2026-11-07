namespace NebulaSoft
{
    [System.Serializable]
    public class HapticPattern
    {
        public string ID;
        public HapticEvent[] Pattern;

        public HapticPattern(string id, HapticEvent[] pattern)
        {
            ID = id;
            Pattern = pattern;
        }
    }
}
