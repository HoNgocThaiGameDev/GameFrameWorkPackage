using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NebulaSoft.Ads;
using UnityEngine;

namespace NebulaSoft
{
    public sealed class DummyAdsProvider : IAdsProvider
    {
        private readonly BannerPosition position;
        private readonly HashSet<string> ready = new HashSet<string>();
        private AdDummyController controller;
        private AdRequest? fullscreen, banner;
        private bool disposed;
        public string Id => "Dummy";
        public event Action<AdEvent> AdEvent;
        public event Action<BannerInfo> BannerChanged;

        public DummyAdsProvider(BannerPosition position) { this.position = position; }

        public Task<bool> InitializeAsync(CancellationToken cancellation)
        {
            if (disposed || cancellation.IsCancellationRequested) return Task.FromResult(false);
            if (controller != null) return Task.FromResult(true);
            controller = AdDummyController.CreateObject();
            controller.Init(position);
            controller.InterstitialClosed += OnInterstitialClosed;
            controller.RewardedClosed += OnRewardedClosed;
            controller.BannerGeometryChanged += OnBannerGeometry;
            return Task.FromResult(true);
        }

        public AdState GetState(AdPlacementDefinition placement) => disposed || controller == null ? AdState.Unavailable :
            fullscreen?.Placement.Id == placement.Id ? AdState.Showing : ready.Contains(placement.Id) ? AdState.Ready : AdState.Unavailable;

        public void Load(AdPlacementDefinition placement)
        {
            ready.Add(placement.Id);
            AdEvent?.Invoke(new AdEvent(Guid.Empty, placement.Id, placement.Format, AdEventKind.Loaded, Id));
        }

        public void Show(AdRequest request)
        {
            fullscreen = request;
            ready.Remove(request.Placement.Id);
            if (request.Placement.Format == AdFormat.Rewarded) controller.ShowRewardedVideo();
            else controller.ShowInterstitial();
            Emit(request, AdEventKind.Started);
        }

        public void ShowBanner(AdRequest request)
        {
            banner = request;
            controller.ShowBanner();
            Emit(request, AdEventKind.Started);
        }

        private void OnBannerGeometry(BannerInfo info) => BannerChanged?.Invoke(info);
        public void HideBanner(bool destroy)
        {
            var previous = banner;
            banner = null;
            controller?.HideBanner();
            BannerChanged?.Invoke(BannerInfo.Hidden);
            if (previous.HasValue) Emit(previous.Value, AdEventKind.Closed);
        }

        private void OnInterstitialClosed() => CloseFullscreen(false);
        private void OnRewardedClosed(bool rewarded) => CloseFullscreen(rewarded);

        private void CloseFullscreen(bool rewarded)
        {
            if (!fullscreen.HasValue) return;
            var request = fullscreen.Value;
            fullscreen = null;
            if (rewarded) Emit(request, AdEventKind.RewardGranted);
            Emit(request, AdEventKind.Closed);
            if (!disposed) Load(request.Placement);
        }

        private void Emit(AdRequest request, AdEventKind kind) =>
            AdEvent?.Invoke(new AdEvent(request.Id, request.Placement.Id, request.Placement.Format, kind, Id));

        public void Tick() { }
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            if (controller != null)
            {
                controller.InterstitialClosed -= OnInterstitialClosed;
                controller.RewardedClosed -= OnRewardedClosed;
                controller.BannerGeometryChanged -= OnBannerGeometry;
                if (Application.isPlaying) UnityEngine.Object.Destroy(controller.gameObject);
                else UnityEngine.Object.DestroyImmediate(controller.gameObject);
            }
            controller = null;
            fullscreen = null;
            banner = null;
            ready.Clear();
            AdEvent = null;
            BannerChanged = null;
        }
    }
}
