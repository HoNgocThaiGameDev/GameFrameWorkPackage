using System;
using System.Collections;
using System.Reflection;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace NebulaSoft.Tests
{
    public sealed class RemoteConfigModuleTests
    {
        private static readonly FieldInfo Snapshot = typeof(RemoteConfigController)
            .GetField("configData", BindingFlags.Static | BindingFlags.NonPublic);
        private object previousSnapshot;
        private object previousBundled;
        private static readonly FieldInfo Bundled = typeof(RemoteConfigController).GetField("bundledData", BindingFlags.Static | BindingFlags.NonPublic);

        [SetUp]
        public void SetUp()
        {
            previousSnapshot = Snapshot.GetValue(null);
            previousBundled = Bundled.GetValue(null);
            Bundled.SetValue(null, null);
            Snapshot.SetValue(null, null);
        }

        [TearDown]
        public void TearDown()
        {
            Snapshot.SetValue(null, previousSnapshot);
            Bundled.SetValue(null, previousBundled);
        }

        [Test]
        public void TemplateRegistersOptionalModuleWithBundledFallback()
        {
            var settings = AssetDatabase.LoadAssetAtPath<ProjectInitSettings>("Assets/Project Files/Data/Project Init Settings.asset");
            var module = settings.GetModule<RemoteConfigInitModule>();

            Assert.That(module, Is.Not.Null);
            Assert.That(typeof(RemoteConfigInitModule).GetCustomAttribute<RegisterModuleAttribute>().Core, Is.False);
            var local = (TextAsset)new SerializedObject(module).FindProperty("localConfig").objectReferenceValue;
            Assert.That(local, Is.Not.Null);
            RemoteConfigController.Init(local.text);
            Assert.That(RemoteConfigController.TryGetConfig<ReviveRemoteConfigData>("revive"), Is.Not.Null);
            Assert.That(RemoteConfigController.TryGetConfig<PuzzleRemoteConfigData>("puzzle"), Is.Not.Null);
        }

        [TestCase("{\"ads\":{\"minLevel\":2},\"ads\":{\"minLevel\":9}}")]
        [TestCase("[]")]
        [TestCase("{}{}")]
        [TestCase("{\"ads\":")]
        public void InvalidPayloadPreservesLastSnapshot(string invalid)
        {
            RemoteConfigController.Init("{\"ads\":{\"minLevel\":12}}");
            LogAssert.Expect(LogType.Warning, "[Remote Config] Invalid or empty payload. Keeping the active configuration.");
            RemoteConfigController.Init(invalid);

            Assert.That(RemoteConfigController.TryGetConfig<AdsRemoteConfigData>("ads").minLevel, Is.EqualTo(12));
        }

        [Test]
        public void SupportsFlagsAndKeepsTextVerbatim()
        {
            RemoteConfigController.Init("{\"feature\":true,\"message\":\"2026-11-07T01:00:00Z\",\"unset\":null}");

            Assert.That(RemoteConfigController.TryGetValue("feature", out bool feature) && feature, Is.True);
            Assert.That(RemoteConfigController.TryGetValue("message", out string message), Is.True);
            Assert.That(message, Is.EqualTo("2026-11-07T01:00:00Z"));
            Assert.That(RemoteConfigController.TryGetValue("unset", out bool _), Is.False);
            Assert.That(RemoteConfigController.TryGetValue(null, out string _), Is.False);
        }

        [Test]
        public void InvalidGameOverridesDoNotEscapeTypedLookup()
        {
            RemoteConfigController.Init("{\"revive\":{\"prices\":[]},\"lives\":{\"resetTime\":0},\"ads\":{\"minLevel\":8}}");

            LogAssert.Expect(LogType.Warning, "[Remote Config] Validation failed for key 'revive'.");
            Assert.That(RemoteConfigController.TryGetConfig<ReviveRemoteConfigData>("revive"), Is.Null);
            LogAssert.Expect(LogType.Warning, "[Remote Config] Validation failed for key 'lives'.");
            Assert.That(RemoteConfigController.TryGetConfig<LivesRemoteConfigData>("lives"), Is.Null);
            Assert.That(RemoteConfigController.TryGetConfig<AdsRemoteConfigData>("ads").minLevel, Is.EqualTo(8));
        }

        [Test]
        public void ConfigExportProducesAnObjectTheRuntimeCanRead()
        {
            string json = RemoteConfigHandlerEditor.GetAllConfigsAsJson();

            Assert.That(JObject.Parse(json)["ads"], Is.Not.Null);
            RemoteConfigController.Init(json);
            Assert.That(RemoteConfigController.TryGetConfig<CurrencyRemoteConfigData>("currencies").GetCurrencyOverride(CurrencyType.Coins), Is.Not.Null);
        }

        [Test]
        public void MissingOrInvalidRemoteKey_UsesBundledValueWithoutDiscardingOtherOverrides()
        {
            Bundled.SetValue(null, JObject.Parse("{\"ads\":{\"minLevel\":6},\"flag\":true}"));
            RemoteConfigController.Init("{\"ads\":{\"minLevel\":-1},\"message\":\"remote\"}");
            LogAssert.Expect(LogType.Warning, "[Remote Config] Validation failed for key 'ads'.");
            Assert.That(RemoteConfigController.TryGetConfig<AdsRemoteConfigData>("ads").minLevel, Is.EqualTo(6));
            Assert.That(RemoteConfigController.TryGetValue("flag", out bool flag) && flag, Is.True);
            Assert.That(RemoteConfigController.TryGetValue("message", out string message), Is.True);
            Assert.That(message, Is.EqualTo("remote"));
        }

        [Test]
        public void GameSettingsCanReturnToAssetDefaultsOnNextInitialization()
        {
            var asset = ScriptableObject.CreateInstance<GameData>();
            int defaultLevel = asset.ShowAdsAfterLevel;
            try
            {
                RemoteConfigController.Init("{\"ads\":{\"minLevel\":25}}");
                asset.Init();
                Assert.That(GameData.Data.ShowAdsAfterLevel, Is.EqualTo(25));
                Assert.That(asset.ShowAdsAfterLevel, Is.EqualTo(defaultLevel));

                RemoteConfigController.Init("{}");
                asset.Init();
                Assert.That(GameData.Data.ShowAdsAfterLevel, Is.EqualTo(defaultLevel));
            }
            finally
            {
                typeof(GameData).GetMethod("UnloadStatic", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
                Object.DestroyImmediate(asset);
            }
        }

        [Test]
        public void PuzzleProfileLimitsRewardedRevivesAndPreservesOmittedSettings()
        {
            var asset = ScriptableObject.CreateInstance<GameData>();
            try
            {
                int defaultOfferSeconds = asset.ReviveOfferSeconds;
                RemoteConfigController.Init("{\"puzzle\":{\"maxRewardedRevivesPerAttempt\":1,\"interstitialOnFinalFailure\":false}}");
                asset.Init();

                Assert.That(GameData.Data.CanOfferRewardedRevive(0), Is.True);
                Assert.That(GameData.Data.CanOfferRewardedRevive(1), Is.False);
                Assert.That(GameData.Data.ReviveOfferSeconds, Is.EqualTo(defaultOfferSeconds));
                Assert.That(GameData.Data.InterstitialOnFinalFailure, Is.False);

                RemoteConfigController.Init("{\"puzzle\":{\"rewardedReviveEnabled\":false,\"reviveOfferSeconds\":8}}");
                asset.Init();
                Assert.That(GameData.Data.CanOfferRewardedRevive(0), Is.False);
                Assert.That(GameData.Data.ReviveOfferSeconds, Is.EqualTo(8));
                Assert.That(GameData.Data.InterstitialOnFinalFailure, Is.True);
                Assert.That(asset.ReviveOfferSeconds, Is.EqualTo(defaultOfferSeconds));
            }
            finally
            {
                typeof(GameData).GetMethod("UnloadStatic", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
                Object.DestroyImmediate(asset);
            }
        }

        [TestCase("{\"reviveOfferSeconds\":0}")]
        [TestCase("{\"reviveOfferSeconds\":31}")]
        [TestCase("{\"maxRewardedRevivesPerAttempt\":6}")]
        public void InvalidPuzzlePolicyIsRejected(string policy)
        {
            RemoteConfigController.Init("{\"puzzle\":" + policy + "}");
            LogAssert.Expect(LogType.Warning, "[Remote Config] Validation failed for key 'puzzle'.");
            Assert.That(RemoteConfigController.TryGetConfig<PuzzleRemoteConfigData>("puzzle"), Is.Null);
        }

        [Test]
        public void BootstrapPreparesRemoteConfigBeforeAnEarlierConsumer()
        {
            var root = new GameObject("Initializer test");
            var settings = ScriptableObject.CreateInstance<ProjectInitSettings>();
            var consumer = ScriptableObject.CreateInstance<RemoteConfigConsumerForTests>();
            var remote = ScriptableObject.CreateInstance<RemoteConfigInitModule>();
            var local = new TextAsset("{\"ads\":{\"minLevel\":13}}");
            try
            {
                Configure(remote, "", local);
                var serializedSettings = new SerializedObject(settings);
                var modules = serializedSettings.FindProperty("modules");
                modules.arraySize = 2;
                modules.GetArrayElementAtIndex(0).objectReferenceValue = consumer;
                modules.GetArrayElementAtIndex(1).objectReferenceValue = remote;
                serializedSettings.ApplyModifiedPropertiesWithoutUndo();

                var initializer = root.AddComponent<Initializer>();
                var serializedInitializer = new SerializedObject(initializer);
                serializedInitializer.FindProperty("initSettings").objectReferenceValue = settings;
                serializedInitializer.ApplyModifiedPropertiesWithoutUndo();

                Drain(initializer.InitModulesCoroutine());

                Assert.That(consumer.LevelAtInitialization, Is.EqualTo(13));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(settings);
                Object.DestroyImmediate(consumer);
                Object.DestroyImmediate(remote);
                Object.DestroyImmediate(local);
            }
        }

        [Test]
        public void OfflineStartupUsesCacheAndIsolatesItByEndpoint()
        {
            var module = ScriptableObject.CreateInstance<RemoteConfigSourceForTests>();
            var local = new TextAsset("{\"ads\":{\"minLevel\":3}}");
            var endpoint = new Uri("https://example.invalid/" + Guid.NewGuid());
            string cacheKey = "NebulaSoft.RemoteConfig.v1." + Hash128.Compute(endpoint.AbsoluteUri);
            try
            {
                Configure(module, endpoint.AbsoluteUri, local);
                module.Response = "{\"ads\":{\"minLevel\":21}}";
                Drain(module.Prepare());
                Assert.That(RemoteConfigController.TryGetConfig<AdsRemoteConfigData>("ads").minLevel, Is.EqualTo(21));

                module.Response = "{\"ads\":";
                Snapshot.SetValue(null, null);
                LogAssert.Expect(LogType.Warning, "[Remote Config] Remote payload was invalid. Using fallback configuration.");
                Drain(module.Prepare());
                Assert.That(RemoteConfigController.TryGetConfig<AdsRemoteConfigData>("ads").minLevel, Is.EqualTo(21));

                module.Response = null;
                Configure(module, endpoint.AbsoluteUri + "/other", local);
                Snapshot.SetValue(null, null);
                Drain(module.Prepare());
                Assert.That(RemoteConfigController.TryGetConfig<AdsRemoteConfigData>("ads").minLevel, Is.EqualTo(3));
            }
            finally
            {
                PlayerPrefs.DeleteKey(cacheKey);
                PlayerPrefs.Save();
                Object.DestroyImmediate(local);
                Object.DestroyImmediate(module);
            }
        }

        [Test]
        public void PlainHttpDoesNotFetchAndUsesBundledConfig()
        {
            var module = ScriptableObject.CreateInstance<RemoteConfigSourceForTests>();
            var local = new TextAsset("{\"ads\":{\"minLevel\":3}}");
            try
            {
                Configure(module, "http://example.invalid/config", local);
                LogAssert.Expect(LogType.Warning, "[Remote Config] Endpoint must be a direct HTTPS URL without credentials or a fragment.");
                Drain(module.Prepare());
                Assert.That(module.FetchCount, Is.Zero);
                Assert.That(RemoteConfigController.TryGetConfig<AdsRemoteConfigData>("ads").minLevel, Is.EqualTo(3));
            }
            finally
            {
                Object.DestroyImmediate(local);
                Object.DestroyImmediate(module);
            }
        }

        [Test]
        public void PreparingWithoutSources_ClearsPreviousSessionOverrides()
        {
            var module = ScriptableObject.CreateInstance<RemoteConfigInitModule>();
            try
            {
                RemoteConfigController.Init("{\"flag\":true}");
                Configure(module, "", null);
                Drain(module.Prepare());
                Assert.That(RemoteConfigController.TryGetValue("flag", out bool _), Is.False);
            }
            finally { Object.DestroyImmediate(module); }
        }

        [Test]
        public void Shutdown_ClearsActiveAndBundledSnapshots()
        {
            Bundled.SetValue(null, JObject.Parse("{\"bundled\":true}"));
            RemoteConfigController.Init("{\"remote\":true}");
            var module = ScriptableObject.CreateInstance<RemoteConfigInitModule>();
            try
            {
                module.Shutdown();
                Assert.That(RemoteConfigController.TryGetValue("remote", out bool _), Is.False);
                Assert.That(RemoteConfigController.TryGetValue("bundled", out bool _), Is.False);
            }
            finally { Object.DestroyImmediate(module); }
        }

        [TestCase("{\"defaultCount\":[]}", false)]
        [TestCase("{\"defaultCount\":[{\"Coins\":-1}]}", false)]
        [TestCase("{\"defaultCount\":[{\"Coins\":1},{\"Coins\":2}]}", false)]
        [TestCase("{\"defaultCount\":[{\"Coins\":1,\"Gem\":2}]}", true)]
        [TestCase("{\"defaultCount\":[{\"Coins\":1.5}]}", true)]
        [TestCase("{}", true)]
        public void InvalidCurrencyConfig_FallsBackToBundledBalance(string payload, bool parseFailure)
        {
            Bundled.SetValue(null, JObject.Parse("{\"currencies\":{\"defaultCount\":[{\"Coins\":15}]}}"));
            RemoteConfigController.Init("{\"currencies\":" + payload + "}");
            LogAssert.Expect(LogType.Warning, parseFailure
                ? "[Remote Config] Failed to deserialize key 'currencies'."
                : "[Remote Config] Validation failed for key 'currencies'.");
            var config = RemoteConfigController.TryGetConfig<CurrencyRemoteConfigData>("currencies");
            Assert.That(config.GetCurrencyOverride("Coins").defaultCount, Is.EqualTo(15));
        }

        private static void Configure(RemoteConfigInitModule module, string endpoint, TextAsset localConfig)
        {
            var settings = new SerializedObject(module);
            settings.FindProperty("endpoint").stringValue = endpoint;
            settings.FindProperty("localConfig").objectReferenceValue = localConfig;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Drain(IEnumerator routine)
        {
            while (routine.MoveNext())
                if (routine.Current is IEnumerator nested) Drain(nested);
        }
    }

    public sealed class RemoteConfigConsumerForTests : InitModule
    {
        public override string ModuleName => "Remote Config test consumer";
        public int? LevelAtInitialization { get; private set; }
        public override void CreateComponent()
        {
            LevelAtInitialization = RemoteConfigController.TryGetConfig<AdsRemoteConfigData>("ads")?.minLevel;
        }
    }

    public sealed class RemoteConfigSourceForTests : RemoteConfigInitModule
    {
        public string Response { get; set; }
        public int FetchCount { get; private set; }
        protected override IEnumerator Fetch(Uri url, Action<string> onSuccess)
        {
            FetchCount++;
            yield return null;
            if (Response != null) onSuccess(Response);
        }
    }
}
