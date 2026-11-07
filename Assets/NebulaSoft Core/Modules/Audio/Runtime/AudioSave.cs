namespace NebulaSoft
{
    [System.Serializable]
    public class AudioSave : ISaveObject
    {
        public VolumeData[] VolumeDatas;

        public void Flush()
        {
            if (!AudioController.IsInitialized) return;
            AudioType[] audioTypes = EnumUtils.GetEnumArray<AudioType>();

            VolumeDatas = new VolumeData[audioTypes.Length];

            for (int i = 0; i < audioTypes.Length; i++)
            {
                VolumeDatas[i] = new VolumeData() { AudioType = audioTypes[i], Volume = AudioController.GetVolume(audioTypes[i]) };
            }
        }

        [System.Serializable]
        public class VolumeData
        {
            public AudioType AudioType;
            public float Volume;
        }
    }
}
