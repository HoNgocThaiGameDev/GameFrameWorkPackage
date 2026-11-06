using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NebulaSoft.Analytics
{
    /// <summary>Application-lifetime service. Call on the Unity main thread; the SDK owns transport/offline persistence.</summary>
    public sealed class AnalyticsService : IAnalyticsService
    {
        private readonly Queue<AnalyticsEvent> pending = new Queue<AnalyticsEvent>();
        private readonly Dictionary<string, string> userProperties = new Dictionary<string, string>();
        private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        private readonly int capacity;
        private readonly Action<Exception> onError;
        private IAnalyticsProvider provider;
        private Task<bool> initialization;
        private bool disposed, hasUserId;
        private string userId;
        public bool IsReady { get; private set; }
        public bool IsCollectionEnabled { get; private set; }
        public int PendingCount => pending.Count;

        public AnalyticsService(int pendingCapacity = 128, bool collectionEnabled = true, Action<Exception> onError = null)
        {
            if (pendingCapacity < 1) throw new ArgumentOutOfRangeException(nameof(pendingCapacity));
            capacity = pendingCapacity;
            IsCollectionEnabled = collectionEnabled;
            this.onError = onError;
        }

        public Task<bool> InitializeAsync(IAnalyticsProvider value, CancellationToken cancellation = default)
        {
            if (disposed) return Task.FromResult(false);
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (IsReady || initialization != null && !initialization.IsCompleted) return initialization;
            if (provider != null && !ReferenceEquals(provider, value)) Safely(provider.Dispose);
            provider = value;
            var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            initialization = completion.Task;
            _ = InitializeCoreAsync(cancellation, completion);
            return completion.Task;
        }

        private async Task InitializeCoreAsync(CancellationToken cancellation, TaskCompletionSource<bool> completion)
        {
            using (var linked = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token, cancellation))
            {
                try
                {
                    await provider.InitializeAsync(linked.Token);
                    linked.Token.ThrowIfCancellationRequested();
                    if (disposed) { completion.TrySetResult(false); return; }
                    provider.SetCollectionEnabled(IsCollectionEnabled);
                    if (hasUserId) provider.SetUserId(userId);
                    foreach (var pair in userProperties) provider.SetUserProperty(pair.Key, pair.Value);
                    IsReady = true;
                    while (pending.Count > 0)
                    {
                        var next = pending.Dequeue();
                        if (IsCollectionEnabled) Send(next);
                    }
                    completion.TrySetResult(true);
                }
                catch (OperationCanceledException) { completion.TrySetResult(false); }
                catch (Exception exception) { Report(exception); completion.TrySetResult(false); }
            }
        }

        public bool Track(AnalyticsEvent value)
        {
            if (disposed || !IsCollectionEnabled || value == null) return false;
            if (IsReady) return Send(value);
            if (pending.Count == capacity) pending.Dequeue();
            pending.Enqueue(value);
            return true;
        }

        private bool Send(AnalyticsEvent value)
        {
            try { provider.LogEvent(value); return true; }
            catch (Exception exception) { Report(exception); return false; }
        }

        public void SetCollectionEnabled(bool enabled)
        {
            if (disposed) return;
            IsCollectionEnabled = enabled;
            if (!enabled) pending.Clear();
            if (IsReady) Safely(() => provider.SetCollectionEnabled(enabled));
        }

        public void SetUserId(string value)
        {
            if (disposed) return;
            userId = value;
            hasUserId = true;
            if (IsReady) Safely(() => provider.SetUserId(value));
        }

        public void SetUserProperty(string name, string value)
        {
            if (disposed) return;
            AnalyticsEvent.ValidateName(name, 24);
            if (value != null && value.Length > 36) throw new ArgumentException("User property values support at most 36 characters.", nameof(value));
            userProperties[name] = value;
            if (IsReady) Safely(() => provider.SetUserProperty(name, value));
        }

        private void Safely(Action action) { try { action(); } catch (Exception exception) { Report(exception); } }
        private void Report(Exception exception) { try { onError?.Invoke(exception); } catch { /* Diagnostics cannot interrupt gameplay. */ } }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            IsReady = false;
            lifetime.Cancel();
            pending.Clear();
            userProperties.Clear();
            if (provider != null) Safely(provider.Dispose);
            lifetime.Dispose();
        }
    }
}
