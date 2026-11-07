using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using NebulaSoft.Ads;
using UnityEngine;

namespace NebulaSoft
{
    public struct BannerLayoutInfo
    {
        public static readonly BannerLayoutInfo Hidden = new BannerLayoutInfo(AdProvider.Disable, false, Vector2Int.zero, 0);
        public AdProvider Provider { get; }
        public bool IsVisible { get; }
        public Vector2Int PixelSize { get; }
        public float TopEdge { get; }
        public float Height => IsVisible ? PixelSize.y : 0;
        public BannerLayoutInfo(AdProvider provider, bool isVisible, Vector2Int pixelSize, float topEdge)
        {
            Provider = provider;
            IsVisible = isVisible && pixelSize.x > 0 && pixelSize.y > 0;
            PixelSize = IsVisible ? pixelSize : Vector2Int.zero;
            TopEdge = IsVisible ? Mathf.Max(pixelSize.y, topEdge) : 0;
        }
    }

    /// <summary>Compatibility facade. New gameplay receives IAdsService; SDK selection belongs to AdsModuleSettings.</summary>
    [StaticUnload]
    public static class AdsManager
    {
        private static AdsModule module;
        private static AdsModuleSettings configuration;
        private static AdsSettings settings;
        private static bool bannerActive = true, remoteBannerEnabled = true, remoteInterstitialEnabled = true;
        private static bool legacyShowPending;
        private static double nextInterstitial;
        private static float interstitialDelay;
        private static readonly ConcurrentQueue<SimpleCallback> callbacks = new ConcurrentQueue<SimpleCallback>();
        public static AdsSettings Settings => settings;
        public static IAdsService Service => module != null ? module.Service : null;
        public static bool IsBannerActive => bannerActive;
        public static BannerLayoutInfo BannerLayout { get; private set; } = BannerLayoutInfo.Hidden;
        public static float BannerHeight => BannerLayout.Height;
        public static event SimpleCallback Configured, Initialized, ForcedAdDisabled, RewardedVideoRewarded, ErrorMessageRequested;
        public static event Action<bool> NoAdsEntitlementChanged;
        public static event Action<BannerLayoutInfo> BannerLayoutChanged;
        public static event Action<bool, string> LoadingPresentationChanged;
        public static event Action<string, bool> AnalyticsRequested;
        public static event AdsModuleCallback AdProviderInitialized;
        public static event AdsEventsCallback AdLoaded, AdDisplayed, AdClosed;
        public static AdsBoolCallback InterstitialConditions;
        public static AdsBoolCallback RewardedVideoConditions;

        public static void ConfigureModule(AdsModuleSettings moduleSettings)
        {
            if (module == null) configuration = moduleSettings;
        }

        public static void Init(MonetizationSettings monetizationSettings)
        {
            if (module != null) return;
            settings = monetizationSettings != null ? monetizationSettings.AdsSettings : null;
            if (configuration == null && settings != null) configuration = settings.ModuleSettings;
            if (configuration == null || !configuration.IsEnabled) return;
            if (!SaveController.IsSaveLoaded || Initializer.GameObject == null)
            { Debug.LogError("[Ads]: Initialize Save and Initializer before Ads."); return; }
            var errors = configuration.ValidateSettings();
            if (errors.Count > 0)
            { Debug.LogError("[Ads]: Invalid configuration:\n" + string.Join("\n", errors), configuration); return; }
            try
            {
                module = Initializer.GameObject.AddComponent<AdsModule>();
                module.Configure(configuration);
                Service.StatusChanged += OnStatus;
                Service.AdEvent += OnAdEvent;
                Service.BannerChanged += OnBanner;
                module.Save.NoAdsChanged += OnNoAdsChanged;
                module.Save.ForcedAdsDisabled += OnForcedAdsDisabled;
                module.Save.ForcedAdsAvailabilityChanged += OnForcedAdsAvailabilityChanged;
                interstitialDelay = settings != null ? settings.InterstitialShowingDelay : 0;
                bool firstLaunch = !PlayerPrefs.HasKey("FIRST_LAUNCH");
                float initialDelay = settings == null ? 0 : firstLaunch ? settings.InterstitialFirstStartDelay : settings.InterstitialStartDelay;
                if (firstLaunch) PlayerPrefs.SetInt("FIRST_LAUNCH", 1);
                var remote = RemoteConfigController.TryGetConfig<AdsRemoteConfigData>("ads");
                if (remote != null)
                {
                    remoteBannerEnabled = remote.useBanner;
                    remoteInterstitialEnabled = remote.useInterstitials;
                    interstitialDelay = remote.intDelay;
                    initialDelay = firstLaunch ? remote.intFSDelay : remote.intSDelay;
                }
                SetInterstitialDelayTime(initialDelay);
                Configured?.Invoke();
                InitializeProvider();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (module != null) { module.Shutdown(); UnityEngine.Object.Destroy(module); module = null; }
            }
        }

