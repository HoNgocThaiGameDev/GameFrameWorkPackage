using System;
using System.Threading;
using System.Threading.Tasks;

namespace NebulaSoft.Ads
{
    public interface IAdsService : IDisposable
    {
        AdsStatus Status { get; }
        bool IsFullscreenBusy { get; }
        bool ForcedAdsDisabled { get; }
        BannerInfo Banner { get; }
        event Action<AdsStatus> StatusChanged;
        event Action<AdEvent> AdEvent;
        event Action<BannerInfo> BannerChanged;
        Task<bool> InitializeAsync(CancellationToken cancellation = default);
        AdState GetState(string placementId);
        bool Load(string placementId);
        Task<AdResult> ShowAsync(string placementId, CancellationToken cancellation = default);
        bool ShowBanner(string placementId);
        void HideBanner(bool destroy = false);
        void SetForcedAdsDisabled(bool disabled);
    }
}
