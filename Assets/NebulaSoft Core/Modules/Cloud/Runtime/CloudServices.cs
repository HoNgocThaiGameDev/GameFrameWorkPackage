using System;
using System.Threading;
using System.Threading.Tasks;

namespace NebulaSoft.Cloud
{
    /// <summary>Main-thread composition root. Initialization is shared and bounded; native resources belong to the adapter.</summary>
    public static class CloudServices
    {
        private static CancellationTokenSource lifetime;
        private static Task<bool> initialization;
        private static bool ready;
        public static ICloudBackend Backend { get; private set; }
        public static bool IsReady => ready;
        public static Exception LastInitializationError { get; private set; }

        public static void Configure(ICloudBackend backend)
        {
            if (backend == null) throw new ArgumentNullException(nameof(backend));
            if (ReferenceEquals(Backend, backend)) return;
            Shutdown();
            Backend = backend;
            lifetime = new CancellationTokenSource();
        }

        public static Task<bool> EnsureInitializedAsync(TimeSpan? timeout = null)
        {
            if (Backend == null) return Task.FromResult(false);
            if (ready) return Task.FromResult(true);
            if (initialization != null && !initialization.IsCompleted) return initialization;
            var duration = timeout ?? TimeSpan.FromSeconds(30);
            if (duration <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(timeout));
            var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            initialization = completion.Task;
            _ = InitializeAsync(Backend, lifetime, duration, completion);
            return completion.Task;
        }

        private static async Task InitializeAsync(ICloudBackend backend, CancellationTokenSource owner,
            TimeSpan timeout, TaskCompletionSource<bool> completion)
        {
            using (var attempt = CancellationTokenSource.CreateLinkedTokenSource(owner.Token))
            {
                try
                {
                    attempt.CancelAfter(timeout);
                    var cancelled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                    using (attempt.Token.Register(() => cancelled.TrySetResult(true)))
                    {
                        var work = backend.InitializeAsync(attempt.Token);
                        if (await Task.WhenAny(work, cancelled.Task) != work)
                        {
                            _ = work.ContinueWith(task => { var observed = task.Exception; },
                                CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);
                            throw new CloudException(CloudError.Timeout, "Cloud initialization was cancelled or timed out.");
                        }
                        await work;
                    }
                    attempt.Token.ThrowIfCancellationRequested();
                    if (!ReferenceEquals(Backend, backend) || lifetime != owner)
                    {
                        completion.TrySetResult(false);
                        return;
                    }
                    if (backend.Auth == null || backend.Documents == null)
                        throw new CloudException(CloudError.Unavailable, "Cloud backend did not provide Auth and Documents.");
                    LastInitializationError = null;
                    ready = true;
                    completion.TrySetResult(true);
                }
                catch (Exception exception)
                {
                    if (ReferenceEquals(Backend, backend) && lifetime == owner)
                        LastInitializationError = attempt.IsCancellationRequested
                            ? new CloudException(CloudError.Timeout, "Cloud initialization was cancelled or timed out.", exception)
                            : exception;
                    completion.TrySetResult(false);
                }
            }
        }

        public static void Shutdown()
        {
            var previous = Backend;
            Backend = null;
            ready = false;
            initialization = null;
            LastInitializationError = null;
            lifetime?.Cancel();
            lifetime?.Dispose();
            lifetime = null;
            previous?.Dispose();
        }
    }
}
