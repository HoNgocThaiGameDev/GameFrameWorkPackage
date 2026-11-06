using NebulaSoft.Ads;
using UnityEngine;

namespace NebulaSoft
{
    [CreateAssetMenu(menuName = "NebulaSoft/Ads/Providers/Dummy")]
    public sealed class DummyAdsProviderConfig : AdsProviderConfig
    {
        [SerializeField] BannerPosition bannerPosition;
        public override IAdsProvider CreateProvider() => new DummyAdsProvider(bannerPosition);
    }
}
