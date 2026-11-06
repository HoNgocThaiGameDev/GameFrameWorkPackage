using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NebulaSoft.Ads;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace NebulaSoft.Tests
{
    public sealed class AdsModuleTests
    {
        private FakeProvider provider;
        private AdsService service;
        private double now;
        private static readonly AdPlacementDefinition Banner = new AdPlacementDefinition("banner", AdFormat.Banner, preload: false);
        private static readonly AdPlacementDefinition Rewarded = new AdPlacementDefinition("revive", AdFormat.Rewarded, preload: false);
        private static readonly AdPlacementDefinition Interstitial = new AdPlacementDefinition("transition", AdFormat.Interstitial, preload: false, cooldownSeconds: 10);

        [SetUp]
        public void SetUp()
        {
            now = 0;
            provider = new FakeProvider();
            service = new AdsService(provider, new[] { Banner, Rewarded, Interstitial }, () => now);
            Assert.That(service.InitializeAsync().Result, Is.True);
        }

        [TearDown] public void TearDown() => service.Dispose();

        [Test]
        public void RewardAndCloseRepeated_CompleteAndGrantOnce()
        {
            int rewards = 0, completions = 0;
            service.AdEvent += value => { if (value.Kind == AdEventKind.RewardGranted) rewards++; };
            var task = service.ShowAsync(Rewarded.Id);
            task.GetAwaiter().OnCompleted(() => completions++);
            var request = provider.Last;
            provider.Emit(request, AdEventKind.Started);
            provider.Emit(request, AdEventKind.RewardGranted);
            provider.Emit(request, AdEventKind.RewardGranted);
            Assert.That(task.IsCompleted, Is.False, "The fullscreen lock lasts until close.");
            provider.Emit(request, AdEventKind.Closed);
            provider.Emit(request, AdEventKind.Closed);
            Assert.That(task.Result.RewardGranted, Is.True);
            Assert.That(rewards, Is.EqualTo(1));
            Assert.That(completions, Is.EqualTo(1));
        }

        [Test]
        public void CloseWithoutReward_DoesNotGrant()
        {
            var task = service.ShowAsync(Rewarded.Id);
            provider.Emit(provider.Last, AdEventKind.Started);
            provider.Emit(provider.Last, AdEventKind.Closed);
            Assert.That(task.Result.Outcome, Is.EqualTo(AdOutcome.ClosedWithoutReward));
        }

        [Test]
        public void ConcurrentShows_RejectSecondWithoutReplacingFirst()
        {
            var first = service.ShowAsync(Rewarded.Id);
            var request = provider.Last;
            var second = service.ShowAsync(Interstitial.Id);
            Assert.That(second.Result.Outcome, Is.EqualTo(AdOutcome.Busy));
            Assert.That(provider.ShowCount, Is.EqualTo(1));
            provider.Emit(request, AdEventKind.RewardGranted);
            provider.Emit(request, AdEventKind.Closed);
            Assert.That(first.Result.RewardGranted, Is.True);
        }

        [Test]
        public void LatePreviousReward_CannotRewardNextRequest()
        {
            var first = service.ShowAsync(Rewarded.Id);
            var old = provider.Last;
            provider.Emit(old, AdEventKind.Closed);
            Assert.That(first.Result.RewardGranted, Is.False);
            var second = service.ShowAsync(Rewarded.Id);
            provider.Emit(old, AdEventKind.RewardGranted);
            provider.Emit(provider.Last, AdEventKind.Closed);
            Assert.That(second.Result.RewardGranted, Is.False);
        }

        [Test]
        public void CancellationDuringNativeAd_KeepsLockUntilClose()
        {
            using (var cancellation = new CancellationTokenSource())
            {
                var task = service.ShowAsync(Rewarded.Id, cancellation.Token);
                var request = provider.Last;
                cancellation.Cancel();
                Assert.That(task.Result.Outcome, Is.EqualTo(AdOutcome.Cancelled));
                Assert.That(service.IsFullscreenBusy, Is.True);
                Assert.That(service.ShowAsync(Interstitial.Id).Result.Outcome, Is.EqualTo(AdOutcome.Busy));
                provider.Emit(request, AdEventKind.RewardGranted);
                provider.Emit(request, AdEventKind.Closed);
                Assert.That(service.IsFullscreenBusy, Is.False);
                Assert.That(task.Result.RewardGranted, Is.False);
            }
        }

        [Test]
        public void CancelBeforeShow_DoesNotCallProvider()
        {
            using (var cancellation = new CancellationTokenSource())
            {
                cancellation.Cancel();
                Assert.That(service.ShowAsync(Rewarded.Id, cancellation.Token).Result.Outcome, Is.EqualTo(AdOutcome.Cancelled));
                Assert.That(provider.ShowCount, Is.Zero);
            }
        }

        [Test]
        public void Dispose_CompletesPendingAndIgnoresCapturedLateCallbacks()
        {
            int events = 0;
            service.AdEvent += value => events++;
            var task = service.ShowAsync(Rewarded.Id);
            var captured = provider.CaptureEventHandler();
            var request = provider.Last;
            service.Dispose();
            captured(new AdEvent(request.Id, request.Placement.Id, request.Placement.Format, AdEventKind.RewardGranted, provider.Id));
            service.Tick();
            Assert.That(task.Result.Outcome, Is.EqualTo(AdOutcome.Disposed));
            Assert.That(events, Is.Zero);
            Assert.That(provider.DisposeCount, Is.EqualTo(1));
            Assert.That(service.ShowAsync(Rewarded.Id).Result.Outcome, Is.EqualTo(AdOutcome.Disposed));
        }

        [Test]
        public void NoAds_BlocksForcedFormatsAndKeepsRewarded()
        {
            service.SetForcedAdsDisabled(true);
            Assert.That(service.ShowBanner(Banner.Id), Is.False);
            Assert.That(service.ShowAsync(Interstitial.Id).Result.Outcome, Is.EqualTo(AdOutcome.Disabled));
            var task = service.ShowAsync(Rewarded.Id);
            provider.Emit(provider.Last, AdEventKind.RewardGranted);
            provider.Emit(provider.Last, AdEventKind.Closed);
            Assert.That(task.Result.RewardGranted, Is.True);
        }

        [Test]
        public void Banner_UsesActualGeometryAndRejectsLateVisibleEventAfterHide()
        {
            int changes = 0;
            service.BannerChanged += value => changes++;
            Assert.That(service.ShowBanner(Banner.Id), Is.True);
            Assert.That(service.Banner.IsVisible, Is.False);
            provider.Banner(new BannerInfo("native", 900, 150, 24));
            provider.Banner(new BannerInfo("native", 900, 150, 24));
            Assert.That(service.Banner.TopEdge, Is.EqualTo(174));
            service.HideBanner();
            provider.Banner(new BannerInfo("native", 900, 150, 24));
            Assert.That(service.Banner.IsVisible, Is.False);
            Assert.That(changes, Is.EqualTo(2));
        }

        [Test]
        public void DuplicateBannerShow_DoesNotStartAnotherLoadOrView()
        {
            service.ShowBanner(Banner.Id);
            service.ShowBanner(Banner.Id);
            Assert.That(provider.BannerShowCount, Is.EqualTo(1));
        }

        [Test]
        public void NoAdsHidesBanner_AndRejectsLateGeometry()
        {
            service.ShowBanner(Banner.Id);
            provider.Banner(new BannerInfo("native", 900, 150));
            service.SetForcedAdsDisabled(true);
            provider.Banner(new BannerInfo("native", 900, 150));
            Assert.That(service.Banner.IsVisible, Is.False);
            Assert.That(provider.HideCount, Is.EqualTo(1));
        }

        [Test]
        public void Cooldown_UsesInjectedClock()
        {
            var task = service.ShowAsync(Interstitial.Id);
            provider.Emit(provider.Last, AdEventKind.Started);
            provider.Emit(provider.Last, AdEventKind.Closed);
            Assert.That(task.Result.Succeeded, Is.True);
            now = 9;
            Assert.That(service.ShowAsync(Interstitial.Id).Result.Outcome, Is.EqualTo(AdOutcome.Disabled));
            now = 10;
            var second = service.ShowAsync(Interstitial.Id);
            Assert.That(second.IsCompleted, Is.False);
            provider.Emit(provider.Last, AdEventKind.Closed);
        }

        [Test]
        public void ProviderShowException_ReturnsFailureAndReleasesLock()
        {
            provider.ThrowOnShow = true;
            Assert.That(service.ShowAsync(Rewarded.Id).Result.Outcome, Is.EqualTo(AdOutcome.Failed));
            Assert.That(service.IsFullscreenBusy, Is.False);
        }

        [Test]
        public void WorkerThreadCallback_IsDeliveredOnTick()
        {
            int deliveredThread = 0;
            int ownerThread = Thread.CurrentThread.ManagedThreadId;
            service.AdEvent += value => deliveredThread = Thread.CurrentThread.ManagedThreadId;
            var task = service.ShowAsync(Rewarded.Id);
            var request = provider.Last;
            var thread = new Thread(() => { provider.Emit(request, AdEventKind.RewardGranted); provider.Emit(request, AdEventKind.Closed); });
            thread.Start(); thread.Join();
            Assert.That(task.IsCompleted, Is.False);
            service.Tick();
            Assert.That(deliveredThread, Is.EqualTo(ownerThread));
            Assert.That(task.Result.RewardGranted, Is.True);
        }

        [Test]
        public void SubscriberException_DoesNotPreventResultOrOtherSubscribers()
        {
            int delivered = 0;
            service.AdEvent += value => throw new InvalidOperationException("UI subscriber");
            service.AdEvent += value => delivered++;
            var task = service.ShowAsync(Rewarded.Id);
            provider.Emit(provider.Last, AdEventKind.Closed);
            Assert.That(task.IsCompleted, Is.True);
            Assert.That(delivered, Is.EqualTo(1));
        }

        [Test]
        public void InvalidOrDisabledPlacement_DoesNotCallProvider()
        {
            Assert.That(service.ShowAsync("missing").Result.Outcome, Is.EqualTo(AdOutcome.Disabled));
            Assert.That(service.ShowAsync(Banner.Id).Result.Outcome, Is.EqualTo(AdOutcome.Disabled));
            Assert.That(service.GetState((string)null), Is.EqualTo(AdState.Unavailable));
            Assert.That(provider.ShowCount, Is.Zero);
        }

        [Test]
        public void NotReady_ReturnsExplicitResultWithoutStartingShow()
        {
            provider.State = AdState.Loading;
            Assert.That(service.ShowAsync(Rewarded.Id).Result.Outcome, Is.EqualTo(AdOutcome.NotReady));
            Assert.That(provider.ShowCount, Is.Zero);
        }

        [Test]
        public void Initialization_IsSharedEvenWhenAStatusSubscriberCallsItAgain()
        {
            service.Dispose();
            provider = new FakeProvider();
            service = new AdsService(provider, new[] { Rewarded }, () => now);
            Task<bool> reentrant = null;
            service.StatusChanged += value => { if (value == AdsStatus.Initializing) reentrant = service.InitializeAsync(); };
            var first = service.InitializeAsync();
            Assert.That(reentrant, Is.SameAs(first));
            Assert.That(first.Result, Is.True);
        }

        [Test]
        public void DisposeDuringInitialization_CompletesCallerAndIgnoresLateSuccess()
        {
            service.Dispose();
            var completion = new TaskCompletionSource<bool>();
            provider = new FakeProvider { Initialization = completion.Task };
            service = new AdsService(provider, new[] { Rewarded }, () => now);
            var task = service.InitializeAsync();
            Assert.That(task.IsCompleted, Is.False);
            service.Dispose();
            Assert.That(task.Result, Is.False);
            completion.SetResult(true);
            Assert.That(service.Status, Is.EqualTo(AdsStatus.Disposed));
        }

        [Test]
        public void FailedInitialization_CanRetryWithoutReplacingTheProvider()
        {
            service.Dispose();
            provider = new FakeProvider { Initialization = Task.FromResult(false) };
            service = new AdsService(provider, new[] { Rewarded }, () => now);
            Assert.That(service.InitializeAsync().Result, Is.False);
            Assert.That(service.Status, Is.EqualTo(AdsStatus.Failed));
            provider.Initialization = Task.FromResult(true);
            Assert.That(service.InitializeAsync().Result, Is.True);
        }

        [Test]
        public void DuplicateCatalogIds_AreRejected()
        {
            Assert.Throws<ArgumentException>(() => new AdsService(provider, new[] { Rewarded, Rewarded }, () => now));
        }

        [Test]
        public void BannerShowException_ClearsGeometryAndAllowsRetry()
        {
            provider.ThrowOnBannerShow = true;
            Assert.That(service.ShowBanner(Banner.Id), Is.False);
            Assert.That(service.Banner.IsVisible, Is.False);
            Assert.That(provider.HideCount, Is.GreaterThan(0));
            provider.ThrowOnBannerShow = false;
            Assert.That(service.ShowBanner(Banner.Id), Is.True);
            Assert.That(provider.BannerShowCount, Is.EqualTo(2));
        }

        [Test]
        public void ProviderTickOrDisposeException_DoesNotBreakServiceCleanup()
        {
            provider.ThrowOnTick = provider.ThrowOnDispose = true;
            Assert.DoesNotThrow(() => service.Tick());
            Assert.DoesNotThrow(() => service.Dispose());
            Assert.That(service.Status, Is.EqualTo(AdsStatus.Disposed));
            Assert.DoesNotThrow(() => service.Dispose());
            Assert.That(provider.DisposeCount, Is.EqualTo(1));
        }

        [Test]
        public void RuntimeAssembly_CannotReferenceUnitySdkOrGameAssemblies()
        {
            foreach (var assembly in typeof(AdsService).Assembly.GetReferencedAssemblies())
            {
                Assert.That(assembly.Name, Does.Not.StartWith("Unity"));
                Assert.That(assembly.Name, Does.Not.StartWith("VoxelBusters"));
                Assert.That(assembly.Name, Is.Not.EqualTo("Assembly-CSharp"));
            }
        }

        [Test]
        public void WiredTemplateSettings_ValidateAndPreserveCurrentFormats()
        {
            var settings = AssetDatabase.LoadAssetAtPath<AdsModuleSettings>("Assets/Project Files/Data/Ads/AdsModuleSettings.asset");
            var init = AssetDatabase.LoadAssetAtPath<ProjectInitSettings>("Assets/Project Files/Data/Project Init Settings.asset");
            Assert.That(settings, Is.Not.Null);
            Assert.That(settings.ValidateSettings(), Is.Empty);
            Assert.That(init.GetModule<AdsInitModule>().Settings, Is.SameAs(settings));
            Assert.That(settings.Banner.IsEnabled, Is.True);
            Assert.That(settings.Interstitial.IsEnabled, Is.False);
            Assert.That(settings.Rewarded.IsEnabled, Is.False);
        }

        [Test]
        public void SaveBridge_PreservesLegacyNoAdsAndTimedReward()
        {
            const BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;
            var field = typeof(SaveController).GetField("globalSave", flags);
            var loaded = typeof(SaveController).GetField("isSaveLoaded", flags);
            var required = typeof(SaveController).GetField("isSaveRequired", flags);
            var revision = typeof(SaveController).GetField("saveRevision", flags);
            object previous = field.GetValue(null), previousLoaded = loaded.GetValue(null), previousRequired = required.GetValue(null), previousRevision = revision.GetValue(null);
            try
            {
                var global = new GlobalSave(); global.Init(0);
                field.SetValue(null, global); loaded.SetValue(null, true);
                var original = global.GetSaveObject<AdSave>("advertisement_forced_ad");
                original.ForcedAdDisabledUntil = -1;
                double unix = 100;
                var bridge = new AdsSaveBridge(service, () => unix);
                Assert.That(bridge.HasNoAds, Is.True);
                bridge.DisableFor(1000);
                Assert.That(original.ForcedAdDisabledUntil, Is.EqualTo(-1));
                bridge.SetNoAds(false);
                bridge.DisableFor(50);
                bridge.SetNoAds(false);
                Assert.That(original.ForcedAdDisabledUntil, Is.EqualTo(150));
                Assert.That(service.ForcedAdsDisabled, Is.True);
                unix = 150; bridge.Tick();
                Assert.That(service.ForcedAdsDisabled, Is.False);
            }
            finally
            {
                field.SetValue(null, previous); loaded.SetValue(null, previousLoaded);
                required.SetValue(null, previousRequired); revision.SetValue(null, previousRevision);
            }
        }

        private sealed class FakeProvider : IAdsProvider
        {
            public string Id => "Fake";
            public event Action<AdEvent> AdEvent;
            public event Action<BannerInfo> BannerChanged;
            public AdRequest Last;
            public int ShowCount, BannerShowCount, HideCount, DisposeCount;
            public bool ThrowOnShow, ThrowOnBannerShow, ThrowOnTick, ThrowOnDispose;
            public AdState State = AdState.Ready;
            public Task<bool> Initialization = Task.FromResult(true);
            public Task<bool> InitializeAsync(CancellationToken cancellation) => Initialization;
            public AdState GetState(AdPlacementDefinition placement) => State;
            public void Load(AdPlacementDefinition placement) { }
            public void Show(AdRequest request) { ShowCount++; Last = request; if (ThrowOnShow) throw new InvalidOperationException("SDK failure"); }
            public void ShowBanner(AdRequest request)
            {
                BannerShowCount++;
                if (!ThrowOnBannerShow) return;
                BannerChanged?.Invoke(new BannerInfo(Id, 320, 50));
                throw new InvalidOperationException("SDK banner failure after reporting geometry");
            }
            public void HideBanner(bool destroy) { HideCount++; }
            public void Tick() { if (ThrowOnTick) throw new InvalidOperationException("SDK tick failure"); }
            public void Dispose() { DisposeCount++; if (ThrowOnDispose) throw new InvalidOperationException("SDK disposal failure"); }
            public Action<AdEvent> CaptureEventHandler() => AdEvent;
            public void Banner(BannerInfo info) => BannerChanged?.Invoke(info);
            public void Emit(AdRequest request, AdEventKind kind) => AdEvent?.Invoke(new AdEvent(request.Id, request.Placement.Id, request.Placement.Format, kind, Id));
        }
    }
}