        private static async void InitializeProvider()
        {
            var owner = module;
            bool ready = await owner.InitializeAsync();
            if (!ready || module != owner || owner.Service == null) return;
            if (settings == null || settings.LoadAdsOnStart) TryToLoadFirstAds();
        }

        private static AdPlacement Placement(AdFormat format) => configuration == null ? null :
            format == AdFormat.Banner ? configuration.Banner : format == AdFormat.Interstitial ? configuration.Interstitial : configuration.Rewarded;
        private static bool Ready(AdFormat format) => Service != null && Service.GetState(Placement(format)) == AdState.Ready;
        public static bool IsInterstitialLoaded() => remoteInterstitialEnabled && IsForcedAdEnabled() && Ready(AdFormat.Interstitial);
        public static bool IsRewardBasedVideoLoaded() => Ready(AdFormat.Rewarded);
        public static void RequestInterstitial() { if (remoteInterstitialEnabled) Service?.Load(Placement(AdFormat.Interstitial)); }
        public static void RequestRewardBasedVideo() => Service?.Load(Placement(AdFormat.Rewarded));
        public static void TryToLoadFirstAds() { RequestRewardBasedVideo(); RequestInterstitial(); ShowBanner(); }

        public static void ShowInterstitial(AdProviderHandler.AdvertisementCallback callback, string analyticsEvent = "Default", bool ignoreConditions = false)
        {
            if (!remoteInterstitialEnabled || !ignoreConditions && (Time.realtimeSinceStartupAsDouble < nextInterstitial || !CheckExtraInterstitialCondition()))
            { callback?.Invoke(false); return; }
            ShowLegacy(AdFormat.Interstitial, callback, analyticsEvent, false);
        }
        public static void ShowRewardBasedVideo(AdProviderHandler.AdvertisementCallback callback, string analyticsEvent = "Default", bool showErrorMessage = true)
        {
            if (RewardedVideoConditions != null)
                foreach (AdsBoolCallback condition in RewardedVideoConditions.GetInvocationList())
                {
                    try { if (condition()) continue; }
                    catch (Exception exception) { Debug.LogException(exception); }
                    callback?.Invoke(false);
                    return;
                }
            ShowLegacy(AdFormat.Rewarded, callback, analyticsEvent, showErrorMessage);
        }

