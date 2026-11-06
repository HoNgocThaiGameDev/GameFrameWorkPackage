using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NebulaSoft.Ads
{
    /// <summary>SDK-free policy and request lifecycle. Construct and Tick on the application's main thread.</summary>
    public sealed class AdsService : IAdsService
    {
        private sealed class PendingShow
        {
            public AdRequest Request;
            public readonly TaskCompletionSource<AdResult> Completion = new TaskCompletionSource<AdResult>();
            public CancellationTokenRegistration Cancellation;
            public bool Rewarded, Cancelled, Started;
        }

        private readonly IAdsProvider provider;
        private readonly Dictionary<string, AdPlacementDefinition> placements = new Dictionary<string, AdPlacementDefinition>(StringComparer.Ordinal);
        private readonly Dictionary<string, double> nextAllowed = new Dictionary<string, double>(StringComparer.Ordinal);
        private readonly ConcurrentQueue<Action> callbacks = new ConcurrentQueue<Action>();
        private readonly Func<double> time;
        private readonly Action<Exception> reportException;
        private readonly int ownerThread = Thread.CurrentThread.ManagedThreadId;
        private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        private Task<bool> initialization;
        private TaskCompletionSource<bool> initializationCompletion;
        private PendingShow pending;
        private AdRequest? bannerRequest;
        private bool bannerRequested;

        public AdsStatus Status { get; private set; }
        public bool IsFullscreenBusy => pending != null;
        public bool ForcedAdsDisabled { get; private set; }
        public BannerInfo Banner { get; private set; }
        public event Action<AdsStatus> StatusChanged;
        public event Action<AdEvent> AdEvent;
        public event Action<BannerInfo> BannerChanged;

        public AdsService(IAdsProvider provider, IEnumerable<AdPlacementDefinition> placements, Func<double> time, Action<Exception> reportException = null)
        {
            this.provider = provider ?? throw new ArgumentNullException(nameof(provider));
            this.time = time ?? throw new ArgumentNullException(nameof(time));
            this.reportException = reportException;
            foreach (var placement in placements ?? throw new ArgumentNullException(nameof(placements)))
            {
                if (placement == null) throw new ArgumentException("Catalog contains a null placement.");
                if (this.placements.ContainsKey(placement.Id)) throw new ArgumentException("Duplicate placement: " + placement.Id);
                this.placements.Add(placement.Id, placement);
            }
            provider.AdEvent += OnProviderEvent;
            provider.BannerChanged += OnProviderBanner;
        }

        public Task<bool> InitializeAsync(CancellationToken cancellation = default)
        {
            if (Status == AdsStatus.Disposed) return Task.FromResult(false);
            if (Status == AdsStatus.Ready) return Task.FromResult(true);
            if (Status == AdsStatus.Initializing) return initialization;
            initializationCompletion = new TaskCompletionSource<bool>();
            initialization = initializationCompletion.Task;
            _ = InitializeProvider(cancellation, initializationCompletion);
            return initialization;
        }

        private async Task InitializeProvider(CancellationToken cancellation, TaskCompletionSource<bool> completion)
        {
            SetStatus(AdsStatus.Initializing);
            if (Status == AdsStatus.Disposed) { completion.TrySetResult(false); return; }
            using (var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellation, lifetime.Token))
            {
                try
                {
                    bool initialized = await provider.InitializeAsync(linked.Token);
                    if (Status == AdsStatus.Disposed) { completion.TrySetResult(false); return; }
                    if (linked.IsCancellationRequested) initialized = false;
                    SetStatus(initialized ? AdsStatus.Ready : AdsStatus.Failed);
                    if (initialized)
                        foreach (var placement in placements.Values)
                            if (placement.Preload && placement.Format != AdFormat.Banner) Load(placement.Id);
                    completion.TrySetResult(initialized && Status == AdsStatus.Ready);
                }
                catch (Exception exception)
                {
                    if (Status != AdsStatus.Disposed) SetStatus(AdsStatus.Failed);
                    if (!(exception is OperationCanceledException)) reportException?.Invoke(exception);
                    completion.TrySetResult(false);
                }
            }
        }

        private bool Allowed(AdPlacementDefinition placement) => Status == AdsStatus.Ready && placement.Enabled &&
            (!ForcedAdsDisabled || placement.Format == AdFormat.Rewarded);

        public AdState GetState(string placementId)
        {
            if (placementId == null || !placements.TryGetValue(placementId, out var placement) || !Allowed(placement)) return AdState.Unavailable;
            if (pending != null && pending.Request.Placement.Id == placementId) return AdState.Showing;
            try { return provider.GetState(placement); }
            catch (Exception exception) { reportException?.Invoke(exception); return AdState.Unavailable; }
        }

        public bool Load(string placementId)
        {
            if (placementId == null || !placements.TryGetValue(placementId, out var placement) || !Allowed(placement)) return false;
            var state = GetState(placementId);
            if (state == AdState.Loading || state == AdState.Ready || state == AdState.Showing) return true;
            try { provider.Load(placement); return true; }
            catch (Exception exception) { reportException?.Invoke(exception); return false; }
        }

        public Task<AdResult> ShowAsync(string placementId, CancellationToken cancellation = default)
        {
            var requestId = Guid.NewGuid();
            if (Status == AdsStatus.Disposed) return Result(requestId, placementId, AdOutcome.Disposed);
            if (cancellation.IsCancellationRequested) return Result(requestId, placementId, AdOutcome.Cancelled);
            if (placementId == null || !placements.TryGetValue(placementId, out var placement) || placement.Format == AdFormat.Banner || !placement.Enabled ||
                (ForcedAdsDisabled && placement.Format != AdFormat.Rewarded)) return Result(requestId, placementId, AdOutcome.Disabled);
            if (IsFullscreenBusy) return Result(requestId, placementId, AdOutcome.Busy);
            if (nextAllowed.TryGetValue(placementId, out var next) && time() < next) return Result(requestId, placementId, AdOutcome.Disabled);
            if (GetState(placementId) != AdState.Ready) return Result(requestId, placementId, AdOutcome.NotReady);

            var show = new PendingShow { Request = new AdRequest(requestId, placement) };
            pending = show;
            show.Cancellation = cancellation.Register(() => Dispatch(() => Cancel(show)));
            if (show.Cancelled) { Complete(show, AdOutcome.Cancelled); return show.Completion.Task; }
            try { provider.Show(show.Request); }
            catch (Exception exception) { Complete(show, AdOutcome.Failed, exception.Message); }
            return show.Completion.Task;
        }

        private static Task<AdResult> Result(Guid requestId, string placement, AdOutcome outcome) =>
            Task.FromResult(new AdResult(requestId, placement, outcome));

        private void Cancel(PendingShow show)
        {
            if (pending != show) return;
            show.Cancelled = true;
            show.Completion.TrySetResult(new AdResult(show.Request.Id, show.Request.Placement.Id, AdOutcome.Cancelled));
            // A caller cancelling cannot dismiss a native ad. Keep the fullscreen lock until the provider closes it.
        }

        public bool ShowBanner(string placementId)
        {
            if (placementId == null || !placements.TryGetValue(placementId, out var placement) || placement.Format != AdFormat.Banner || !Allowed(placement)) return false;
            if (bannerRequested && bannerRequest?.Placement.Id == placementId) return true;
            HideBanner();
            bannerRequested = true;
            bannerRequest = new AdRequest(Guid.NewGuid(), placement);
            try { provider.ShowBanner(bannerRequest.Value); return true; }
            catch (Exception exception) { HideBanner(true); reportException?.Invoke(exception); return false; }
        }

        public void HideBanner(bool destroy = false)
        {
            bool wasRequested = bannerRequested;
            bannerRequested = false;
            bannerRequest = null;
            SetBanner(BannerInfo.Hidden);
            if (Status == AdsStatus.Disposed || !wasRequested && !destroy) return;
            try { provider.HideBanner(destroy); }
            catch (Exception exception) { reportException?.Invoke(exception); }
        }

        public void SetForcedAdsDisabled(bool disabled)
        {
            if (Status == AdsStatus.Disposed || ForcedAdsDisabled == disabled) return;
            ForcedAdsDisabled = disabled;
            if (disabled) HideBanner(true);
        }

        private void OnProviderBanner(BannerInfo banner) => Dispatch(() =>
        {
            if (Status != AdsStatus.Disposed && bannerRequested && !ForcedAdsDisabled) SetBanner(banner);
        });

        private void OnProviderEvent(AdEvent value) => Dispatch(() =>
        {
            if (Status == AdsStatus.Disposed || value.PlacementId == null || !placements.ContainsKey(value.PlacementId)) return;
            if (value.Kind == AdEventKind.Loaded || value.Kind == AdEventKind.LoadFailed) { Notify(AdEvent, value); return; }
            if (value.Format == AdFormat.Banner)
            {
                if (bannerRequest?.Id != value.RequestId) return;
                if (value.Kind == AdEventKind.Closed || value.Kind == AdEventKind.ShowFailed)
                { bannerRequested = false; bannerRequest = null; SetBanner(BannerInfo.Hidden); }
                Notify(AdEvent, value);
                return;
            }
            var show = pending;
            if (show == null || show.Request.Id != value.RequestId || show.Request.Placement.Id != value.PlacementId || show.Request.Placement.Format != value.Format) return;
            if (value.Kind == AdEventKind.RewardGranted)
            {
                if (show.Rewarded || show.Cancelled || value.Format != AdFormat.Rewarded) return;
                show.Rewarded = true;
            }
            if (value.Kind == AdEventKind.Started) show.Started = true;
            Notify(AdEvent, value);
            if (value.Kind == AdEventKind.Closed)
                Complete(show, show.Rewarded ? AdOutcome.RewardGranted : value.Format == AdFormat.Rewarded ? AdOutcome.ClosedWithoutReward : show.Started ? AdOutcome.Completed : AdOutcome.Failed);
            else if (value.Kind == AdEventKind.ShowFailed)
                Complete(show, show.Rewarded ? AdOutcome.RewardGranted : AdOutcome.Failed, value.Error);
        });

        private void Complete(PendingShow show, AdOutcome outcome, string error = null)
        {
            if (pending != show) return;
            pending = null;
            show.Cancellation.Dispose();
            if (show.Started) nextAllowed[show.Request.Placement.Id] = time() + show.Request.Placement.CooldownSeconds;
            show.Completion.TrySetResult(new AdResult(show.Request.Id, show.Request.Placement.Id, show.Cancelled ? AdOutcome.Cancelled : outcome, error));
        }

        private void Dispatch(Action callback)
        {
            if (Thread.CurrentThread.ManagedThreadId == ownerThread) callback();
            else callbacks.Enqueue(callback);
        }

        public void Tick()
        {
            if (Status == AdsStatus.Disposed) return;
            while (callbacks.TryDequeue(out var callback)) callback();
            if (Status == AdsStatus.Ready)
                try { provider.Tick(); } catch (Exception exception) { reportException?.Invoke(exception); }
        }

        private void SetBanner(BannerInfo banner)
        { if (Banner.Equals(banner)) return; Banner = banner; Notify(BannerChanged, banner); }

        private void SetStatus(AdsStatus status)
        { Status = status; Notify(StatusChanged, status); }

        private void Notify<T>(Action<T> handlers, T value)
        {
            if (handlers == null) return;
            foreach (Action<T> handler in handlers.GetInvocationList())
                try { handler(value); } catch (Exception exception) { reportException?.Invoke(exception); }
        }

        public void Dispose()
        {
            if (Status == AdsStatus.Disposed) return;
            HideBanner(true);
            if (pending != null) Complete(pending, AdOutcome.Disposed);
            SetStatus(AdsStatus.Disposed);
            initializationCompletion?.TrySetResult(false);
            lifetime.Cancel();
            provider.AdEvent -= OnProviderEvent;
            provider.BannerChanged -= OnProviderBanner;
            try { provider.Dispose(); } catch (Exception exception) { reportException?.Invoke(exception); }
            lifetime.Dispose();
            while (callbacks.TryDequeue(out _)) { }
            AdEvent = null; BannerChanged = null; StatusChanged = null;
        }
    }
}
