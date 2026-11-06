#if MODULE_ADSKIT
using UnityEngine;
using VoxelBusters.AdsKit;

namespace NebulaSoft
{
    /// <summary>SDK-specific consent factory. Add a different implementation when using another consent system.</summary>
    public abstract class AdsKitConsentProviderConfig : ScriptableObject
    {
        public abstract IConsentFormProvider CreateProvider();
    }
}
#endif
