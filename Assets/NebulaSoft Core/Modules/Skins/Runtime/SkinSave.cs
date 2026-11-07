namespace NebulaSoft
{
    [System.Serializable]
    [SaveModel("skin", CloudSync = true)]
    public class SkinSave : ISaveObject
    {
        public bool IsUnlocked = false;

        public void Flush()
        {

        }
    }
}
