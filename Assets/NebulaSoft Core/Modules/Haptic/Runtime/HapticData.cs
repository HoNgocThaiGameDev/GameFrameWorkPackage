namespace NebulaSoft
{
    public class HapticData
    {
        public float Duration = 0.05f;
        public float Intensity = 0.0f;

        public HapticData(float duration, float intensity = 0.0f)
        {
            Duration = duration;
            Intensity = intensity;
        }
    }
}
