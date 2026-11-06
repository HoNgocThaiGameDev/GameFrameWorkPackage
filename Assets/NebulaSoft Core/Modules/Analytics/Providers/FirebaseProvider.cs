#if FIREBASE_ANALYTICS
using System;
using System.Threading;
using System.Threading.Tasks;
using Firebase;
using Firebase.Analytics;

namespace NebulaSoft.Analytics
{
    /// <summary>Only this adapter references Firebase. Authentication is not required to initialize Analytics.</summary>
    public sealed class FirebaseProvider : IAnalyticsProvider
    {
        private bool disposed;
        public async Task InitializeAsync(CancellationToken cancellation)
        {
            cancellation.ThrowIfCancellationRequested();
            var status = await FirebaseApp.CheckAndFixDependenciesAsync();
            cancellation.ThrowIfCancellationRequested();
            if (disposed) throw new ObjectDisposedException(nameof(FirebaseProvider));
            if (status != DependencyStatus.Available)
                throw new InvalidOperationException("Firebase dependencies are unavailable: " + status);
        }
        public void SetCollectionEnabled(bool enabled) => FirebaseAnalytics.SetAnalyticsCollectionEnabled(enabled);
        public void SetUserId(string userId) => FirebaseAnalytics.SetUserId(userId);
        public void SetUserProperty(string name, string value) => FirebaseAnalytics.SetUserProperty(name, value);
        public void LogEvent(AnalyticsEvent value)
        {
            if (disposed) return;
            var parameters = new Parameter[value.Parameters.Count];
            int index = 0;
            foreach (var pair in value.Parameters)
            {
                if (pair.Value is long integer) parameters[index++] = new Parameter(pair.Key, integer);
                else if (pair.Value is double number) parameters[index++] = new Parameter(pair.Key, number);
                else parameters[index++] = new Parameter(pair.Key, (string)pair.Value);
            }
            FirebaseAnalytics.LogEvent(value.Name, parameters);
        }
        public void Dispose() { disposed = true; }
    }
}
#endif
