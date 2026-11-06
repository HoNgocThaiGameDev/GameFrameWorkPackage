using System.Collections;
using NebulaSoft.Ads;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace NebulaSoft.Tests
{
    public sealed class AdsDummyPlayModeTests
    {
        [UnityTest]
        public IEnumerator DummyUi_BannerRewardAndCloseUseTheServiceLifecycle()
        {
            yield return new EnterPlayMode();
            var runInBackground = Application.runInBackground;
            Application.runInBackground = true;
            var provider = new DummyAdsProvider(BannerPosition.Bottom);
            var banner = new AdPlacementDefinition("banner", AdFormat.Banner);
            var rewarded = new AdPlacementDefinition("rewarded", AdFormat.Rewarded);
            var interstitial = new AdPlacementDefinition("interstitial", AdFormat.Interstitial);
            using (var service = new AdsService(provider, new[] { banner, rewarded, interstitial }, () => Time.realtimeSinceStartupAsDouble))
            {
                Assert.That(service.InitializeAsync().Result, Is.True);
                service.ShowBanner(banner.Id);
                yield return null;
                Assert.That(service.Banner.IsVisible, Is.True);
                Assert.That(service.Banner.TopEdge, Is.GreaterThan(0));
                var controller = Object.FindFirstObjectByType<AdDummyController>();
                Assert.That(controller, Is.Not.Null);
                var reward = service.ShowAsync(rewarded.Id);
                controller.GetRewardButton();
                Assert.That(reward.Result.RewardGranted, Is.True);
                var skipped = service.ShowAsync(rewarded.Id);
                controller.CloseRewardedVideoButton();
                Assert.That(skipped.Result.RewardGranted, Is.False);
                var forced = service.ShowAsync(interstitial.Id);
                controller.CloseInterstitialButton();
                Assert.That(forced.Result.Succeeded, Is.True);
                service.SetForcedAdsDisabled(true);
                Assert.That(service.Banner.IsVisible, Is.False);
                Assert.That(service.ShowBanner(banner.Id), Is.False);
            }
            double deadline = EditorApplication.timeSinceStartup + 3;
            while (Object.FindFirstObjectByType<AdDummyController>() != null && EditorApplication.timeSinceStartup < deadline)
            {
                EditorApplication.QueuePlayerLoopUpdate();
                yield return null;
            }
            Application.runInBackground = runInBackground;
            Assert.That(Object.FindFirstObjectByType<AdDummyController>() == null, Is.True);
            yield return new ExitPlayMode();
        }

        [UnityTearDown]
        public IEnumerator ReturnToEditMode()
        {
            if (Application.isPlaying) yield return new ExitPlayMode();
        }
    }
}
