using System;
using System.Threading.Tasks;

namespace NebulaSoft
{
    public abstract class IAPWrapper : IDisposable
    {
        public abstract Task Init(IAPSettings settings);
        public abstract void RestorePurchases();
        public abstract void BuyProduct(string key);
        public abstract ProductData GetProductData(string key);
        public abstract bool IsSubscribed(string key);
        public abstract bool IsPurchased(string id);
        public virtual void Dispose() { }
        public void BuyProduct(ProductKeyType key) => BuyProduct(IAPManager.GetIAPItem(key)?.Key);
        public ProductData GetProductData(ProductKeyType key) => GetProductData(IAPManager.GetIAPItem(key)?.Key);
        public bool IsSubscribed(ProductKeyType key) => IsSubscribed(IAPManager.GetIAPItem(key)?.Key);
    }
}
