using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NebulaSoft.Cloud;
using NebulaSoft.Integrations.Firebase;
using NUnit.Framework;
using UnityEngine;

namespace NebulaSoft.Tests
{
    public sealed class CloudModuleTests
    {
        private FakeBackend backend;
        private string previousCustomId;
        [SetUp]
        public void SetUp()
        {
            CloudServices.Shutdown();
            previousCustomId = User.CustomId;
            backend = new FakeBackend();
            CloudServices.Configure(backend);
        }
        [TearDown]
        public void TearDown()
        {
            CloudServices.Shutdown();
            User.SetCustomId(previousCustomId);
        }

        [Test]
        public async Task Initialization_IsSharedAcrossCallersAndRetryableAfterFailure()
        {
            backend.FailNextInitialization = true;
            Assert.That(await CloudServices.EnsureInitializedAsync(), Is.False);
            Assert.That(CloudServices.LastInitializationError, Is.TypeOf<CloudException>());
            backend.Gate = new TaskCompletionSource<bool>();
            var first = CloudServices.EnsureInitializedAsync();
            Assert.That(CloudServices.EnsureInitializedAsync(), Is.SameAs(first));
            backend.Gate.SetResult(true);
            Assert.That(await first, Is.True);
            Assert.That(backend.InitializeCount, Is.EqualTo(2));
            Assert.That(CloudServices.LastInitializationError, Is.Null);
        }

        [Test]
        public async Task Initialization_TimeoutUnblocksCallerAndLateAttemptCannotPublishReady()
        {
            var abandoned = backend.Gate = new TaskCompletionSource<bool>();
            Assert.That(await CloudServices.EnsureInitializedAsync(TimeSpan.FromMilliseconds(50)), Is.False);
            Assert.That(CloudServices.IsReady, Is.False);
            Assert.That(((CloudException)CloudServices.LastInitializationError).Error, Is.EqualTo(CloudError.Timeout));
            backend.Gate = new TaskCompletionSource<bool>();
            var retry = CloudServices.EnsureInitializedAsync();
            abandoned.SetResult(true);
            await Task.Yield();
            Assert.That(CloudServices.IsReady, Is.False);
            backend.Gate.SetResult(true);
            Assert.That(await retry, Is.True);
        }

        [Test]
        public async Task Shutdown_UnblocksInitializationAndReleasesOwnedAdapterOnce()
        {
            backend.Gate = new TaskCompletionSource<bool>();
            var initialization = CloudServices.EnsureInitializedAsync();
            CloudServices.Shutdown();
            CloudServices.Shutdown();
            Assert.That(await initialization, Is.False);
            Assert.That(backend.DisposeCount, Is.EqualTo(1));
            backend.Gate.SetResult(true);
            await Task.Yield();
            Assert.That(CloudServices.Backend, Is.Null);
            Assert.That(CloudServices.IsReady, Is.False);
        }

        [Test]
        public async Task BackendReplacement_IgnoresPreviousSessionCompletion()
        {
            backend.Gate = new TaskCompletionSource<bool>();
            var old = CloudServices.EnsureInitializedAsync();
            var replacement = new FakeBackend();
            CloudServices.Configure(replacement);
            Assert.That(await old, Is.False);
            Assert.That(await CloudServices.EnsureInitializedAsync(), Is.True);
            backend.Gate.SetResult(true);
            await Task.Yield();
            Assert.That(CloudServices.Backend, Is.SameAs(replacement));
            Assert.That(CloudServices.IsReady, Is.True);
            Assert.That(backend.DisposeCount, Is.EqualTo(1));
        }

        [Test]
        public void Snapshot_DetachesNestedDataAndDistinguishesNullFromMissing()
        {
            var nested = new Dictionary<string, object> { ["energy"] = 8L };
            var bytes = new byte[] { 1, 2 };
            var snapshot = new CloudDocumentSnapshot(true, new Dictionary<string, object>
            {
                ["extra"] = nested, ["bytes"] = bytes, ["cleared"] = null,
                ["items"] = new List<object> { nested }
            });
            nested["energy"] = 99L;
            bytes[0] = 99;
            Assert.That(snapshot.TryGetValue<Dictionary<string, object>>("extra", out var copy), Is.True);
            Assert.That(copy["energy"], Is.EqualTo(8L));
            copy["energy"] = 100L;
            Assert.That(((Dictionary<string, object>)snapshot.ToDictionary()["extra"])["energy"], Is.EqualTo(8L));
            Assert.That(((byte[])snapshot.ToDictionary()["bytes"])[0], Is.EqualTo(1));
            Assert.That(snapshot.TryGetValue<object>("cleared", out _), Is.True);
            Assert.That(snapshot.TryGetValue<object>("missing", out _), Is.False);
            Assert.That(snapshot.TryGetValue<long>("cleared", out _), Is.False);
        }

