using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NebulaSoft.Ads;
using NebulaSoft.Analytics;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace NebulaSoft.Tests
{
    public sealed class AnalyticsModuleTests
    {
        private AnalyticsService service;
        private FakeProvider provider;
        private AnalyticsEvents events;
        private double now;

        [SetUp]
        public void SetUp()
        {
            now = 0;
            service = new AnalyticsService();
            provider = new FakeProvider();
            events = new AnalyticsEvents(service);
        }

        [TearDown]
        public void TearDown() { service.Dispose(); AnalyticsController.Shutdown(); }

        [Test]
        public void Event_SnapshotsMutableInputAndPreservesNumericTypes()
        {
            var input = new Dictionary<string, object> { ["level"] = 7, ["success"] = true, ["duration"] = 2.5f };
            var value = new AnalyticsEvent("level_end", input);
            input["level"] = 99;
            Assert.That(value.Parameters["level"], Is.EqualTo(7L));
            Assert.That(value.Parameters["success"], Is.EqualTo(1L));
            Assert.That(value.Parameters["duration"], Is.TypeOf<double>());
        }

        [TestCase("firebase_custom")]
        [TestCase("google_test")]
        [TestCase("ga_test")]
        [TestCase("1_level")]
        [TestCase("level complete")]
        [TestCase("évent")]
        public void Event_RejectsInvalidOrReservedNames(string name)
        { Assert.Throws<ArgumentException>(() => new AnalyticsEvent(name)); }

        [Test]
        public void Event_RejectsUnsupportedValuesTooManyParametersAndNonFiniteNumbers()
        {
            Assert.Throws<ArgumentException>(() => new AnalyticsEvent("test", new Dictionary<string, object> { ["invalid"] = new object() }));
            Assert.Throws<ArgumentException>(() => new AnalyticsEvent("test", new Dictionary<string, object> { ["value"] = double.NaN }));
            var tooMany = Enumerable.Range(0, 26).ToDictionary(i => "p_" + i, i => (object)i);
            Assert.Throws<ArgumentException>(() => new AnalyticsEvent("test", tooMany));
        }

        [Test]
        public void StartupQueue_FlushesSnapshotsInOrderAndInitializesOnlyOnce()
        {
            provider.Gate = new TaskCompletionSource<bool>();
            events.LevelStart(3);
            var initialization = service.InitializeAsync(provider);
            Assert.That(service.InitializeAsync(provider), Is.SameAs(initialization));
            events.BoosterUsed("hammer", 3);
            Assert.That(provider.Events, Is.Empty);
            Assert.That(service.PendingCount, Is.EqualTo(2));
            provider.Gate.SetResult(true);
            Assert.That(initialization.Result, Is.True);
            Assert.That(provider.Events.Select(e => e.Name), Is.EqualTo(new[] { "level_start", "booster_used" }));
            Assert.That(provider.InitializeCount, Is.EqualTo(1));
            Assert.That(service.PendingCount, Is.Zero);
        }

        [Test]
        public void StartupQueue_IsBoundedAndRetainsNewestEvents()
        {
            service.Dispose(); service = new AnalyticsService(2); events = new AnalyticsEvents(service);
            events.LevelStart(1); events.LevelStart(2); events.LevelStart(3);
            Assert.That(service.PendingCount, Is.EqualTo(2));
            Assert.That(service.InitializeAsync(provider).Result, Is.True);
            Assert.That(provider.Events.Select(e => e.Parameters["level"]), Is.EqualTo(new object[] { 2L, 3L }));
        }

        [Test]
        public void CollectionDisabled_DropsPendingAndNewEventsThenAllowsFutureEvents()
        {
            events.LevelStart(1);
            service.SetCollectionEnabled(false);
            Assert.That(service.PendingCount, Is.Zero);
            Assert.That(events.LevelStart(2), Is.False);
            Assert.That(service.InitializeAsync(provider).Result, Is.True);
            Assert.That(provider.CollectionEnabled, Is.False);
            service.SetCollectionEnabled(true);
            events.LevelStart(3);
            Assert.That(provider.Events.Single().Parameters["level"], Is.EqualTo(3L));
        }

        [Test]
        public void DisposeDuringInitialization_DiscardsLateCompletionAndUnsentEvents()
        {
            provider.Gate = new TaskCompletionSource<bool>();
            events.LevelStart(1);
            var initialization = service.InitializeAsync(provider);
            service.Dispose(); service.Dispose();
            provider.Gate.SetResult(true);
            Assert.That(initialization.Result, Is.False);
            Assert.That(service.IsReady, Is.False);
            Assert.That(provider.Events, Is.Empty);
            Assert.That(provider.DisposeCount, Is.EqualTo(1));
            Assert.That(events.LevelStart(2), Is.False);
        }

        [Test]
        public void ProviderException_DoesNotInterruptGameplayOrBlockFollowingEvents()
        {
            provider.FailNextEvent = true;
            service.InitializeAsync(provider).GetAwaiter().GetResult();
            Assert.That(events.LevelStart(1), Is.False);
            Assert.That(events.LevelStart(2), Is.True);
            Assert.That(provider.Events.Single().Parameters["level"], Is.EqualTo(2L));
        }

        [Test]
        public void UserContext_SetBeforeInitializationIsAppliedBeforeFirstEvent()
        {
            service.SetUserId("anonymous-player-id");
            service.SetUserProperty("difficulty_group", "variant_b");
            events.LevelStart(1);
            service.InitializeAsync(provider).GetAwaiter().GetResult();
            Assert.That(provider.UserId, Is.EqualTo("anonymous-player-id"));
            Assert.That(provider.UserProperties["difficulty_group"], Is.EqualTo("variant_b"));
            Assert.That(provider.ContextPresentAtLog, Is.True);
            service.SetUserId(null);
            Assert.That(provider.UserId, Is.Null);
        }

        [Test]
        public void LevelSession_ReviveAndRepeatedCompletionProduceOneFinalOutcome()
        {
            service.InitializeAsync(provider).GetAwaiter().GetResult();
            var session = new LevelTracker(events, () => now);
            session.Start(12, 2); session.Start(12, 2);
            session.Move(); session.Booster("hammer");
            now = 5; session.Fail("timeout"); session.Revive("rewarded_ad");
            now = 9; session.End(true); session.End(false);
            Assert.That(provider.Events.Count(e => e.Name == "level_start"), Is.EqualTo(1));
            Assert.That(provider.Events.Count(e => e.Name == "level_end"), Is.EqualTo(1));
            var result = provider.Events.Single(e => e.Name == "level_end").Parameters;
            Assert.That(result["success"], Is.EqualTo(1L));
            Assert.That(result["duration"], Is.EqualTo(9d));
            Assert.That(result["attempt"], Is.EqualTo(2L));
            Assert.That(result["moves"], Is.EqualTo(1L));
            Assert.That(result["boosters_used"], Is.EqualTo(1L));
        }

        [Test]
        public void LevelSession_ExcludesBackgroundTimeAndEndsQuitOnlyOnce()
        {
            service.InitializeAsync(provider).GetAwaiter().GetResult();
            var session = new LevelTracker(events, () => now);
            session.Start(1); now = 3; session.SetPaused(true);
            now = 100; session.SetPaused(true); session.SetPaused(false);
            now = 102; session.End(false, "quit"); session.End(false, "quit");
            var result = provider.Events.Single(e => e.Name == "level_end").Parameters;
            Assert.That(result["duration"], Is.EqualTo(5d));
            Assert.That(result["reason"], Is.EqualTo("quit"));
            Assert.That(result["success"], Is.EqualTo(0L));
        }

        [Test]
        public void Catalogue_ContainsTutorialEconomyBoosterRewardsAndMonetizationEvents()
        {
            service.InitializeAsync(provider).GetAwaiter().GetResult();
            events.TutorialBegin("intro"); events.TutorialStep("intro", 2); events.TutorialComplete("intro");
            events.BoosterPurchased("hammer", 3, "shop"); events.BoosterUsed("hammer", 1, 2);
            events.EarnCurrency("coins", 100, "daily_reward"); events.SpendCurrency("coins", 20, "hammer");
            events.RewardClaimed("quest", "quest_1"); events.IapAttempt("starter"); events.IapCompleted("starter");
            events.IapFailed("starter", "cancelled"); events.LevelRetry(1); events.LevelRevive(1, "coins");
            events.ScreenView("Home"); events.Login("anonymous"); events.SettingChanged("sound", "off");
            Assert.That(provider.Events.Count, Is.EqualTo(16));
            Assert.That(provider.Events.Select(e => e.Name), Does.Contain("tutorial_complete"));
            Assert.That(provider.Events.Select(e => e.Name), Does.Contain("earn_virtual_currency"));
            Assert.That(provider.Events.Select(e => e.Name), Does.Not.Contain("in_app_purchase"));
            Assert.That(provider.Events.Select(e => e.Name), Does.Not.Contain("purchase"));
        }

        [Test]
        public void AdsBridge_OnlyRealImpressionAndRewardCallbacksProduceThoseEvents()
        {
            service.InitializeAsync(provider).GetAwaiter().GetResult();
            using (var bridge = new AnalyticsBridge(events))
            {
                bridge.OnAdEvent(new AdEvent(Guid.NewGuid(), "revive", AdFormat.Rewarded, AdEventKind.Loaded, "fake"));
                Assert.That(provider.Events, Is.Empty);
                bridge.OnAdEvent(new AdEvent(Guid.NewGuid(), "revive", AdFormat.Rewarded, AdEventKind.ShowFailed, "fake", "not_ready"));
                Assert.That(provider.Events.Select(e => e.Name), Is.EqualTo(new[] { "ad_fail" }));
                bridge.OnAdEvent(new AdEvent(Guid.NewGuid(), "revive", AdFormat.Rewarded, AdEventKind.Started, "fake"));
                bridge.OnAdEvent(new AdEvent(Guid.NewGuid(), "revive", AdFormat.Rewarded, AdEventKind.Impression, "fake"));
                bridge.OnAdEvent(new AdEvent(Guid.NewGuid(), "revive", AdFormat.Rewarded, AdEventKind.RewardGranted, "fake"));
                Assert.That(provider.Events.Count(e => e.Name == "ad_impression_custom"), Is.EqualTo(1));
                Assert.That(provider.Events.Select(e => e.Name), Does.Not.Contain("ad_impression"));
                Assert.That(provider.Events.Count(e => e.Name == "ad_reward"), Is.EqualTo(1));
            }
        }

        [Test]
        public void LegacyCurrencyHooks_ReachNewServiceAndDoNotCountLegacyAdRequestsAsImpressions()
        {
            AnalyticsController.Shutdown();
            AnalyticsController.Service.InitializeAsync(provider).GetAwaiter().GetResult();
            AnalyticsController.OnCurrencySource("daily", new Dictionary<CurrencyType, int> { [CurrencyType.Coins] = 25 });
            AnalyticsController.OnRVClicked("revive"); AnalyticsController.OnInterstitialDisplayed("complete");
            Assert.That(provider.Events.Select(e => e.Name), Is.EqualTo(new[] { "earn_virtual_currency" }));
            Assert.That(provider.Events[0].Parameters["value"], Is.EqualTo(25d));
        }

        [Test]
        public void ProjectRegistry_HasOneConfiguredAnalyticsModule()
        {
            var registry = AssetDatabase.LoadAssetAtPath<ProjectInitSettings>("Assets/Project Files/Data/Project Init Settings.asset");
            Assert.That(registry.Modules.OfType<AnalyticsInitModule>().Count(), Is.EqualTo(1));
            var module = registry.GetModule<AnalyticsInitModule>();
            Assert.That(module.Settings, Is.Not.Null);
            Assert.That(module.Settings.Provider, Is.EqualTo(AnalyticsBackend.Console));
        }

        [Test]
        public void IapBridge_ReportsFailureFromManagerAndUnsubscribesOnDispose()
        {
            service.InitializeAsync(provider).GetAwaiter().GetResult();
            var bridge = new AnalyticsBridge(events);
            try
            {
                IAPManager.OnPurchaseFailed("starter_pack", PurchaseFailureReason.UserCancelled);
                Assert.That(provider.Events.Single().Name, Is.EqualTo("iap_failed"));
                Assert.That(provider.Events[0].Parameters["item_id"], Is.EqualTo("starter_pack"));
                Assert.That(provider.Events[0].Parameters["reason"], Is.EqualTo("UserCancelled"));
            }
            finally { bridge.Dispose(); }
            IAPManager.OnPurchaseFailed("starter_pack", PurchaseFailureReason.UserCancelled);
            Assert.That(provider.Events.Count, Is.EqualTo(1));
        }

        [Test]
        public void Module_ConfiguresOnceAndDisposesItsServiceOnShutdown()
        {
            AnalyticsController.Shutdown();
            var owned = AnalyticsController.Service;
            var settings = ScriptableObject.CreateInstance<AnalyticsSettings>();
            var owner = new GameObject("Analytics module lifecycle test");
            try
            {
                var module = owner.AddComponent<AnalyticsModule>();
                AnalyticsController.Events.LevelStart(1);
                module.Configure(settings);
                module.Configure(settings);
                Assert.That(owned.IsReady, Is.True);
                Assert.That(owned.PendingCount, Is.Zero);
                Assert.That(AnalyticsController.Service, Is.SameAs(owned));
                module.Shutdown();
                module.Shutdown();
                Assert.That(owned.IsReady, Is.False);
                Assert.That(owned.Track(new AnalyticsEvent("after_dispose")), Is.False);
                Assert.That(AnalyticsController.Service, Is.Not.SameAs(owned));
            }
            finally
            {
                if (owner != null)
                {
                    owner.GetComponent<AnalyticsModule>()?.Shutdown();
                    UnityEngine.Object.DestroyImmediate(owner);
                }
                UnityEngine.Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void RetiredModule_ShutdownDoesNotDisposeAReplacementService()
        {
            AnalyticsController.Shutdown();
            var settings = ScriptableObject.CreateInstance<AnalyticsSettings>();
            var owner = new GameObject("Analytics retired module test");
            try
            {
                var module = owner.AddComponent<AnalyticsModule>();
                module.Configure(settings);
                AnalyticsController.Shutdown();
                var replacement = AnalyticsController.Service;
                replacement.InitializeAsync(provider).GetAwaiter().GetResult();
                module.Shutdown();
                Assert.That(replacement.IsReady, Is.True);
                Assert.That(AnalyticsController.Service, Is.SameAs(replacement));
                Assert.That(provider.DisposeCount, Is.Zero);
            }
            finally
            {
                owner.GetComponent<AnalyticsModule>()?.Shutdown();
                UnityEngine.Object.DestroyImmediate(owner);
                UnityEngine.Object.DestroyImmediate(settings);
            }
        }

        private sealed class FakeProvider : IAnalyticsProvider
        {
            public TaskCompletionSource<bool> Gate;
            public readonly List<AnalyticsEvent> Events = new List<AnalyticsEvent>();
            public readonly Dictionary<string, string> UserProperties = new Dictionary<string, string>();
            public int InitializeCount, DisposeCount;
            public bool CollectionEnabled, FailNextEvent, ContextPresentAtLog;
            public string UserId;
            public Task InitializeAsync(CancellationToken cancellation) { InitializeCount++; return Gate != null ? Gate.Task : Task.CompletedTask; }
            public void SetCollectionEnabled(bool enabled) { CollectionEnabled = enabled; }
            public void SetUserId(string userId) { UserId = userId; }
            public void SetUserProperty(string name, string value) { UserProperties[name] = value; }
            public void LogEvent(AnalyticsEvent value)
            {
                if (FailNextEvent) { FailNextEvent = false; throw new InvalidOperationException("Simulated provider failure."); }
                ContextPresentAtLog = UserId != null && UserProperties.Count > 0;
                Events.Add(value);
            }
            public void Dispose() { DisposeCount++; }
        }
    }
}
