using System.Collections.Generic;
using UnityEngine;

namespace NebulaSoft.Ads
{
    /// <summary>The composition root calls this factory. Runtime only receives IAdsProvider.</summary>
    public abstract class AdsProviderConfig : ScriptableObject
    {
        public abstract IAdsProvider CreateProvider();
        public virtual IEnumerable<string> ValidateProvider(AdsCatalog catalog) { yield break; }
    }
}
