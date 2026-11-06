#if MODULE_ADSKIT
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NebulaSoft.Ads;
using UnityEngine;
using VoxelBusters.AdsKit;
using VoxelBusters.AdsKit.Adapters;
using VoxelBusters.CoreLibrary;
using SdkAds = VoxelBusters.AdsKit.AdsManager;
using SdkAdType = VoxelBusters.AdsKit.AdType;

namespace NebulaSoft
{
    /// <summary>All SDK API translation lives here. Uses public APIs only; does not modify vendor files.</summary>
    public sealed class AdsKitProvider : IAdsProvider, IAdLifecycleEventListener
    {
        private readonly Dictionary<string, AdsKitProviderConfig.PlacementMapping> mappings = new Dictionary<string, AdsKitProviderConfig.PlacementMapping>(StringComparer.Ordinal);
        private readonly Dictionary<string, AdNetworkAdapter> networks = new Dictionary<string, AdNetworkAdapter>(StringComparer.Ordinal);
        private readonly AdsKitConsentProviderConfig consent;
        private readonly List<Action> detachContentHandlers = new List<Action>();
        private TaskCompletionSource<bool> initialization;
        private AdRequest? banner;
        private BannerAdContent bannerContent;
        private bool disposed, registered, bannerRequested, bannerShowing, bannerHidden;
        private bool bannerImpressionReported;
        public string Id => "AdsKit";
        public int CallbackOrder => 0;
        public event Action<AdEvent> AdEvent;
        public event Action<BannerInfo> BannerChanged;

        public AdsKitProvider(IEnumerable<AdsKitProviderConfig.PlacementMapping> mappings, AdsKitConsentProviderConfig consent)
        {
            this.consent = consent;
            foreach (var mapping in mappings) this.mappings.Add(mapping.Placement.Id, mapping);
        }

        public async Task<bool> InitializeAsync(CancellationToken cancellation)
        {
            if (disposed || cancellation.IsCancellationRequested) return false;
            if (!registered) { SdkAds.RegisterListener(this); registered = true; }
            if (SdkAds.IsInitialised)
            {
                CacheNetwork(SdkAds.AdMob); CacheNetwork(SdkAds.AppLovin); CacheNetwork(SdkAds.LevelPlay);
                return true;
            }
            var completion = new TaskCompletionSource<bool>();
            initialization = completion;
            using (cancellation.Register(() => completion.TrySetCanceled()))
            {
                var operation = SdkAds.Initialise(consent.CreateProvider());
                if (operation == null) return false;
                operation.OnComplete += completed =>
                {
                    if (disposed) return;
                    if (completed.Error == null && completed.Result != null)
                        foreach (var network in completed.Result.SubscribedAdNetworks) CacheNetwork(network);
                    completion.TrySetResult(completed.Error == null && SdkAds.IsInitialised);
                };
                if (operation.IsDone)
                {
                    if (operation.Result != null)
                        foreach (var network in operation.Result.SubscribedAdNetworks) CacheNetwork(network);
                    completion.TrySetResult(operation.Error == null && SdkAds.IsInitialised);
                }
                return await completion.Task;
            }
        }

        private void CacheNetwork(AdNetworkAdapter network) { if (network != null) networks[network.NetworkId] = network; }
        private string Resolve(AdPlacementDefinition placement) => mappings[placement.Id].SdkPlacement;

        public AdState GetState(AdPlacementDefinition placement)
        {
            if (disposed || !SdkAds.IsInitialised) return AdState.Unavailable;
            switch (SdkAds.GetAdPlacementState(Resolve(placement)))
            {
#if NEBULASOFT_ADSKIT_LEGACY_STATES
                case AdPlacementState.Ready: return AdState.Ready;
                case AdPlacementState.Waiting: return AdState.Loading;
#else
                case AdPlacementState.ReadyToShow: return AdState.Ready;
                case AdPlacementState.Loading: return AdState.Loading;
                case AdPlacementState.Showing: return AdState.Showing;
#endif
                default: return AdState.Unavailable;
            }
        }

        public void Load(AdPlacementDefinition placement) => SdkAds.LoadAd(Resolve(placement));

