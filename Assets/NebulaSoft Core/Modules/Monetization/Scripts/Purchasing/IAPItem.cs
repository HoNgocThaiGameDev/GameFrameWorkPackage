using UnityEngine;

namespace NebulaSoft
{
    [System.Serializable]
    public class IAPItem
    {
        [SerializeField] string key;
        [SerializeField] string displayName;
        [SerializeField] bool useLegacyKey = true;
        [SerializeField] string androidID;
        [SerializeField] string iOSID;

        [SerializeField] ProductKeyType productKeyType;
        [SerializeField] ProductType productType;

        [SerializeField] float defaultUSDPrice = 0.99f;

        [CreateScriptableObject]
        [SerializeField] RewardsSet rewardsSet;

        public string Key => string.IsNullOrWhiteSpace(key) && useLegacyKey ? GetLegacyKey(productKeyType) : key;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? Key : displayName;
        public bool HasLegacyKey => useLegacyKey;
        public string AndroidID => androidID;
        public string IOSID => iOSID;
        public string SaveKey => useLegacyKey ? $"iap_{productKeyType}" : $"iap_{Key}";

        public static string GetLegacyKey(ProductKeyType product)
        {
            switch (product)
            {
                case ProductKeyType.NoAds: return "no_ads";
                case ProductKeyType.StarterPack: return "starter_pack";
                case ProductKeyType.GoldSmall: return "coins_small";
                case ProductKeyType.GoldMedium: return "coins_medium";
                case ProductKeyType.GoldBig: return "coins_big";
                case ProductKeyType.PUPack: return "powerup_pack";
                default: return product.ToString();
            }
        }

        public string ID
        {
            get
            {
#if UNITY_ANDROID
                return androidID;
#elif UNITY_IOS
                return iOSID;
#else
                return !string.IsNullOrWhiteSpace(androidID) ? androidID : iOSID;
#endif
            }
        }

        public ProductType ProductType { get => productType; set => productType = value; }
        public ProductKeyType ProductKeyType { get => productKeyType; set => productKeyType = value; }
        public float DefaultUSDPrice { get => defaultUSDPrice; }

        public int TimesPurchased => save == null ? 0 : save.TimesPurchased;
        public RewardsSet RewardsSet => rewardsSet;

        private Save save;

        public void Init()
        {
            save = SaveController.GetSaveObject<Save>(SaveKey);
        }

        public void OnProductPurchased()
        {
            rewardsSet?.ApplyReward();
            save.TimesPurchased++;
            SaveController.MarkAsSaveIsRequired();
        }

        public void OnProductRestored()
        {
            rewardsSet?.RestoreReward();
        }

        internal void ApplyPurchaseRewards(int startIndex, System.Action<int> stepCompleted)
        {
            rewardsSet?.ApplyRewardSteps(startIndex, stepCompleted);
        }

        internal void RecordPurchase()
        {
            save.TimesPurchased++;
            SaveController.MarkAsSaveIsRequired();
        }

        public void OverrideDefaultPrice(IAPRemoteConfigData.IAP remoteConfigItem)
        {
            if (remoteConfigItem != null)
                defaultUSDPrice = remoteConfigItem.price;
        }

        public class Save : ISaveObject
        {
            public int TimesPurchased;

            public bool IsPurchased => TimesPurchased > 0;

            public void Flush()
            {

            }
        }
    }
}