        private static async void ShowLegacy(AdFormat format, AdProviderHandler.AdvertisementCallback callback, string analyticsEvent, bool showError)
        {
            if (Service == null || legacyShowPending || Service.IsFullscreenBusy)
            { callback?.Invoke(false); return; }
            var owner = module;
            legacyShowPending = true;
            bool loadingShown = false, completed = false;
            try
            {
                if (settings != null && settings.LoadingAdDuration > 0 && Ready(format))
                {
                    loadingShown = true;
                    LoadingPresentationChanged?.Invoke(true, settings.LoadingMessage);
                    await Task.Delay(Mathf.RoundToInt(settings.LoadingAdDuration * 1000));
                    loadingShown = false;
                    LoadingPresentationChanged?.Invoke(false, null);
                }
                if (module != owner || owner.Service == null) return;
                AnalyticsRequested?.Invoke(analyticsEvent, format == AdFormat.Rewarded);
                var result = await owner.Service.ShowAsync(Placement(format));
                if (module != owner || owner.Service == null) return;
                legacyShowPending = false;
                if (showError && (result.Outcome == AdOutcome.NotReady || result.Outcome == AdOutcome.Failed)) ShowErrorMessage();
                completed = true;
                callback?.Invoke(result.Succeeded);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (!completed && module == owner) callback?.Invoke(false);
            }
            finally
            {
                if (module == owner)
                {
                    legacyShowPending = false;
                    if (loadingShown) LoadingPresentationChanged?.Invoke(false, null);
                }
            }
        }

        public static void SetInterstitialDelayTime(float time) => nextInterstitial = Time.realtimeSinceStartupAsDouble + Mathf.Max(0, time);
        public static void ResetInterstitialDelayTime() => SetInterstitialDelayTime(interstitialDelay);
        public static bool CheckExtraInterstitialCondition()
        {
            if (InterstitialConditions != null)
                foreach (AdsBoolCallback condition in InterstitialConditions.GetInvocationList()) if (!condition()) return false;
            return true;
        }
        public static void ShowBanner()
        {
            if (bannerActive && remoteBannerEnabled && Service != null && UnityEngine.Object.FindFirstObjectByType<LoadingGraphics>() == null)
                Service.ShowBanner(Placement(AdFormat.Banner));
        }
        public static void HideBanner() => Service?.HideBanner();
        public static void DestroyBanner() => Service?.HideBanner(true);
        public static void EnableBanner() { bannerActive = true; ShowBanner(); }
        public static void DisableBanner() { bannerActive = false; HideBanner(); }
        public static float GetBannerHeight() => BannerLayout.Height;
        public static float GetBannerTopEdge() => BannerLayout.TopEdge;
        public static bool IsForcedAdEnabled() => module == null || module.Save.ForcedAdsEnabled;
        public static void DisableForcedAdForever() => module?.Save.SetNoAds(true);
        public static void DisableForcedAd(int seconds) => module?.Save.DisableFor(seconds);
        public static void SetNoAdsEntitlement(bool noAds) => module?.Save.SetNoAds(noAds);
        private static void OnForcedAdsDisabled() => ForcedAdDisabled?.Invoke();
        private static void OnForcedAdsAvailabilityChanged(bool enabled) { if (enabled) ShowBanner(); }
        private static void OnNoAdsChanged(bool noAds) { NoAdsEntitlementChanged?.Invoke(noAds); if (!noAds) ShowBanner(); }

