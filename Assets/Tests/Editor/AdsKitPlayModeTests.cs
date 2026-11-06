#if MODULE_ADSKIT && MODULE_ADMOB
using System;
using System.Collections;
using System.Linq;
using System.Threading;
using NebulaSoft.Ads;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace NebulaSoft.Tests
{
    public sealed class AdsKitPlayModeTests
    {
        [UnityTest]
        public IEnumerator InstalledSdk_InitializesLoadsFormatsAndClearsBannerForNoAds()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android && EditorUserBuildSettings.activeBuildTarget != BuildTarget.iOS)
                Assert.Ignore("SDK simulation needs an Android or iOS build target.");
            yield return new EnterPlayMode();
            Application.runInBackground = true;
            EditorApplication.isPaused = false;
            Time.timeScale = 1;
            var config = AssetDatabase.LoadAssetAtPath<AdsModuleSettings>("Assets/Project Files/Data/Ads/AdsModuleSettings.asset");
            Assert.That(config.Provider, Is.TypeOf<AdsKitProviderConfig>());
            Assert.That(config.ValidateSettings(), Is.Empty);
            Assert.That(VoxelBusters.AdsKit.AdsKitSettings.Instance.ForceTestMode, Is.True);
            // Enable all formats for this test without changing the template's placement assets.
            var definitions = config.Catalog.CreateDefinitions().Select(p => new AdPlacementDefinition(p.Id, p.Format, true, true)).ToArray();
            var failures = new System.Collections.Generic.List<string>();
            using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
            using (var service = new AdsService(config.Provider.CreateProvider(), definitions, () => Time.realtimeSinceStartupAsDouble, error => failures.Add(error.ToString())))
            {
                var init = service.InitializeAsync(timeout.Token);
                while (!init.IsCompleted && !timeout.IsCancellationRequested) { EditorApplication.QueuePlayerLoopUpdate(); service.Tick(); yield return null; }
                Assert.That(init.IsCompleted, Is.True, "SDK initialization timed out. SDK ready: " + VoxelBusters.AdsKit.AdsManager.IsInitialised + "; frames: " + Time.frameCount);
                Assert.That(init.Result, Is.True, "SDK initialization failed. " + string.Join("\n", failures));
                service.ShowBanner(config.Banner);
                while ((VoxelBusters.AdsKit.AdsManager.GetAdPlacementState(config.Banner.Id) != VoxelBusters.AdsKit.AdPlacementState.Showing || service.GetState(config.Interstitial) != AdState.Ready ||
                        service.GetState(config.Rewarded) != AdState.Ready) && !timeout.IsCancellationRequested)
                { EditorApplication.QueuePlayerLoopUpdate(); service.Tick(); yield return null; }
                Assert.That(service.GetState(config.Interstitial), Is.EqualTo(AdState.Ready), "SDK state: " + VoxelBusters.AdsKit.AdsManager.GetAdPlacementState(config.Interstitial.Id) + "; frame " + Time.frameCount + "; " + string.Join("\n", failures));
                Assert.That(service.GetState(config.Rewarded), Is.EqualTo(AdState.Ready), "SDK state: " + VoxelBusters.AdsKit.AdsManager.GetAdPlacementState(config.Rewarded.Id));
                Assert.That(VoxelBusters.AdsKit.AdsManager.GetAdPlacementState(config.Banner.Id), Is.EqualTo(VoxelBusters.AdsKit.AdPlacementState.Showing));
                service.Tick();
                var size = VoxelBusters.AdsKit.AdsManager.AdMob.GetAdViewProxy(config.Banner.Id).Size;
                if (size.HasValue && size.Value.x > 0 && size.Value.y > 0)
                {
                    Assert.That(service.Banner.IsVisible, Is.True);
                    Assert.That(service.Banner.Height, Is.EqualTo(size.Value.y));
                }
                else Assert.That(service.Banner.IsVisible, Is.False, "Unknown SDK pixel geometry must not reserve an invented banner height.");
                service.SetForcedAdsDisabled(true);
                Assert.That(service.Banner.IsVisible, Is.False);
                Assert.That(service.ShowBanner(config.Banner), Is.False);
                Assert.That(service.GetState(config.Rewarded), Is.EqualTo(AdState.Ready));
            }
            yield return new ExitPlayMode();
        }

        [UnityTearDown]
        public IEnumerator ReturnToEditMode()
        {
            if (Application.isPlaying) yield return new ExitPlayMode();
        }
    }
}
#endif
