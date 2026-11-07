using UnityEngine;

namespace NebulaSoft
{
    [System.Serializable]
    public class DuoColor
    {
        public Color32 firstValue;
        public Color32 secondValue;

        public DuoColor(Color32 first, Color32 second)
        {
            firstValue = first;
            secondValue = second;
        }

        public DuoColor(Color32 color)
        {
            firstValue = color;
            secondValue = color;
        }

        public Color32 Lerp(float state)
        {
            return Color32.Lerp(firstValue, secondValue, state);
        }

        public Color32 RandomBetween()
        {
            return Color32.Lerp(firstValue, secondValue, UnityEngine.Random.value);
        }
    }
}