        private static AdProvider Provider(string id)
        {
            if (Enum.TryParse(id, true, out AdProvider provider)) return provider;
            return settings != null ? settings.BannerType : AdProvider.Disable;
        }
        private static void OnStatus(AdsStatus status)
        {
            if (status != AdsStatus.Ready) return;
            Initialized?.Invoke();
            AdProviderInitialized?.Invoke(Provider(module.ProviderId));
        }
        private static void OnAdEvent(AdEvent value)
        {
            var provider = Provider(value.ProviderId);
            var format = (AdType)value.Format;
            if (value.Kind == AdEventKind.Loaded) AdLoaded?.Invoke(provider, format);
            else if (value.Kind == AdEventKind.RewardGranted) RewardedVideoRewarded?.Invoke();
            else if (value.Kind == AdEventKind.Started) { AdDisplayed?.Invoke(provider, format); if (format != AdType.Banner) ResetInterstitialDelayTime(); }
            else if (value.Kind == AdEventKind.Closed) { AdClosed?.Invoke(provider, format); if (format != AdType.Banner) ResetInterstitialDelayTime(); }
        }
        private static void OnBanner(BannerInfo info)
        {
            BannerLayout = info.IsVisible ? new BannerLayoutInfo(Provider(info.ProviderId), true, new Vector2Int(info.Width, info.Height), info.TopEdge) : BannerLayoutInfo.Hidden;
            BannerLayoutChanged?.Invoke(BannerLayout);
        }
        public static bool IsModuleEnabled(AdProvider provider) => configuration != null && configuration.IsEnabled &&
            provider != AdProvider.Disable && module != null && Provider(module.ProviderId) == provider;
        public static bool IsModuleActive(AdProvider provider) => IsModuleEnabled(provider);
        public static bool IsModuleInititalized(AdProvider provider) => IsModuleActive(provider) && Service.Status == AdsStatus.Ready;
        [Obsolete("Use IAdsService. Native provider access is outside the module's public API.")]
        public static AdProviderHandler GetAdProvider(AdProvider provider) => null;
        public static void ShowErrorMessage() => ErrorMessageRequested?.Invoke();
        public static void CallEventInMainThread(SimpleCallback callback) { if (callback != null) callbacks.Enqueue(callback); }
        internal static void TickCompatibility()
        {
            while (callbacks.TryDequeue(out var callback))
                try { callback(); } catch (Exception exception) { Debug.LogException(exception); }
        }

        // Retained for source compatibility with retired handlers; new providers emit per-request events.
        public static void ResetBannerHeight() => ResetBannerLayout();
        public static void ResetBannerLayout() => OnBanner(BannerInfo.Hidden);
        public static void SetBannerHeight(float height) => SetBannerLayout(settings != null ? settings.BannerType : AdProvider.Disable, new Vector2Int(Screen.width, Mathf.CeilToInt(height)));
        public static void SetBannerLayout(AdProvider provider, Vector2Int pixels, float bottomInset = 0) => OnBanner(new BannerInfo(provider.ToString(), pixels.x, pixels.y, bottomInset));
        public static void OnProviderInitialized(AdProvider provider) => AdProviderInitialized?.Invoke(provider);
        public static void OnProviderAdLoaded(AdProvider provider, AdType type) => AdLoaded?.Invoke(provider, type);
        public static void OnProviderAdDisplayed(AdProvider provider, AdType type) => AdDisplayed?.Invoke(provider, type);
        public static void OnProviderAdClosed(AdProvider provider, AdType type) => AdClosed?.Invoke(provider, type);
        [Obsolete("Providers must return per-request results through IAdsProvider.")]
        public static void ExecuteInterstitialCallback(bool result) { }
        [Obsolete("Providers must return per-request results through IAdsProvider.")]
        public static void ExecuteRewardVideoCallback(bool result) { }

        public static void Shutdown() => UnloadStatic();

        private static void UnloadStatic()
        {
            if (module != null) module.Shutdown();
            module = null; configuration = null; settings = null;
            legacyShowPending = false; bannerActive = true; remoteBannerEnabled = true; remoteInterstitialEnabled = true;
            nextInterstitial = 0; interstitialDelay = 0; BannerLayout = BannerLayoutInfo.Hidden;
            while (callbacks.TryDequeue(out _)) { }
            Configured = null; Initialized = null; ForcedAdDisabled = null; NoAdsEntitlementChanged = null;
            RewardedVideoRewarded = null; BannerLayoutChanged = null; ErrorMessageRequested = null;
            LoadingPresentationChanged = null; AnalyticsRequested = null;
            AdProviderInitialized = null; AdLoaded = null; AdDisplayed = null; AdClosed = null; InterstitialConditions = null;
            RewardedVideoConditions = null;
        }
        public delegate void AdsModuleCallback(AdProvider provider);
        public delegate void AdsEventsCallback(AdProvider provider, AdType type);
        public delegate bool AdsBoolCallback();
    }
}
