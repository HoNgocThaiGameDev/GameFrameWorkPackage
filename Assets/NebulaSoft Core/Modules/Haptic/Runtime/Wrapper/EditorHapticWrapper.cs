using UnityEngine;

namespace NebulaSoft
{
    public sealed class EditorHapticWrapper : BaseHapticWrapper
    {
        public override void Init()
        {
            Log("Module is Initialized!");
        }

        public override void Play(float duration = 0.3f, float intensity = 1.0f)
        {
            Log(string.Format("Play method invoked (Duration: {0}; Intensity: {1})!", duration, intensity));
        }

        public override void Play(string patternID)
        {
            Log(string.Format("Play method invoked (PatternID: {0})!", patternID));
        }

        public override void RegisterPattern(HapticPattern pattern)
        {
            Log(string.Format("Pattern with ID: {0} is registered!", pattern.ID));
        }
    }
}
