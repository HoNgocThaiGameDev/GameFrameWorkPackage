using NebulaSoft.Analytics;
using UnityEngine;

namespace NebulaSoft
{
    public static partial class AnalyticsController
    {
        private static AnalyticsService service;
        private static AnalyticsEvents events;
        public static IAnalyticsService Service => GetService();
        public static AnalyticsEvents Events => events ?? (events = new AnalyticsEvents(GetService()));
        public static AnalyticsDiagnostics Diagnostics => service != null ? service.Diagnostics : default;

        internal static IAnalyticsService ConfigureService(int pendingCapacity)
        {
            var active = GetService();
            active.ConfigureQueueCapacity(pendingCapacity);
            return active;
        }

        private static AnalyticsService GetService() => service ?? (service = new AnalyticsService(onError: Debug.LogException));

        public static bool Track(AnalyticsEvent value) => GetService().Track(value);
        public static void SetCollectionEnabled(bool enabled) => GetService().SetCollectionEnabled(enabled);
        public static void SetUserId(string userId) => GetService().SetUserId(userId);
        public static void SetUserProperty(string name, string value) => GetService().SetUserProperty(name, value);

        public static void Shutdown()
        {
            service?.Dispose();
            service = null;
            events = null;
        }

        internal static void Shutdown(IAnalyticsService owner)
        {
            if (ReferenceEquals(service, owner)) Shutdown();
        }
    }
}
