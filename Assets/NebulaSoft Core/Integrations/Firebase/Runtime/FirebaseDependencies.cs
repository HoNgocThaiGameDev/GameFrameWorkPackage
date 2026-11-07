#if FIREBASE || FIREBASE_ANALYTICS
using System;
using System.Threading;
using System.Threading.Tasks;
using Firebase;
using NebulaSoft.Cloud;
using UnityEngine;

namespace NebulaSoft.Integrations.Firebase
{
    internal static class FirebaseDependencies
    {
        private static Task check;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => check = null;

        internal static async Task EnsureAsync(CancellationToken cancellation)
        {
            cancellation.ThrowIfCancellationRequested();
            // Cache success and share in-flight native dependency work across Analytics/Auth/Firestore.
            if (check == null || check.IsCanceled || check.IsFaulted) check = CheckAsync();
            var active = check;
            var cancelled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            using (cancellation.Register(() => cancelled.TrySetResult(true)))
            {
                if (await Task.WhenAny(active, cancelled.Task) != active)
                {
                    _ = active.ContinueWith(t => { var observed = t.Exception; },
                        CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);
                    cancellation.ThrowIfCancellationRequested();
                }
                await active;
                cancellation.ThrowIfCancellationRequested();
            }
        }

        private static async Task CheckAsync()
        {
            var status = await FirebaseApp.CheckAndFixDependenciesAsync();
            if (status != DependencyStatus.Available)
                throw new CloudException(CloudError.Unavailable, "Firebase dependencies are unavailable: " + status);
        }
    }
}
#endif
