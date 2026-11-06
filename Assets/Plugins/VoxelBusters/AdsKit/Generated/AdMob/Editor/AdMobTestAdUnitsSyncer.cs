using System;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using VoxelBusters.AdsKit.Editor;
using VoxelBusters.CoreLibrary;

namespace VoxelBusters.AdsKit.Adapters.Editor
{
    public class AdMobTestAdUnitsSyncer : IPreprocessBuildWithReport
    {
        public int callbackOrder => -1;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (!AdsKitSettingsEditorUtility.SettingsExists)
                return;

            var settings = AdsKitSettingsEditorUtility.DefaultSettings;

            if (settings.IsTestMode)
            {
                UpdateTestAdUnitIds(settings);
            }

        }

        private void UpdateTestAdUnitIds(AdsKitSettings settings)
        {
            AdNetworkSettings networkSettings = null;
            foreach (var each in settings.NetworkSettingsArray)
            {
                if (each.NetworkId.Equals(AdNetworkServiceId.kGoogleMobileAds))
                {
                    networkSettings = each;
                    break;
                }
            }

            if (networkSettings == null)
                return;


            //1. Go through each admeta entry,
            //2. Find the placement
            //3. Get the placement details from the placement meta
            //4. Get the AdType and content options set in placement meta
            //5. Use AdMobUtility.GetTestAdUnitId to get the test ad unit id
            //6. Set the test ad unit id in the admeta's m_testAdUnitIds (create a setter if required)
            foreach (var each in networkSettings.AdMetaArray)
            {
                var placement = each.Placement;
                var placementMeta = Array.Find(settings.PlacementMetaArray, (item) => string.Equals(item.Name, placement));

                if (placementMeta == null)
                    continue;

                var adType = placementMeta.AdType;
                var contentOptions = placementMeta.ContentOptions ?? settings.GetDefaultSettings(adType);

                var testAdUnitId = GetTestAdUnitId(adType, contentOptions);
                each.SetTestAdUnitId(testAdUnitId);
            }
        }

        private RuntimePlatformConstantSet GetTestAdUnitId(AdType adType, AdContentOptions adOptions = null)
        {
            if (adType == AdType.Banner)
            {
                BannerAdOptions bannerOptions = adOptions as BannerAdOptions;
                if (bannerOptions != null)
                {
                    switch (bannerOptions.Size.Type)
                    {
                        case AdSize.AdSizeType.Fixed:
                            return new RuntimePlatformConstantSet(
                                ios: "ca-app-pub-3940256099942544/2934735716",
                                android: "ca-app-pub-3940256099942544/6300978111");
                        case AdSize.AdSizeType.AnchoredAdaptive:
                            return new RuntimePlatformConstantSet(
                                ios: "ca-app-pub-3940256099942544/2435281174",
                                android: "ca-app-pub-3940256099942544/9214589741");
                    }
                }
            }
            else if (adType == AdType.Interstitial)
            {
                return new RuntimePlatformConstantSet(
                    ios: "ca-app-pub-3940256099942544/4411468910",
                    android: "ca-app-pub-3940256099942544/1033173712");
            }
            else if (adType == AdType.RewardedInterstitial)
            {
                return new RuntimePlatformConstantSet(
                    ios: "ca-app-pub-3940256099942544/6978759866",
                    android: "ca-app-pub-3940256099942544/5354046379");
            }
            else if (adType == AdType.RewardedVideo)
            {
                return new RuntimePlatformConstantSet(
                    ios: "ca-app-pub-3940256099942544/1712485313",
                    android: "ca-app-pub-3940256099942544/5224354917");
            }

            return null;
        }
    }
}