        [Test]
        public void Snapshot_RejectsUnsupportedNativeValuesInsteadOfDroppingThem()
        {
            var error = Assert.Throws<CloudException>(() => new CloudDocumentSnapshot(true,
                new Dictionary<string, object> { ["native"] = new object() }));
            Assert.That(error.Error, Is.EqualTo(CloudError.InvalidData));
        }

        [Test]
        public async Task DocumentReference_PreservesPathsReadSourceAndMergeIntent()
        {
            ICloudDocumentStore store = backend.Store;
            var document = store.Collection("Players").Document("uid").Collection("Items").Document("item");
            await document.SetAsync(new Dictionary<string, object> { ["balance"] = 4L }, CloudWriteMode.Merge);
            await document.GetSnapshotAsync(CloudReadSource.Server);
            Assert.That(document.Path, Is.EqualTo("Players/uid/Items/item"));
            Assert.That(backend.Store.LastReadSource, Is.EqualTo(CloudReadSource.Server));
            Assert.That(backend.Store.LastWriteMode, Is.EqualTo(CloudWriteMode.Merge));
            Assert.Throws<ArgumentException>(() => store.Collection("Players").Document("uid/other"));
        }

        [Test]
        public async Task NameClaim_PreservesUnknownFieldsAndExistingSaveVersion()
        {
            backend.Store.Data["Players/uid"] = new Dictionary<string, object>
            {
                ["saveVersion"] = 3L, ["extraGameField"] = "keep", ["coins"] = 99L
            };
            var result = await Claim("Alice", new Dictionary<string, object> { ["saveVersion"] = 3L, ["coins"] = 100L });
            Assert.That(result.Status, Is.EqualTo(PlayerNameClaimStatus.Succeeded));
            Assert.That(backend.Store.Data["Players/uid"]["extraGameField"], Is.EqualTo("keep"));
            Assert.That(backend.Store.Data["Players/uid"]["coins"], Is.EqualTo(100L));
            Assert.That(backend.Store.Data["PlayerNames/" + result.NameKey]["uid"], Is.EqualTo("uid"));
        }

        [Test]
        public async Task NameClaim_TakenNameDoesNotMutatePlayerOrOtherClaim()
        {
            string key = FirebasePlayerNameRegistry.ComputeKey("alice");
            backend.Store.Data["PlayerNames/" + key] = new Dictionary<string, object> { ["uid"] = "other" };
            var result = await Claim("Alice", new Dictionary<string, object> { ["coins"] = 100L });
            Assert.That(result.Status, Is.EqualTo(PlayerNameClaimStatus.Taken));
            Assert.That(backend.Store.Data.ContainsKey("Players/uid"), Is.False);
            Assert.That(backend.Store.Data["PlayerNames/" + key]["uid"], Is.EqualTo("other"));
        }

        [Test]
        public async Task NameClaim_NewerCloudVersionBlocksWriteAndRetainsOriginalDocument()
        {
            backend.Store.Data["Players/uid"] = new Dictionary<string, object> { ["saveVersion"] = 99L, ["extra"] = "keep" };
            var result = await Claim("Alice", new Dictionary<string, object> { ["saveVersion"] = 3L });
            Assert.That(result.Status, Is.EqualTo(PlayerNameClaimStatus.Failed));
            Assert.That(backend.Store.Data["Players/uid"]["saveVersion"], Is.EqualTo(99L));
            Assert.That(backend.Store.Data.Count, Is.EqualTo(1));
        }

