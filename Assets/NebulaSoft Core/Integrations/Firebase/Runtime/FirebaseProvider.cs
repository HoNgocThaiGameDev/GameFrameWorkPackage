#if FIREBASE_ANALYTICS
using System;
using System.Threading;
using System.Threading.Tasks;
using Firebase.Analytics;
using NebulaSoft.Integrations.Firebase;

namespace NebulaSoft.Analytics
{
    /// <summary>Analytics transport. Firebase Authentication is not required.</summary>
    internal sealed class FirebaseProvider : IAnalyticsProvider
    {
        private bool disposed;
        public async Task InitializeAsync(CancellationToken cancellation)
        {
            cancellation.ThrowIfCancellationRequested();
            await FirebaseDependencies.EnsureAsync(cancellation);
            cancellation.ThrowIfCancellationRequested();
            if (disposed) throw new ObjectDisposedException(nameof(FirebaseProvider));
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
