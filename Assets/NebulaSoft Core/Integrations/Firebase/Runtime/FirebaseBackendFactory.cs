using System;
using NebulaSoft.Analytics;
using NebulaSoft.Cloud;

namespace NebulaSoft.Integrations.Firebase
{
    public static class FirebaseBackendFactory
    {
        public const string AnalyticsSetupMessage = "Import FirebaseAnalytics.unitypackage, then enable FIREBASE_ANALYTICS for the build target.";

        public static bool IsCloudEnabled
        {
            get
            {
#if FIREBASE
                return true;
#else
                return false;
#endif
            }
        }

        public static bool IsAnalyticsEnabled
        {
            get
            {
#if FIREBASE_ANALYTICS
                return true;
#else
                return false;
#endif
            }
        }

        public static ICloudBackend Create()
        {
#if FIREBASE
            return new FirebaseCloudBackend();
#else
            return null;
#endif
        }

        public static IAnalyticsProvider CreateAnalytics()
        {
#if FIREBASE_ANALYTICS
            return new FirebaseProvider();
#else
            throw new InvalidOperationException(AnalyticsSetupMessage);
#endif
        }
    }
}