        public void Show(AdRequest request)
        {
            var content = SdkAds.ShowAd(Resolve(request.Placement));
            if (content == null) { Emit(request, AdEventKind.ShowFailed, null, "SDK did not accept the show request."); return; }
            bool started = false, ended = false, impressed = false;
            networks.TryGetValue(content.ProviderId ?? "", out var network);
            AdNetworkAdapter.AdImpressionRecordedDelegate onImpression = (sender, info) =>
            {
                if (disposed || ended || impressed || !content.IsShowing || info.Placement != content.Placement || info.AdUnitId != content.AdUnitId) return;
                impressed = true;
                Emit(request, AdEventKind.Impression, content.ProviderId);
            };
            Callback<AdContent> onStart = value => { if (disposed || started) return; started = true; Emit(request, AdEventKind.Started, value.ProviderId); };
            Callback<AdContent> onClick = value => { if (!disposed && !ended) Emit(request, AdEventKind.Clicked, value.ProviderId); };
            Callback<(AdContent content, AdReward reward)> onReward = value =>
            { if (!disposed && !ended) Emit(request, AdEventKind.RewardGranted, value.content.ProviderId); };
            Action detach = null;
            Callback<AdContent> onFinish = value =>
            { if (disposed || ended) return; ended = true; detach(); Emit(request, AdEventKind.Closed, value.ProviderId); };
            Callback<AdContent> onFail = value =>
            { if (disposed || ended) return; ended = true; detach(); Emit(request, AdEventKind.ShowFailed, value.ProviderId, value.Error?.ToString()); };
            detach = () =>
            {
                content.OnAdStart -= onStart; content.OnAdClick -= onClick; content.OnAdReward -= onReward;
                content.OnAdFinish -= onFinish; content.OnAdFail -= onFail;
                if (network != null) network.OnAdImpressionRecorded -= onImpression;
                detachContentHandlers.Remove(detach);
            };
            detachContentHandlers.Add(detach);
            content.OnAdStart += onStart; content.OnAdClick += onClick; content.OnAdReward += onReward;
            content.OnAdFinish += onFinish; content.OnAdFail += onFail;
            if (network != null) network.OnAdImpressionRecorded += onImpression;
            if (content.IsShowing) onStart(content);
            if (content.IsDone) { if (content.Error != null) onFail(content); else onFinish(content); }
        }

        public void ShowBanner(AdRequest request)
        {
            banner = request;
            bannerImpressionReported = false;
            bannerRequested = true;
            if ((bannerContent != null && bannerContent.Placement == Resolve(request.Placement) && bannerHidden) || GetState(request.Placement) == AdState.Ready) ShowLoadedBanner();
            else if (GetState(request.Placement) != AdState.Loading) Load(request.Placement);
        }

        private void ShowLoadedBanner()
        {
            if (disposed || !bannerRequested || !banner.HasValue || bannerShowing) return;
            var content = SdkAds.ShowAd(Resolve(banner.Value.Placement)) as BannerAdContent;
            if (content == null) { var request = banner.Value; bannerRequested = false; Emit(request, AdEventKind.ShowFailed, null, "SDK did not accept the banner request."); return; }
            bannerContent = content;
            bannerHidden = false;
            if (content.IsShowing) { bannerShowing = true; RefreshBannerGeometry(); }
        }

        public void HideBanner(bool destroy)
        {
            bannerRequested = false;
            bannerShowing = false;
            var previous = banner;
            banner = null;
            if (bannerContent != null)
            {
                if (bannerContent.IsShowing) bannerContent.HideAd(destroy);
                else if (destroy && bannerHidden && networks.TryGetValue(bannerContent.ProviderId ?? "", out var network))
                    network.HideBanner(bannerContent.Placement, true);
                bannerHidden = !destroy;
                if (destroy) bannerContent = null;
            }
            BannerChanged?.Invoke(BannerInfo.Hidden);
            if (previous.HasValue) Emit(previous.Value, AdEventKind.Closed, null);
        }

        private void RefreshBannerGeometry()
        {
            if (!bannerRequested || !bannerShowing || !banner.HasValue || bannerContent == null) return;
            if (networks.TryGetValue(bannerContent.ProviderId ?? "", out var network))
            {
                var size = network.GetAdViewProxy(Resolve(banner.Value.Placement))?.Size;
                if (size.HasValue) BannerChanged?.Invoke(new BannerInfo(network.NetworkId, size.Value.x, size.Value.y));
            }
            // No guessed 320x50 size: UI moves only when the SDK reports actual geometry.
        }

