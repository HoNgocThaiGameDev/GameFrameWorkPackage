using UnityEngine;

namespace NebulaSoft
{
    public static class DuoTypesExtensions
    {
        public static int Range(this DuoInt duoInt)
        {
            return Random.Range(duoInt.firstValue, duoInt.secondValue);
        }

        public static float Range(this DuoFloat duoFloat)
        {
            return Random.Range(duoFloat.firstValue, duoFloat.secondValue);
        }
    }
}
