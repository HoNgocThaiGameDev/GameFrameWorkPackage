using UnityEngine;

namespace NebulaSoft
{
    [System.Serializable]
    public class DuoVector3
    {
        public Vector3 firstValue;
        public Vector3 secondValue;

        public DuoVector3(Vector3 firstValue, Vector3 secondValue)
        {
            this.firstValue = firstValue;
            this.secondValue = secondValue;
        }

        public DuoVector3(Vector3 value)
        {
            this.firstValue = value;
            this.secondValue = value;
        }

        public static DuoVector3 One => new DuoVector3(Vector3.one);
        public static DuoVector3 Zero => new DuoVector3(Vector3.zero);

        public static DuoVector3 operator *(DuoVector3 a, DuoVector3 b) => new DuoVector3(new Vector3(a.firstValue.x * b.firstValue.x, a.firstValue.y * b.firstValue.y, a.firstValue.z * b.firstValue.z), new Vector3(a.secondValue.x * b.secondValue.x, a.secondValue.y * b.secondValue.y, a.secondValue.z * b.secondValue.z));
        public static DuoVector3 operator /(DuoVector3 a, DuoVector3 b)
        {
            if ((b.firstValue.x == 0) || (b.firstValue.y == 0) || (b.firstValue.z == 0) || (b.secondValue.x == 0) || (b.secondValue.y == 0) || (b.secondValue.z == 0))
            {
                throw new System.DivideByZeroException();
            }

            return new DuoVector3(new Vector3(a.firstValue.x / b.firstValue.x, a.firstValue.y / b.firstValue.y, a.firstValue.z / b.firstValue.z), new Vector3(a.secondValue.x / b.secondValue.x, a.secondValue.y / b.secondValue.y, a.secondValue.z / b.secondValue.z));
        }

        public Vector3 Random()
        {
            return new Vector3(UnityEngine.Random.Range(firstValue.x, secondValue.x), UnityEngine.Random.Range(firstValue.y, secondValue.y), UnityEngine.Random.Range(firstValue.z, secondValue.z));
        }

        public override string ToString()
        {
            return "[" + FormatValue(firstValue) + ", " + FormatValue(secondValue) + "]";
        }

        private string FormatValue(float value)
        {
            return value.ToString("0.0").Replace(',', '.');
        }

        private string FormatValue(Vector3 value)
        {
            return "(" + FormatValue(value.x) + ", " + FormatValue(value.y) + ", " + FormatValue(value.z) + ")";
        }
    }
}