using UnityEngine;
using System.Runtime.CompilerServices;

namespace NebulaSoft
{
    [System.Serializable]
    public class DuoFloat
    {
        public float firstValue;
        public float secondValue;

        public float this[int i] => i == 0 ? firstValue : secondValue;

        public DuoFloat(float firstValue, float secondValue)
        {
            this.firstValue = firstValue;
            this.secondValue = secondValue;
        }
        public DuoFloat(float value)
        {
            this.firstValue = value;
            this.secondValue = value;
        }

        public static DuoFloat One => new DuoFloat(1);
        public static DuoFloat Zero => new DuoFloat(0);
        public static DuoFloat ZeroOne => new DuoFloat(0, 1);
        public static DuoFloat OneZero => new DuoFloat(1, 0);
        public static DuoFloat MinusOneOne => new DuoFloat(-1, 1);

        public static DuoFloat operator -(DuoFloat value) => new DuoFloat(-value.x, -value.y);

        public float Random()
        {
            return UnityEngine.Random.Range(firstValue, secondValue);
        }

        public float Lerp(float t)
        {
            return Mathf.Lerp(firstValue, secondValue, t);
        }

        public static DuoFloat Lerp(DuoFloat a, DuoFloat b, float t)
        {
            return a + (b - a) * Mathf.Clamp01(t);
        }

        public float Clamp(float value)
        {
            return Mathf.Clamp(value, firstValue, secondValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Remap(float value, DuoFloat inMinMax, DuoFloat outMinMax)
        {
            return outMinMax.firstValue + (value - inMinMax.firstValue) * (outMinMax.secondValue - outMinMax.firstValue) / (inMinMax.secondValue - inMinMax.firstValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DuoFloat Remap(DuoFloat value, DuoFloat inMin, DuoFloat inMax, DuoFloat outMin, DuoFloat outMax)
        {
            return new DuoFloat(
                Remap(value.x, new DuoFloat(inMin.x, inMax.x), new DuoFloat(outMin.x, outMax.x)),
                Remap(value.y, new DuoFloat(inMin.y, inMax.y), new DuoFloat(outMin.y, outMax.y))
            );
        }

        public static implicit operator Vector2(DuoFloat value) => value.xy;

        public static implicit operator DuoFloat(Vector2 vec) => new DuoFloat(vec.x, vec.y);
        public static implicit operator DuoFloat(float value) => new DuoFloat(value);
        public static implicit operator DuoFloat(int value) => new DuoFloat(value);

        public static DuoFloat operator +(DuoFloat a, DuoFloat b) => new DuoFloat(a.firstValue + b.firstValue, a.secondValue + b.secondValue);
        public static DuoFloat operator -(DuoFloat a, DuoFloat b) => new DuoFloat(a.firstValue - b.firstValue, a.secondValue - b.secondValue);
        public static DuoFloat operator *(DuoFloat a, DuoFloat b) => new DuoFloat(a.firstValue * b.firstValue, a.secondValue * b.secondValue);
        public static DuoFloat operator /(DuoFloat a, DuoFloat b)
        {
            if ((b.firstValue == 0) || (b.secondValue == 0))
            {
                throw new System.DivideByZeroException();
            }

            return new DuoFloat(a.firstValue / b.firstValue, a.secondValue / b.secondValue);
        }

        public static DuoFloat operator *(DuoFloat a, float b) => new DuoFloat(a.firstValue * b, a.secondValue * b);
        public static DuoFloat operator /(DuoFloat a, float b)
        {
            if (b == 0)
            {
                throw new System.DivideByZeroException();
            }

            return new DuoFloat(a.firstValue / b, a.secondValue / b);
        }

        public override string ToString()
        {
            return "(" + FormatValue(firstValue) + ", " + FormatValue(secondValue) + ")";
        }

        private string FormatValue(float value)
        {
            return value.ToString("0.0").Replace(',', '.');
        }

        public float Max => firstValue > secondValue ? firstValue : secondValue;
        public float Min => firstValue < secondValue ? firstValue : secondValue;

        public float x => firstValue;
        public float y => secondValue;

        public Vector2 xy => new Vector2(firstValue, secondValue);
        public Vector2 yx => new Vector2(secondValue, firstValue);
        public Vector3 x0y => new Vector3(firstValue, 0, secondValue);
        public Vector3 y0x => new Vector3(secondValue, 0, firstValue);

        public float r => UnityEngine.Random.Range(firstValue, secondValue);
        public Vector2 rr => new Vector2(r, r);
        public Vector3 rrr => new Vector3(r, r, r);
        public Vector3 r0r => new Vector3(r, 0, r);

        public static DuoFloat XZ(Vector3 value) => new DuoFloat(value.x, value.z);
    }
}