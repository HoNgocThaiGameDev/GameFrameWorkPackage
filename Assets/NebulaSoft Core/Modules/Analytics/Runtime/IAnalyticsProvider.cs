using System;
using System.Threading;
using System.Threading.Tasks;

namespace NebulaSoft.Analytics
{
    public interface IAnalyticsProvider : IDisposable
    {
        Task InitializeAsync(CancellationToken cancellation);
        void SetCollectionEnabled(bool enabled);
        void SetUserId(string userId);
        void SetUserProperty(string name, string value);
        void LogEvent(AnalyticsEvent value);
    }
}