        public void Tick() { if (!disposed) RefreshBannerGeometry(); }
        private void Emit(AdRequest request, AdEventKind kind, string network, string error = null) =>
            AdEvent?.Invoke(new AdEvent(request.Id, request.Placement.Id, request.Placement.Format, kind, network ?? Id, error));

        public void OnInitialisationComplete(InitialiseResult result)
        {
            if (disposed) return;
            foreach (var network in result.SubscribedAdNetworks) CacheNetwork(network);
            initialization?.TrySetResult(true);
        }
        public void OnInitialisationFail(Error error) { if (!disposed) initialization?.TrySetResult(false); }
        public void OnLoadAdComplete(string placement, LoadAdResult result)
        {
            if (disposed) return;
            BroadcastLoad(placement, AdEventKind.Loaded, result.PreferredAdProvider, null);
            if (bannerRequested && banner.HasValue && Resolve(banner.Value.Placement) == placement) ShowLoadedBanner();
        }
        public void OnLoadAdFail(string placement, Error error)
        {
            if (disposed) return;
            BroadcastLoad(placement, AdEventKind.LoadFailed, Id, error?.ToString());
            // Release the banner request after a failed load so an explicit ShowBanner can retry.
            CloseBannerFromSdk(placement, error?.ToString() ?? "Banner load failed.");
        }
        private void BroadcastLoad(string sdkPlacement, AdEventKind kind, string network, string error)
        {
            foreach (var mapping in mappings.Values)
                if (mapping.SdkPlacement == sdkPlacement) AdEvent?.Invoke(new AdEvent(Guid.Empty, mapping.Placement.Id, mapping.Placement.Format, kind, network ?? Id, error));
        }
        public void OnShowAdStart(string placement)
        {
            if (disposed || !bannerRequested || !banner.HasValue || Resolve(banner.Value.Placement) != placement) return;
            bannerShowing = true;
            Emit(banner.Value, AdEventKind.Started, bannerContent?.ProviderId);
        }
        public void OnShowAdComplete(string placement, ShowAdResult result) => CloseBannerFromSdk(placement, null);
        public void OnShowAdFail(string placement, Error error) => CloseBannerFromSdk(placement, error?.ToString());
        private void CloseBannerFromSdk(string placement, string error)
        {
            if (disposed || !bannerRequested || !banner.HasValue || Resolve(banner.Value.Placement) != placement) return;
            var request = banner.Value;
            bannerRequested = false; bannerShowing = false; banner = null;
            BannerChanged?.Invoke(BannerInfo.Hidden);
            Emit(request, error == null ? AdEventKind.Closed : AdEventKind.ShowFailed, null, error);
        }
        // Fullscreen reward/close callbacks use per-content handlers above, so late callbacks retain their original request ID.
        public void OnShowAdClick(string placement) { }
        public void OnAdReward(string placement, AdReward reward) { }
        public void OnAdImpressionRecorded(string placement)
        {
            if (disposed || !bannerRequested || !bannerShowing || !banner.HasValue || bannerImpressionReported ||
                bannerContent == null || !bannerContent.IsShowing || Resolve(banner.Value.Placement) != placement) return;
            bannerImpressionReported = true;
            Emit(banner.Value, AdEventKind.Impression, bannerContent.ProviderId);
        }
        public void OnAdPaid(string placement, AdTransaction transaction) { }

        public static bool MatchesFormat(AdFormat format, SdkAdType sdkType) =>
            format == AdFormat.Banner && sdkType == SdkAdType.Banner ||
            format == AdFormat.Interstitial && sdkType == SdkAdType.Interstitial ||
            format == AdFormat.Rewarded && sdkType == SdkAdType.RewardedVideo;

        public void Dispose()
        {
            if (disposed) return;
            HideBanner(true);
            disposed = true;
            foreach (var detach in detachContentHandlers.ToArray()) detach();
            if (registered) SdkAds.UnregisterListener(this);
            registered = false;
            initialization?.TrySetResult(false);
            AdEvent = null; BannerChanged = null;
        }
    }
}
#endif
