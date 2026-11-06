using System.Threading;
using System.Threading.Tasks;

namespace NebulaSoft.Ads
{
    public static class AdsPlacementExtensions
    {
        public static Task<AdResult> ShowAsync(this IAdsService service, AdPlacement placement, CancellationToken cancellation = default) =>
            service.ShowAsync(placement != null ? placement.Id : null, cancellation);
        public static bool ShowBanner(this IAdsService service, AdPlacement placement) => service.ShowBanner(placement != null ? placement.Id : null);
        public static bool Load(this IAdsService service, AdPlacement placement) => service.Load(placement != null ? placement.Id : null);
        public static AdState GetState(this IAdsService service, AdPlacement placement) => service.GetState(placement != null ? placement.Id : null);
    }
}
