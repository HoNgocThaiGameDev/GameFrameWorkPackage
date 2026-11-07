namespace NebulaSoft
{
    [System.Serializable]
    public class DuoDouble
    {
        public double firstValue;
        public double secondValue;
        private static System.Random random;

        public DuoDouble(double firstValue, double secondValue)
        {
            this.firstValue = firstValue;
            this.secondValue = secondValue;
        }

        public DuoDouble(double value)
        {
            this.firstValue = value;
            this.secondValue = value;
        }

        public static DuoDouble One => new DuoDouble(1);
        public static DuoDouble Zero => new DuoDouble(0);

        public double Random()
        {
            if (random == null)
            {
                random = new System.Random();
            }

            return random.NextDouble() * (this.secondValue - this.firstValue) + this.firstValue;
        }

        public double Clamp(double value)
        {
            if (value < firstValue)
            {
                return firstValue;
            }
            else if (value > secondValue)
            {
                return secondValue;
            }
            else
            {
                return value;
            }
        }

        public static DuoDouble operator *(DuoDouble a, DuoDouble b) => new DuoDouble(a.firstValue * b.firstValue, a.secondValue * b.secondValue);
        public static DuoDouble operator /(DuoDouble a, DuoDouble b)
        {
            if ((b.firstValue == 0) || (b.secondValue == 0))
            {
                throw new System.DivideByZeroException();
            }

            return new DuoDouble(a.firstValue / b.firstValue, a.secondValue / b.secondValue);
        }

        public override string ToString()
        {
            return "(" + FormatValue(firstValue) + ", " + FormatValue(secondValue) + ")";
        }

        private string FormatValue(double value)
        {
            return value.ToString("0.0").Replace(',', '.');
        }
    }
}