using System;

namespace NebulaSoft.Ads
{
    public enum AdFormat { Banner, Interstitial, Rewarded }
    public enum AdState { Unavailable, Loading, Ready, Showing }
    public enum AdsStatus { NotInitialized, Initializing, Ready, Failed, Disposed }
    public enum AdOutcome { Completed, RewardGranted, ClosedWithoutReward, NotReady, Disabled, Busy, Failed, Cancelled, Disposed }
    public enum AdEventKind { Loaded, LoadFailed, Started, Clicked, RewardGranted, Closed, ShowFailed, Impression }

    public sealed class AdPlacementDefinition
    {
        public string Id { get; }
        public AdFormat Format { get; }
        public bool Enabled { get; }
        public bool Preload { get; }
        public double CooldownSeconds { get; }

        public AdPlacementDefinition(string id, AdFormat format, bool enabled = true, bool preload = true, double cooldownSeconds = 0)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Placement ID is required.", nameof(id));
            if (!Enum.IsDefined(typeof(AdFormat), format)) throw new ArgumentOutOfRangeException(nameof(format));
            Id = id;
            Format = format;
            Enabled = enabled;
            Preload = preload;
            CooldownSeconds = Math.Max(0, cooldownSeconds);
        }
    }

    public readonly struct AdRequest
    {
        public Guid Id { get; }
        public AdPlacementDefinition Placement { get; }
        public AdRequest(Guid id, AdPlacementDefinition placement) { Id = id; Placement = placement; }
    }

    public readonly struct AdResult
    {
        public Guid RequestId { get; }
        public string PlacementId { get; }
        public AdOutcome Outcome { get; }
        public string Error { get; }
        public bool RewardGranted => Outcome == AdOutcome.RewardGranted;
        public bool Succeeded => Outcome == AdOutcome.Completed || RewardGranted;

        public AdResult(Guid requestId, string placementId, AdOutcome outcome, string error = null)
        { RequestId = requestId; PlacementId = placementId; Outcome = outcome; Error = error; }
    }

    public readonly struct AdEvent
    {
        public Guid RequestId { get; }
        public string PlacementId { get; }
        public AdFormat Format { get; }
        public AdEventKind Kind { get; }
        public string ProviderId { get; }
        public string Error { get; }

        public AdEvent(Guid requestId, string placementId, AdFormat format, AdEventKind kind, string providerId, string error = null)
        { RequestId = requestId; PlacementId = placementId; Format = format; Kind = kind; ProviderId = providerId; Error = error; }
    }

    /// <summary>Display pixels. BottomInset is measured from the display bottom; unknown geometry stays hidden.</summary>
    public readonly struct BannerInfo : IEquatable<BannerInfo>
    {
        public static readonly BannerInfo Hidden = default;
        public string ProviderId { get; }
        public bool IsVisible { get; }
        public int Width { get; }
        public int Height { get; }
        public float BottomInset { get; }
        public float TopEdge => IsVisible ? BottomInset + Height : 0;

        public BannerInfo(string providerId, int width, int height, float bottomInset = 0)
        {
            IsVisible = width > 0 && height > 0;
            ProviderId = IsVisible ? providerId : null;
            Width = IsVisible ? width : 0;
            Height = IsVisible ? height : 0;
            BottomInset = IsVisible ? Math.Max(0, bottomInset) : 0;
        }

        public bool Equals(BannerInfo other) => ProviderId == other.ProviderId && IsVisible == other.IsVisible &&
            Width == other.Width && Height == other.Height && BottomInset.Equals(other.BottomInset);
        public override bool Equals(object obj) => obj is BannerInfo other && Equals(other);
        public override int GetHashCode() => Width ^ Height ^ BottomInset.GetHashCode();
    }
}
