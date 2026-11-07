namespace NebulaSoft
{
    public static class Core
    {
        public static bool IsMonetizationActive()
        {
#if MODULE_MONETIZATION
            return Monetization.IsActive;
#else
            return false;
#endif
        }
    }
}
