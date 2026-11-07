namespace NebulaSoft
{
    [System.Serializable]
    public class PlayerProfileSave : ISaveObject
    {
        public string FirebaseUid;
        public string PlayerName;
        public int AvatarIndex = 5;
        public int FrameIndex = 5;

        public void Flush()
        {
        }
    }
}
