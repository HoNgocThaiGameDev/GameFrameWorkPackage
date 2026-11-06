using System;
using System.Threading;
using System.Threading.Tasks;

namespace NebulaSoft.Ads
{
    /// <summary>Only adapters implement this port. Events must carry the request ID supplied to Show.</summary>
    public interface IAdsProvider : IDisposable
    {
        string Id { get; }
        event Action<AdEvent> AdEvent;
        event Action<BannerInfo> BannerChanged;
        Task<bool> InitializeAsync(CancellationToken cancellation);
        AdState GetState(AdPlacementDefinition placement);
        void Load(AdPlacementDefinition placement);
        void Show(AdRequest request);
        void ShowBanner(AdRequest request);
        void HideBanner(bool destroy);
        void Tick();
    }
}
