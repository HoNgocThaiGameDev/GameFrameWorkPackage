using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NebulaSoft.Analytics
{
    /// <summary>Application-lifetime service. Call on the Unity main thread; the SDK owns transport/offline persistence.</summary>
    public sealed class AnalyticsService : IAnalyticsService, IAnalyticsDiagnostics
    {
        private readonly Queue<AnalyticsEvent> pending = new Queue<AnalyticsEvent>();
        private readonly Dictionary<string, string> userProperties = new Dictionary<string, string>();
        private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        private int capacity;
        private readonly Action<Exception> onError;
        private IAnalyticsProvider provider;
        private Task<bool> initialization;
        private bool disposed, hasUserId;
        private string userId;
        private bool contextReady;
        private long acceptedCount, sentCount, queueOverflowCount, identityDiscardCount;
        private long collectionDiscardCount, shutdownDiscardCount, rejectedCount, sendFailureCount, initializationFailureCount;
        public bool IsReady { get; private set; }
        public bool IsCollectionEnabled { get; private set; }
        public int PendingCount => pending.Count;
        public AnalyticsDiagnostics Diagnostics => new AnalyticsDiagnostics(pending.Count, acceptedCount,
            sentCount, queueOverflowCount, identityDiscardCount, collectionDiscardCount,
            shutdownDiscardCount, rejectedCount, sendFailureCount, Interlocked.Read(ref initializationFailureCount));

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
            _ = InitializeCoreAsync(value, cancellation, completion);
            return completion.Task;
        }

        private async Task InitializeCoreAsync(IAnalyticsProvider candidate, CancellationToken cancellation, TaskCompletionSource<bool> completion)
        {
            using (var linked = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token, cancellation))
            using (linked.Token.Register(() => FailInitialization(completion)))
            {
                try
                {
                    linked.Token.ThrowIfCancellationRequested();
                    await candidate.InitializeAsync(linked.Token);
                    linked.Token.ThrowIfCancellationRequested();
                    if (disposed || initialization != completion.Task) { FailInitialization(completion); return; }
                    candidate.SetCollectionEnabled(IsCollectionEnabled);
                    ApplyContext(candidate);
                    IsReady = true;
                    while (pending.Count > 0)
                    {
                        var next = pending.Dequeue();
                        if (IsCollectionEnabled) Send(next);
                    }
                    completion.TrySetResult(true);
                }
                catch (OperationCanceledException) { FailInitialization(completion); }
                catch (Exception exception) { FailInitialization(completion); Report(exception); }
            }
        }

        private void FailInitialization(TaskCompletionSource<bool> completion)
        {
            if (completion.TrySetResult(false)) Interlocked.Increment(ref initializationFailureCount);
        }

        public bool Track(AnalyticsEvent value)
        {
            if (disposed || !IsCollectionEnabled || value == null)
            {
                rejectedCount++;
                return false;
            }
            if (IsReady)
            {
                if (!Send(value)) return false;
                acceptedCount++;
                return true;
            }
            if (pending.Count == capacity)
            {
                pending.Dequeue();
                queueOverflowCount++;
            }
            pending.Enqueue(value);
            acceptedCount++;
            return true;
        }

        private bool Send(AnalyticsEvent value)
        {
            if (!contextReady) { sendFailureCount++; return false; }
            try
            {
                provider.LogEvent(value);
                sentCount++;
                return true;
            }
            catch (Exception exception) { sendFailureCount++; Report(exception); return false; }
        }

        public void ConfigureQueueCapacity(int pendingCapacity)
        {
            if (pendingCapacity < 1) throw new ArgumentOutOfRangeException(nameof(pendingCapacity));
            if (disposed) return;
            capacity = pendingCapacity;
            while (pending.Count > capacity)
            {
                pending.Dequeue();
                queueOverflowCount++;
            }
        }

        public void SetCollectionEnabled(bool enabled)
        {
            if (disposed) return;
            IsCollectionEnabled = enabled;
            if (!enabled)
            {
                collectionDiscardCount += pending.Count;
                pending.Clear();
            }
            if (IsReady) Safely(() => provider.SetCollectionEnabled(enabled));
        }

        public void SetUserId(string value)
        {
            if (disposed) return;
            if (!string.Equals(userId, value, StringComparison.Ordinal))
            {
                // Account boundaries discard buffered events rather than attributing them to the next user.
                identityDiscardCount += pending.Count;
                pending.Clear();
                foreach (string name in new List<string>(userProperties.Keys)) userProperties[name] = null;
            }
            userId = value;
            hasUserId = true;
            if (IsReady) RefreshContext();
        }

        public void SetUserProperty(string name, string value)
        {
            if (disposed) return;
            AnalyticsEvent.ValidateName(name, 24);
            if (value != null && value.Length > 36) throw new ArgumentException("User property values support at most 36 characters.", nameof(value));
            userProperties[name] = value;
            if (IsReady) RefreshContext();
        }

        private void ApplyContext(IAnalyticsProvider target)
        {
            contextReady = false;
            if (hasUserId) target.SetUserId(userId);
            foreach (var pair in userProperties) target.SetUserProperty(pair.Key, pair.Value);
            contextReady = true;
        }

        private void RefreshContext()
        {
            try { ApplyContext(provider); }
            catch (Exception exception) { Report(exception); }
        }

        private void Safely(Action action) { try { action(); } catch (Exception exception) { Report(exception); } }
        private void Report(Exception exception) { try { onError?.Invoke(exception); } catch { /* Diagnostics cannot interrupt gameplay. */ } }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            IsReady = false;
            lifetime.Cancel();
            shutdownDiscardCount += pending.Count;
            pending.Clear();
            userProperties.Clear();
            if (provider != null) Safely(provider.Dispose);
            lifetime.Dispose();
        }
    }
}
