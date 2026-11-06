using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace NebulaSoft.Analytics
{
    public sealed class ConsoleProvider : IAnalyticsProvider
    {
        public Task InitializeAsync(CancellationToken cancellation) { cancellation.ThrowIfCancellationRequested(); return Task.CompletedTask; }
        public void SetCollectionEnabled(bool enabled) { }
        public void SetUserId(string userId) { }
        public void SetUserProperty(string name, string value) { }
        public void LogEvent(AnalyticsEvent value)
        {
            var message = new StringBuilder("[Analytics] ").Append(value.Name);
            foreach (var pair in value.Parameters) message.Append(' ').Append(pair.Key).Append('=').Append(pair.Value);
            Debug.Log(message.ToString());
        }
        public void Dispose() { }
    }
}
