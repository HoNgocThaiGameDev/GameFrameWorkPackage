#if MODULE_IAP
using UnityEngine.Purchasing;
#endif

namespace NebulaSoft
{
    public class ProductData
    {
        public ProductType ProductType { get; }

        public decimal Price { get; }
        public string ISOCurrencyCode { get; } 
        public string LocalizedPriceString { get; }

        public bool IsSubscribed { get; }

#if MODULE_IAP
        public Product Product { get; }
#endif

        public ProductData()
        {
            Price = 0.00m;
            ISOCurrencyCode = "USD";
            LocalizedPriceString = string.Empty;

            IsSubscribed = false;
        }

        public ProductData(ProductType productType)
        {
            ProductType = productType;

            Price = 0.00m;
            ISOCurrencyCode = "USD";
            LocalizedPriceString = string.Empty;

            IsSubscribed = false;
        }

        public string GetLocalPrice()
        {
            if (!string.IsNullOrWhiteSpace(LocalizedPriceString))
                return LocalizedPriceString;

            return string.Format("{0} {1}", ISOCurrencyCode, Price);
        }

#if MODULE_IAP
        public ProductData(Product product)
        {
            Product = product;

            ProductType = (ProductType)product.definition.type;

            Price = product.metadata.localizedPrice;
            ISOCurrencyCode = product.metadata.isoCurrencyCode;
            LocalizedPriceString = product.metadata.localizedPriceString;
        }
#endif
    }
}

// -----------------
// IAP Manager v 1.2.2
// -----------------

// Changelog
// v 1.2.2
// • Fixed serialization bug
// v 1.2.1
// • Added test mode
// v 1.2
// • Support of IAP version 4.11.0
// • Added Editor purchase wrapper
// v 1.1
// • Support of IAP version 4.9.3
// v 1.0.3
// • Support of IAP version 4.7.0
// v 1.0.2
// • Added quick access to the local price of IAP via GetProductLocalPriceString method
// v 1.0.1
// • Added restoration status messages
// v 1.0.0
// • Documentation added
// v 0.4
// • IAPStoreListener inheriting from MonoBehaviour
// v 0.3
// • Editor style update
// v 0.2
// • IAPManager structure changed
// • Enums from UnityEditor.Purchasing has duplicated to prevent serialization problems
// v 0.1
// • Added basic version
