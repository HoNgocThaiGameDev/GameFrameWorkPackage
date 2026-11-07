namespace NebulaSoft
{
    public abstract class TweenCaseFunction<TBaseObject, TValue> : TweenCase
    {
        public TBaseObject tweenObject;

        public TValue startValue;
        public TValue resultValue;

        public TweenCaseFunction(TBaseObject tweenObject, TValue resultValue)
        {
            this.tweenObject = tweenObject;
            this.resultValue = resultValue;
        }
    }
}
