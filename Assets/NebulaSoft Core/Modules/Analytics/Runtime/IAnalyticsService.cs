using System;
using System.Threading;
using System.Threading.Tasks;

namespace NebulaSoft.Analytics
{
    public interface IAnalyticsService : IDisposable
    {
        bool IsReady { get; }
        bool IsCollectionEnabled { get; }
        int PendingCount { get; }
        Task<bool> InitializeAsync(IAnalyticsProvider provider, CancellationToken cancellation = default);
        bool Track(AnalyticsEvent value);
        void SetCollectionEnabled(bool enabled);
        void SetUserId(string userId);
        void SetUserProperty(string name, string value);
    }
}
