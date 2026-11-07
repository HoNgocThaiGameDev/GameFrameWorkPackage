using System;

namespace NebulaSoft
{
    [StaticUnload]
    public static partial class AnalyticsModules
    {
        private static BaseAnalyticsModule[] analyticsModules;

        public static void Init()
        {
            analyticsModules = GetAnalyticsModules();
        }

        public static void OnModuleInitialized<T>()
        {
            Type type = typeof(T);

            if(!analyticsModules.IsNullOrEmpty())
            {
                foreach (BaseAnalyticsModule analyticsModule in analyticsModules)
                {
                    if (analyticsModule.GetType() == type)
                        analyticsModule.Init();
                }
            }
        }

        private static void UnloadStatic()
        {
            analyticsModules = null;
        }
    }
}
