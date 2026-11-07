using UnityEngine;

namespace NebulaSoft
{
    [System.Serializable]
    public class DuoInt
    {
        public int firstValue;
        public int secondValue;

        public DuoInt(int firstValue, int secondValue)
        {
            this.firstValue = firstValue;
            this.secondValue = secondValue;
        }

        public DuoInt(int value)
        {
            this.firstValue = value;
            this.secondValue = value;
        }

        public static DuoInt One => new DuoInt(1);
        public static DuoInt Zero => new DuoInt(0);

        public int Random()
        {
            return UnityEngine.Random.Range(firstValue, secondValue + 1); // Because second parameter is exclusive. Withot + 1 method Random.Range(1,2) will always return 1
        }

        public int Clamp(int value)
        {
            return Mathf.Clamp(value, firstValue, secondValue);
        }

        public int Lerp(float t)
        {
            return Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(firstValue, secondValue, t)), firstValue, secondValue);
        }

        public static implicit operator Vector2Int(DuoInt value) => new Vector2Int(value.firstValue, value.secondValue);
        public static explicit operator DuoInt(Vector2Int vec) => new DuoInt(vec.x, vec.y);

        public static DuoInt operator *(DuoInt a, DuoInt b) => new DuoInt(a.firstValue * b.firstValue, a.secondValue * b.secondValue);
        public static DuoInt operator /(DuoInt a, DuoInt b)
        {
            if ((b.firstValue == 0) || (b.secondValue == 0))
            {
                throw new System.DivideByZeroException();
            }

            return new DuoInt(a.firstValue / b.firstValue, a.secondValue / b.secondValue);
        }

        public static DuoInt operator *(DuoInt a, float b) => new DuoInt((int)(a.firstValue * b), (int)(a.secondValue * b));
        public static DuoInt operator /(DuoInt a, float b)
        {
            if (b == 0)
            {
                throw new System.DivideByZeroException();
            }

            return new DuoInt((int)(a.firstValue / b), (int)(a.secondValue / b));
        }

        public DuoFloat ToDuoFloat()
        {
            return new DuoFloat(firstValue, secondValue);
        }

        public override string ToString()
        {
            return "(" + firstValue + ", " + secondValue + ")";
        }
    }
}