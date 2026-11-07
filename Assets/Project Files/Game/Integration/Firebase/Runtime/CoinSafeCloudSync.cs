using System.Threading.Tasks;
using UnityEngine;

namespace NebulaSoft
{
    public static class CoinSafeCloudSync
    {
        private const float LogoutFlushTimeoutSeconds = 3f;

        private static bool syncRequested;
        private static bool syncRunning;
        private static int sessionGeneration;
        private static Runner runner;

        public static float LogoutFlushTimeout => LogoutFlushTimeoutSeconds;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            sessionGeneration++;
            syncRequested = false;
            syncRunning = false;
            runner = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            EnsureRunner();
        }

        public static void RequestCheckpointSync()
        {
            if (!Application.isPlaying || FirebaseAuthHandler.IsCurrentUserAnonymous)
                return;

            EnsureRunner();
            syncRequested = true;

            if (!syncRunning)
                _ = ProcessSyncQueueAsync(sessionGeneration);
        }

        public static async Task<bool> FlushActiveFacebookAsync(float timeoutSeconds = LogoutFlushTimeoutSeconds)
        {
            int runGeneration = sessionGeneration;
            CoinSafeSyncSnapshot snapshot;
            if (!CoinSafeProgress.TryGetActiveFacebookSnapshot(out snapshot)
                || !CoinSafeProgress.IsFacebookSnapshotDirty(snapshot.Uid, snapshot.Revision))
            {
                return true;
            }

            RequestCheckpointSync();
            float deadline = Time.realtimeSinceStartup + Mathf.Max(0.1f, timeoutSeconds);
            while (runGeneration == sessionGeneration
                && syncRunning
                && Time.realtimeSinceStartup < deadline)
            {
                await Task.Delay(50);
            }

            return runGeneration == sessionGeneration
                && !CoinSafeProgress.TryGetDirtyFacebookSnapshot(snapshot.Uid, out _);
        }

        public static async Task InitializeRestoredSessionAsync()
        {
            int runGeneration = sessionGeneration;
            if (!SaveController.IsSaveLoaded || !await FirebaseAuthHandler.EnsureSignedInAsync()
                || runGeneration != sessionGeneration)
                return;

            if (FirebaseAuthHandler.IsCurrentUserAnonymous)
            {
                return;
            }

            string uid = FirebaseAuthHandler.CurrentUserId;
            if (string.IsNullOrEmpty(uid))
                return;

            if (CoinSafeProgress.HasPendingFacebookResolution)
            {
                if (!string.Equals(CoinSafeProgress.PendingFacebookUid, uid, System.StringComparison.Ordinal))
                {
                    CoinSafeProgress.BeginFacebookResolution(uid);
                }

                return;
            }

            // Keep the current local value active and require an explicit Local/Cloud choice instead of
            // silently replacing it with the downloaded value.
            if (!CoinSafeProgress.IsActiveFacebookOwner(uid))
            {
                CoinSafeProgress.BeginFacebookResolution(uid);
                return;
            }

            CoinSafeSyncSnapshot dirtySnapshot;
            if (CoinSafeProgress.TryGetDirtyFacebookSnapshot(uid, out dirtySnapshot))
            {
                CoinSafeProgress.ActivateCachedFacebookOwner(uid);
                RequestCheckpointSync();
            }
        }

        private static async Task ProcessSyncQueueAsync(int runGeneration)
        {
            if (runGeneration != sessionGeneration || syncRunning)
                return;

            syncRunning = true;
            try
            {
                while (runGeneration == sessionGeneration && syncRequested)
                {
                    syncRequested = false;

                    CoinSafeSyncSnapshot snapshot;
                    if (!CoinSafeProgress.TryGetActiveFacebookSnapshot(out snapshot)
                        || !CoinSafeProgress.IsFacebookSnapshotDirty(snapshot.Uid, snapshot.Revision))
                    {
                        continue;
                    }

                    CoinSafeProgress.FlushLocalSave();
                    bool success = await FirebaseProfileHandler.UploadCoinSafeAmountAsync(snapshot.Uid, snapshot.Amount);
                    if (runGeneration != sessionGeneration || !success)
                        return;

                    CoinSafeProgress.MarkFacebookSnapshotSynced(snapshot.Uid, snapshot.Revision);
                    CoinSafeProgress.FlushLocalSave();

                    CoinSafeSyncSnapshot latest;
                    if (CoinSafeProgress.TryGetDirtyFacebookSnapshot(snapshot.Uid, out latest))
                        syncRequested = true;
                }
            }
            finally
            {
                if (runGeneration == sessionGeneration)
                {
                    syncRunning = false;

                    if (syncRequested)
                        _ = ProcessSyncQueueAsync(runGeneration);
                }
            }
        }

        private static void EnsureRunner()
        {
            if (runner != null || !Application.isPlaying)
                return;

            GameObject gameObject = new GameObject("[COIN SAFE CLOUD SYNC]");
            gameObject.hideFlags = HideFlags.HideInHierarchy;
            Object.DontDestroyOnLoad(gameObject);
            runner = gameObject.AddComponent<Runner>();
        }

        private sealed class Runner : MonoBehaviour
        {
            private void Awake()
            {
                if (runner != null && runner != this)
                {
                    Destroy(gameObject);
                    return;
                }

                runner = this;
            }

            private async void Start()
            {
                int runGeneration = sessionGeneration;
                while (runGeneration == sessionGeneration && !SaveController.IsSaveLoaded)
                    await Task.Yield();

                if (runGeneration != sessionGeneration)
                    return;

                await InitializeRestoredSessionAsync();
            }

            private void OnApplicationFocus(bool hasFocus)
            {
                if (!SaveController.IsSaveLoaded)
                    return;

                if (!hasFocus)
                    CoinSafeProgress.FlushLocalSave();

                RequestCheckpointSync();
            }

            private void OnApplicationPause(bool paused)
            {
                if (!SaveController.IsSaveLoaded)
                    return;

                if (paused)
                    CoinSafeProgress.FlushLocalSave();

                RequestCheckpointSync();
            }

            private void OnApplicationQuit()
            {
                CoinSafeProgress.FlushLocalSave();
            }

            private void OnDestroy()
            {
                if (runner == this)
                    runner = null;
            }
        }
    }
}
