using UnityEngine;

namespace NebulaSoft
{
    [System.Serializable]
    public class SkinsHandler
    {
        [SerializeField] AbstractSkinDatabase[] skinProviders;
        public AbstractSkinDatabase[] SkinsProviders => skinProviders;

        public int ProvidersCount => skinProviders.Length;

        public AbstractSkinDatabase GetSkinsProvider(int index)
        {
            return skinProviders[index];
        }

        public AbstractSkinDatabase GetSkinsProvider(System.Type providerType)
        {
            if (!skinProviders.IsNullOrEmpty())
            {
                foreach (AbstractSkinDatabase skinProvider in skinProviders)
                {
                    if (skinProvider.GetType() == providerType)
                        return skinProvider;
                }
            }

            return null;
        }

        public bool HasSkinsProvider(System.Type providerType)
        {
            if (!skinProviders.IsNullOrEmpty())
            {
                foreach (AbstractSkinDatabase skinProvider in skinProviders)
                {
                    if (skinProvider.GetType() == providerType)
                        return true;
                }
            }

            return false;
        }

        public bool HasSkinsProvider(AbstractSkinDatabase provider)
        {
            if(!skinProviders.IsNullOrEmpty())
            {
                foreach (AbstractSkinDatabase skinProvider in skinProviders)
                {
                    if (skinProvider == provider)
                        return true;
                }
            }

            return false;
        }
    }
}