        [Test]
        public void ProgressMapper_HandlesUtcTimestampLargeIntegersAndAbsentNewFields()
        {
            var utc = new DateTime(2026, 11, 7, 1, 0, 0, DateTimeKind.Utc);
            var snapshot = new CloudDocumentSnapshot(true, new Dictionary<string, object>
            {
                ["maxLevel"] = 8L, ["coinBalance"] = long.MaxValue, ["realLevelIndex"] = -1L,
                ["updatedAt"] = utc, ["powerUps"] = new Dictionary<string, object> { ["Hammer"] = long.MaxValue }
            });
            var method = typeof(FirebaseProfileHandler).GetMethod("CreateProgressFromSnapshot", BindingFlags.Static | BindingFlags.NonPublic);
            var result = (FirebasePlayerProgress)method.Invoke(null, new object[] { "uid", snapshot });
            Assert.That(result.MaxReachedLevelIndex, Is.EqualTo(8));
            Assert.That(result.CoinBalance, Is.EqualTo(int.MaxValue));
            Assert.That(result.RealLevelIndex, Is.EqualTo(-1));
            Assert.That(result.PowerUpAmounts["Hammer"], Is.EqualTo(int.MaxValue));
            Assert.That(result.UpdatedAtUtc, Is.EqualTo(utc));
            Assert.That(result.HasUpdatedAt, Is.True);
            Assert.That(result.FrameworkSave, Is.Null);
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task AuthFacade_UsesNeutralPortForLinkAndCredentialConflict(bool conflict)
        {
            backend.Authentication.User = new CloudUser("guest", true);
            backend.Authentication.LinkConflict = conflict;
            var result = await FirebaseAuthHandler.SignInWithFacebookAsync("test-token");
            Assert.That(result.Status, Is.EqualTo(conflict ? FirebaseFacebookSignInStatus.NeedsConflictResolution : FirebaseFacebookSignInStatus.Success));
            Assert.That(result.UserId, Is.EqualTo(conflict ? "existing" : "guest"));
            Assert.That(result.LinkedAnonymousUser, Is.EqualTo(!conflict));
            Assert.That(FirebaseAuthHandler.Authentication, Is.SameAs(backend.Authentication));
        }

        [TestCase(CloudError.Timeout, FirebaseSyncFailureKind.Timeout)]
        [TestCase(CloudError.Offline, FirebaseSyncFailureKind.Offline)]
        [TestCase(CloudError.PermissionDenied, FirebaseSyncFailureKind.PermissionDenied)]
        [TestCase(CloudError.Authentication, FirebaseSyncFailureKind.Authentication)]
        public void ProfileFailureMapping_UsesNeutralCodes(CloudError error, FirebaseSyncFailureKind expected)
        {
            var method = typeof(FirebaseProfileHandler).GetMethod("ClassifyFailure", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method.Invoke(null, new object[] { new CloudException(error, "failure") }), Is.EqualTo(expected));
        }

        [Test]
        public async Task AuthFacade_SdkFreeTemplateCanDetachLocalFacebookSync()
        {
            if (FirebaseBackendFactory.IsCloudEnabled) Assert.Ignore("Requires the SDK-free configuration.");
            CloudServices.Shutdown();
            Assert.That(await FirebaseAuthHandler.SignOutFacebookSyncAsync(), Is.True);
        }

        [Test]
        public void FrameworkPorts_DoNotReferenceUnityOrFirebaseAndSdkCallsStayInsideAdapterBoundary()
        {
            foreach (var assembly in new[] { typeof(ICloudBackend).Assembly, typeof(NebulaSoft.Analytics.IAnalyticsProvider).Assembly })
                Assert.That(assembly.GetReferencedAssemblies().Any(a => a.Name.StartsWith("Firebase") || a.Name.StartsWith("UnityEngine")), Is.False);
            var roots = new[] { "Assets/NebulaSoft Core", "Assets/Project Files", "Assets/Tests" };
            const string adapter = "Assets/NebulaSoft Core/Integrations/Firebase/Runtime/";
            const string sdkReference = @"(?m)(?<![.\w])Firebase[ \t]*\.[ \t]*[A-Za-z_]|\busing\s+(?:global::\s*)?Firebase\s*;|^\s*#(?:if|elif)\b[^\r\n]*\bFIREBASE(?:_[A-Z]+)?\b";
            foreach (string path in roots.SelectMany(r => Directory.GetFiles(r, "*.cs", SearchOption.AllDirectories)))
            {
                string normalized = path.Replace('\\', '/');
                if (normalized.StartsWith(adapter, StringComparison.Ordinal)) continue;
                string source = File.ReadAllText(path);
                var match = System.Text.RegularExpressions.Regex.Match(source, sdkReference);
                Assert.That(match.Success, Is.False, $"{path}: SDK reference outside the Firebase adapter: {match.Value}");
            }
        }

        private Task<PlayerNameClaimResult> Claim(string name, Dictionary<string, object> data)
            => FirebasePlayerNameRegistry.ClaimAndWritePlayerAsync(backend.Store, backend.Store.Collection("Players").Document("uid"), "uid", name, data);

        private sealed class FakeBackend : ICloudBackend
        {
            public readonly FakeAuth Authentication = new FakeAuth();
            public readonly FakeStore Store = new FakeStore();
            public TaskCompletionSource<bool> Gate;
            public bool FailNextInitialization;
            public int InitializeCount, DisposeCount;
            public ICloudAuthProvider Auth => Authentication;
            public ICloudDocumentStore Documents => Store;
            public Task InitializeAsync(CancellationToken cancellation)
            {
                InitializeCount++;
                if (FailNextInitialization)
                {
                    FailNextInitialization = false;
                    return Task.FromException(new CloudException(CloudError.Unavailable, "retry"));
                }
                return Gate != null ? Gate.Task : Task.CompletedTask;
            }
            public void Dispose() { DisposeCount++; }
        }
        private sealed class FakeAuth : ICloudAuthProvider
        {
            public CloudUser User;
            public bool LinkConflict;
            public CloudUser CurrentUser => User;
            public Task<CloudUser> SignInAnonymouslyAsync() => Task.FromResult(User = new CloudUser("guest", true));
            public Task<CloudUser> LinkFacebookAsync(string token)
            {
                if (LinkConflict) throw new CloudException(CloudError.CredentialAlreadyInUse, "conflict");
                return Task.FromResult(User = new CloudUser(User.UserId, false));
            }
            public Task<CloudUser> SignInFacebookAsync(string token) => Task.FromResult(User = new CloudUser("existing", false));
            public void SignOut() { User = null; }
        }
        private sealed class FakeStore : ICloudDocumentStore
        {
            public Dictionary<string, Dictionary<string, object>> Data = new Dictionary<string, Dictionary<string, object>>();
            public CloudReadSource LastReadSource;
            public CloudWriteMode LastWriteMode;
            public Task<CloudDocumentSnapshot> GetSnapshotAsync(string path, CloudReadSource source)
            {
                LastReadSource = source;
                bool exists = Data.TryGetValue(path, out var data);
                return Task.FromResult(new CloudDocumentSnapshot(exists, data));
            }
            public Task SetAsync(string path, IDictionary<string, object> data, CloudWriteMode mode)
            {
                LastWriteMode = mode;
                if (mode == CloudWriteMode.Replace || !Data.ContainsKey(path)) Data[path] = new Dictionary<string, object>();
                foreach (var pair in data) Data[path][pair.Key] = pair.Value is CloudFieldValue ? (object)DateTime.UtcNow : pair.Value;
                return Task.CompletedTask;
            }
            public async Task RunTransactionAsync(Func<ICloudTransaction, Task> action)
            {
                var working = new FakeStore { Data = Data.ToDictionary(p => p.Key, p => new Dictionary<string, object>(p.Value)) };
                await action(new FakeTransaction(working));
                Data = working.Data;
            }
            private sealed class FakeTransaction : ICloudTransaction
            {
                private readonly FakeStore store;
                public FakeTransaction(FakeStore store) { this.store = store; }
                public Task<CloudDocumentSnapshot> GetSnapshotAsync(CloudDocumentReference document) => store.GetSnapshotAsync(document.Path, CloudReadSource.Server);
                public void Set(CloudDocumentReference document, IDictionary<string, object> data, CloudWriteMode mode = CloudWriteMode.Replace) => store.SetAsync(document.Path, data, mode).GetAwaiter().GetResult();
                public void Delete(CloudDocumentReference document) => store.Data.Remove(document.Path);
            }
        }
    }
}
