using System;
using System.Threading.Tasks;
using UnityEngine;

namespace NebulaSoft
{
    public class DummyIAPWrapper : IAPWrapper
    {
        private TweenCase pendingPurchase;
        private bool disposed;
        public override async Task Init(IAPSettings settings)
        {
            await Task.Yield();
            if (!disposed) IAPManager.OnModuleInitialized();
        }

        public override void BuyProduct(string key)
        {
            var item = IAPManager.GetIAPItemByKey(key);
            SystemMessage.ShowLoadingPanel();
            SystemMessage.ChangeLoadingMessage("Payment in progress..");
            pendingPurchase = Tween.DelayedCall(1.0f, () =>
            {
                try
                {
                    if (!IAPManager.TryFulfillPurchase(item, "dummy_" + Guid.NewGuid().ToString("N")))
                        IAPManager.OnPurchaseFailed(key, PurchaseFailureReason.Unknown);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                    IAPManager.OnPurchaseFailed(key, PurchaseFailureReason.Unknown);
                }
                finally
                {
                    IAPManager.FinishPurchase();
                    SystemMessage.HideLoadingPanel();
                }
            }, unscaledTime: true);
        }

        public override ProductData GetProductData(string key)
        {
            var item = IAPManager.GetIAPItemByKey(key);
            return item != null ? new ProductData(item.ProductType) : null;
        }
        public override bool IsSubscribed(string key) => false;
        public override bool IsPurchased(string id)
        {
            var item = IAPManager.GetIAPItem(id);
            return item != null && item.ProductType == ProductType.NonConsumable && item.TimesPurchased > 0;
        }
        public override void RestorePurchases()
        {
            IAPManager.FinishRestorePurchases();
            IAPManager.OnPurchasesRestored();
        }
        public override void Dispose()
        {
            disposed = true;
            pendingPurchase.KillActive();
        }
    }
}
