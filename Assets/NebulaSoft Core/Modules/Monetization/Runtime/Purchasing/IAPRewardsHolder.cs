using UnityEngine;

namespace NebulaSoft
{
    public sealed class IAPRewardsHolder : RewardsHolder
    {
        // Retained for existing serialized prefabs and public API callers.
        [SerializeField, HideInInspector, Hide] ProductKeyType productKey;
        [SerializeField, HideInInspector, Hide] string productKeyId;
        [SerializeField, HideInInspector, Hide] bool useLegacyProductKey = true;
        public ProductKeyType ProductKey => productKey;
        public string ProductKeyId => string.IsNullOrWhiteSpace(productKeyId) && useLegacyProductKey ? IAPItem.GetLegacyKey(productKey) : productKeyId;
        [Group("Settings")]
        [SerializeField] IAPButton purchaseButton;
        protected override bool ShouldPresentReward => isActiveAndEnabled;

        private void OnEnable()
        {
            IAPManager.StateChanged += Refresh;
            Refresh();
        }
        private void Start() => Refresh();
        private void OnDisable() => IAPManager.StateChanged -= Refresh;

        public void Refresh()
        {
            var item = IAPManager.GetIAPItemByKey(ProductKeyId);
            purchaseButton?.Init(ProductKeyId);
            if (item == null) return;
            if (!isInitialized && item.RewardsSet != null)
            {
                rewardSet = item.RewardsSet;
                InitializeComponents();
            }
            if (item.ProductType == ProductType.NonConsumable && IAPManager.IsPurchased(item.Key) || CheckDisableState())
                gameObject.SetActive(false);
        }
    }
}
