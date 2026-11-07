using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using System;
using System.Reflection;
using System.Collections.ObjectModel;
using System.Linq;

#if MODULE_IAP && UNITY_IAP_NEW
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Security;
#endif

namespace NebulaSoft
{
    /// <summary>
    /// Wrapper class for Unity IAP functionality.
    /// </summary>
    public class UnityIAP5Wrapper : IAPWrapper
    {
        private readonly StoreType CURRENT_STORE = GetCurrentStore();

#if MODULE_IAP && UNITY_IAP_NEW
        private StoreController m_StoreController;
        
        private List<PurchaseCallback> purchaseCallbacks = new List<PurchaseCallback>();

        private CrossPlatformValidator m_Validator = null;

        private EntitlementsService entitlementsService;
        private IAPItem activePurchaseItem;
        private bool isPurchaseInProgress;
        private int restoredEntitlementCount;
#endif

        private FraudDetectionData fraudDetectionData;

        /// <summary>
        /// Initializes the IAP system with the provided settings.
        /// </summary>
        /// <param name="settings">The IAP settings to use for initialization.</param>
        public override async Task Init(IAPSettings settings)
        {
#if MODULE_IAP && UNITY_IAP_NEW
            try
            {
                m_StoreController = UnityIAPServices.StoreController();

                m_StoreController.OnPurchasePending += OnPurchasePending;
                m_StoreController.OnPurchaseConfirmed += OnPurchaseConfirmed;
                m_StoreController.OnPurchaseFailed += OnPurchaseFailed;

                m_StoreController.OnStoreDisconnected += OnStoreDisconnected;

                Debug.Log("[IAP Manager]: Connecting to store.");

                await m_StoreController.Connect();

                fraudDetectionData = new FraudDetectionData();
                entitlementsService = new EntitlementsService(m_StoreController);

#if UNITY_ANDROID
                ConfigureGoogleFraudDetection(m_StoreController.GooglePlayStoreExtendedService);
#elif UNITY_IOS
                ConfigureAppleFraudDetection(m_StoreController.AppleStoreExtendedService);
#endif

                ConfigureValidator();

                m_StoreController.OnProductsFetchFailed += OnProductsFetchedFailed;
                m_StoreController.OnProductsFetched += OnProductsFetched;
                m_StoreController.OnPurchasesFetched += OnPurchasesFetched;
                m_StoreController.OnPurchasesFetchFailed += OnPurchasesFetchFailed;
                m_StoreController.OnPurchaseDeferred += OnPurchaseDeferred;

                List<ProductDefinition> initialProductsToFetch = new List<ProductDefinition>();

                IAPItem[] items = settings.StoreItems;
                foreach (var item in items)
                {
                    if (!string.IsNullOrEmpty(item.ID))
                    {
                        initialProductsToFetch.Add(new ProductDefinition(item.ID, (UnityEngine.Purchasing.ProductType)item.ProductType));
                    }
                    else
                    {
                        Debug.LogWarning($"[IAP Manager]: Product {item.ProductType} does not have configured IDs.");
                    }
                }

                m_StoreController.FetchProducts(initialProductsToFetch);
            }
            catch (System.Exception exception)
            {
                IAPManager.OnInitializationFailed(exception.Message);
            }
#else
            await Task.Run(() => Debug.Log("[IAP Manager]: Define MODULE_IAP is disabled!"));
#endif
        }

#if MODULE_IAP && UNITY_IAP_NEW
        private void OnPurchaseDeferred(DeferredOrder order)
        {
            // Approval can arrive in a later session; the store retains the order.
            if (!OwnsActivePurchase(order)) return;
            ClearPurchaseInProgress();
            SystemMessage.ChangeLoadingMessage("Waiting for payment approval..");
            SystemMessage.HideLoadingPanel();
            foreach (CartItem cartItem in order.CartOrdered.Items())
            {
                Product product = cartItem.Product;

                if (product is null)
                {
                    Debug.Log("[IAPManager]: Could not find product in order.");

                    continue;
                }

                string id = product.definition.id;

                Debug.Log($"OnPurchaseDeferred - Product: {product?.definition.id}");
            }
        }
#endif

        private void ConfigureValidator()
        {
            if (CURRENT_STORE != StoreType.GooglePlay) return;

#if MODULE_IAP && UNITY_IAP_NEW
#if !UNITY_EDITOR
            Type googlePlayTangleType = Type.GetType("UnityEngine.Purchasing.Security.GooglePlayTangle")
                ?? typeof(UnityIAP5Wrapper).Assembly.GetType("UnityEngine.Purchasing.Security.GooglePlayTangle");
            if (googlePlayTangleType != null)
            {
                MethodInfo dataMethod = googlePlayTangleType.GetMethod("Data", BindingFlags.Static | BindingFlags.Public);
                if (dataMethod != null)
                {
                    byte[] googleData = (byte[])dataMethod.Invoke(null, null);
                    m_Validator = new CrossPlatformValidator(googleData, Application.identifier);
                }
            }
#endif
#endif
        }

#if MODULE_IAP && UNITY_IAP_NEW
        private void OnPurchaseFailed(FailedOrder order)
        {
            bool ownsActivePurchase = OwnsActivePurchase(order);
            foreach (CartItem cartItem in order.CartOrdered.Items())
            {
                Product product = cartItem.Product;

                if (product is null)
                {
                    Debug.Log("[IAPManager]: Could not find product in order.");

                    continue;
                }

                string id = product.definition.id;

                Debug.Log($"Confirmation failed - Product: '{product?.definition.id}'," +
                          $"PurchaseFailureReason: {order.FailureReason.ToString()},"
                          + $"Confirmation Failure Details: {order.Details}");

                IAPItem item = IAPManager.GetIAPItem(product.definition.id);
                if (item != null)
                {
                    QueuePurchaseAttempt(item, GetAttemptStatus(order.FailureReason.ToString()), order.FailureReason.ToString());

                    int callbackIndex = purchaseCallbacks.FindIndex(x => x.ProductKeyType == item.Key);
                    if (callbackIndex != -1)
                        purchaseCallbacks.RemoveAt(callbackIndex);

                    NebulaSoft.PurchaseFailureReason purchaseFailureReason = (NebulaSoft.PurchaseFailureReason)order.FailureReason;

                    IAPManager.Safely(() => AnalyticsController.OnIAPFailed(item, purchaseFailureReason));

                    IAPManager.OnPurchaseFailed(item.Key, purchaseFailureReason);
                }
                else
                {
                    if (Monetization.VerboseLogging)
                        Debug.Log($"[IAPManager]: Product - {product.definition.id} can't be found!");
                }

                Debug.LogWarning($"Purchase failed - Product: '{product?.definition.id}'," +
                          $"PurchaseFailureReason: {order.FailureReason.ToString()},"
                          + $"Purchase Failure Details: {order.Details}");
            }

            if (ownsActivePurchase)
            {
                ClearPurchaseInProgress();
                SystemMessage.ChangeLoadingMessage("Payment failed!");
                SystemMessage.HideLoadingPanel();
            }
        }

        private bool IsPurchaseValid(Order order)
        {
            //If the validator doesn't support the current store, we assume the purchase is valid
            if (m_Validator != null)
            {
                try
                {
                    IPurchaseReceipt[] result = m_Validator.Validate(order.Info.Receipt);

#if DEBUG_LOGS
                    Debug.Log("Receipt is valid. Contents:");
                    foreach (var receipt in result)
                    {
                        Debug.Log($"Product ID: {receipt.productID}\n" +
                            $"Purchase Date: {receipt.purchaseDate}\n" +
                            $"Transaction ID: {receipt.transactionID}");

                        if (receipt is GooglePlayReceipt googleReceipt)
                        {
                            Debug.Log($"Purchase State: {googleReceipt.purchaseState}\n" +
                                $"Purchase Token: {googleReceipt.purchaseToken}");
                        }
                    }
#endif
                }
                //If the purchase is deemed invalid, the validator throws an IAPSecurityException.
            catch (IAPSecurityException reason)
            {
                Debug.Log($"Invalid receipt: {reason}");

                return false;
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[IAP Manager]: Local receipt validation could not complete: " + exception.Message);

                return false;
            }
            }

            return true;
        }

        private void OnPurchasePending(PendingOrder order)
        {
            bool ownsActivePurchase = OwnsActivePurchase(order);
            if (!IsPurchaseValid(order))
            {
                if (ownsActivePurchase)
                {
                    IAPManager.OnPurchaseFailed(activePurchaseItem.Key, NebulaSoft.PurchaseFailureReason.SignatureInvalid);
                    QueueActivePurchaseAttempt("failed", "local_validation_failed");
                    ClearPurchaseInProgress();
                    SystemMessage.ChangeLoadingMessage("Purchase validation failed. Please reconnect and try again.");
                    SystemMessage.HideLoadingPanel();
                }
                Debug.LogWarning("[IAP Manager]: Local receipt validation failed. The order remains pending for a later retry.");
                return;
            }

            // Reward locally before acknowledgement. A persisted transaction hash
            // prevents confirmation retries from granting the same order again.
            try
            {
                string hash = IAPIntegration.GetTransactionIdHash(order.Info.TransactionID);
                if (IAPManager.IsIAPTransactionFulfilled(hash)) SaveController.SaveChecked();
                else if (!FulfillLocalPurchase(order))
                {
                    throw new InvalidOperationException("Product or transaction is missing. Order remains pending.");
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[IAP]: Fulfillment failed; order remains pending: " + exception.Message);
                if (ownsActivePurchase)
                {
                    IAPManager.OnPurchaseFailed(activePurchaseItem.Key, NebulaSoft.PurchaseFailureReason.Unknown);
                    ClearPurchaseInProgress();
                    SystemMessage.HideLoadingPanel();
                }
                return;
            }

            try
            {
                QueueGooglePlayTransactionLogs(order);
            }
            catch (Exception exception)
            {
                // A client-reported Firestore log is best-effort only. It must
                // never prevent Unity IAP from acknowledging a valid purchase.
                Debug.LogWarning("[Firebase IAP] Transaction logging was deferred: " + exception.Message);
            }

            try { m_StoreController.ConfirmPurchase(order); }
            catch (Exception exception)
            {
                // Rewards are already durable; redelivery only retries acknowledgement.
                Debug.LogWarning("[IAP]: Store confirmation remains pending: " + exception.Message);
                if (OwnsActivePurchase(order))
                {
                    ClearPurchaseInProgress();
                    SystemMessage.HideLoadingPanel();
                }
            }
        }

        private bool FulfillLocalPurchase(Order order)
        {
            string hash = IAPIntegration.GetTransactionIdHash(order.Info.TransactionID);
            if (string.IsNullOrWhiteSpace(hash)) return false;
            // Validate the whole cart before applying any reward.
            var entries = order.CartOrdered.Items().ToList();
            if (entries.Count == 0 || entries.Any(entry => entry.Product == null || IAPManager.GetIAPItem(entry.Product.definition.id) == null))
                return false;
            foreach (var entry in entries)
            {
                var product = entry.Product;
                var item = IAPManager.GetIAPItem(product.definition.id);
                if (!IAPManager.TryFulfillPurchase(item, hash + ":" + product.definition.id)) return false;
                IAPManager.Safely(() => InvokePurchaseAnalyticsCallback(item, product, order.Info.Receipt));
            }
            // Keep recognizing transaction hashes written by earlier framework versions.
            IAPManager.MarkIAPTransactionFulfilled(hash);
            SaveController.SaveChecked();
            return true;
        }

        private void QueueGooglePlayTransactionLogs(PendingOrder order)
        {
            if (CURRENT_STORE != StoreType.GooglePlay)
                return;

            foreach (CartItem cartItem in order.CartOrdered.Items())
            {
                Product product = cartItem.Product;
                if (product == null)
                    continue;

                IAPItem item = IAPManager.GetIAPItem(product.definition.id);
                IAPIntegration.QueueGooglePlayPurchase(
                    product.definition.id,
                    item != null ? item.ProductType.ToString() : "Unknown",
                    order.Info.TransactionID,
                    "purchase_callback");
            }
        }

        private void QueueGooglePlayTransactionLogs(ConfirmedOrder order)
        {
            if (CURRENT_STORE != StoreType.GooglePlay)
                return;

            foreach (CartItem cartItem in order.CartOrdered.Items())
            {
                Product product = cartItem.Product;
                if (product == null)
                    continue;

                IAPItem item = IAPManager.GetIAPItem(product.definition.id);
                IAPIntegration.QueueGooglePlayPurchase(
                    product.definition.id,
                    item != null ? item.ProductType.ToString() : "Unknown",
                    order.Info.TransactionID,
                    "restore_callback");
            }
        }

        private void OnPurchasesFetchFailed(PurchasesFetchFailureDescription description)
        {
            if (!IAPManager.IsRestoreInProgress)
                return;

            CompleteRestore(false, restoredEntitlementCount, description.Message);
        }

        private void OnPurchasesFetched(Orders orders)
        {
            int restoredCount = 0;
            try
            {
                var ownedProductIds = new HashSet<string>();
                if (orders != null)
                    foreach (ConfirmedOrder order in orders.ConfirmedOrders)
                    {
                        IAPManager.Safely(() => QueueGooglePlayTransactionLogs(order));
                        foreach (CartItem entry in order.CartOrdered.Items())
                            if (entry.Product != null) ownedProductIds.Add(entry.Product.definition.id);
                    }

                foreach (string productId in ownedProductIds)
                {
                    var item = IAPManager.GetIAPItem(productId);
                    if (!CanRestoreEntitlement(item)) continue;
                    item.OnProductRestored();
                    restoredCount++;
                }
                restoredEntitlementCount = restoredCount;
                SaveController.SaveChecked();
                IAPManager.OnPurchasesRestored();
                IAPManager.Safely(IAPIntegration.TryFlushPendingTransactions);
#if UNITY_ANDROID
                if (IAPManager.IsRestoreInProgress) CompleteRestore(true, restoredCount, string.Empty);
#endif
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[IAP]: Restore failed: " + exception.Message);
                if (IAPManager.IsRestoreInProgress) CompleteRestore(false, restoredCount, exception.Message);
            }
        }

        private static bool CanRestoreEntitlement(IAPItem item)
        {
            if (item == null || item.ProductType == ProductType.Consumable)
                return false;

            return item.ProductType == ProductType.NonConsumable;
        }

        private void OnPurchaseConfirmed(Order order)
        {
            switch (order)
            {
                case ConfirmedOrder confirmedOrder:
                    OnPurchaseConfirmed(confirmedOrder);
                    break;
                case FailedOrder failedOrder:
                    OnPurchaseConfirmationFailed(failedOrder);
                    break;
                default:
                    Debug.Log("Unknown OnPurchaseConfirmed result.");
                    break;
            }
        }

        private void OnPurchaseConfirmed(ConfirmedOrder order)
        {
            if (!OwnsActivePurchase(order)) return;
            ClearPurchaseInProgress();
            SystemMessage.ChangeLoadingMessage("Payment complete!");
            SystemMessage.HideLoadingPanel();

        }

        private void InvokePurchaseAnalyticsCallback(IAPItem item, Product product, string receipt)
        {
            int callbackIndex = purchaseCallbacks.FindIndex(x => x.ProductKeyType == item.Key);
            if (callbackIndex == -1)
                return;

            PurchaseCallback callback = purchaseCallbacks[callbackIndex];

            Debug.Log("[IAPManager]: IAP Analytics Callback");

            callback.Callback?.Invoke(new PurchaseEvent()
            {
                Item = item,

                Receipt = receipt,

                IsoCurrencyCode = product.metadata.isoCurrencyCode,
                LocalizedPrice = (float)product.metadata.localizedPrice,
                StoreSpecificId = product.definition.storeSpecificId
            });

            purchaseCallbacks.RemoveAt(callbackIndex);
        }

        private void OnPurchaseConfirmationFailed(FailedOrder order)
        {
            foreach (CartItem cartItem in order.CartOrdered.Items())
            {
                Product product = cartItem.Product;

                if (product is null)
                {
                    Debug.Log("[IAPManager]: Could not find product in order.");

                    continue;
                }

                string id = product.definition.id;

                Debug.Log($"Confirmation failed - Product: '{product?.definition.id}'," +
                          $"PurchaseFailureReason: {order.FailureReason.ToString()},"
                          + $"Confirmation Failure Details: {order.Details}");
            }

            if (!OwnsActivePurchase(order)) return;
            ClearPurchaseInProgress();
            // The reward was granted locally before acknowledgement. Unity IAP
            // will deliver the pending order again if Google Play needs a retry.
            SystemMessage.ChangeLoadingMessage("Purchase processed. Store confirmation will retry.");
            SystemMessage.HideLoadingPanel();
        }

        // Calling StoreController.Connect without a listener on the StoreController.OnStoreDisconnected event will result in warnings.
        private void OnStoreDisconnected(StoreConnectionFailureDescription description)
        {
            if (Monetization.VerboseLogging)
                Debug.Log($"Store disconnected details: {description.message}");

            IAPManager.OnInitializationFailed(description.message);
            if (IAPManager.IsRestoreInProgress)
            {
                CompleteRestore(false, restoredEntitlementCount, description.message);
                return;
            }

            if (!isPurchaseInProgress)
                return;

            QueueActivePurchaseAttempt("store_disconnected", "store_disconnected");
            ClearPurchaseInProgress();
            SystemMessage.ChangeLoadingMessage("Store connection lost. Please reconnect and try again.");
            SystemMessage.HideLoadingPanel();
        }

        private void QueuePurchaseAttempt(IAPItem item, string status, string failureReason)
        {
            if (CURRENT_STORE != StoreType.GooglePlay || item == null || string.IsNullOrWhiteSpace(item.ID))
                return;

            IAPManager.Safely(() => IAPIntegration.QueueGooglePlayPurchaseAttempt(
                item.ID,
                item.ProductType.ToString(),
                status,
                failureReason));
        }

        private void QueueActivePurchaseAttempt(string status, string failureReason)
        {
            QueuePurchaseAttempt(activePurchaseItem, status, failureReason);
        }

        private void ClearPurchaseInProgress()
        {
            isPurchaseInProgress = false;
            activePurchaseItem = null;
            IAPManager.FinishPurchase();
        }

        private bool OwnsActivePurchase(Order order)
        {
            if (activePurchaseItem == null || order == null) return false;
            return order.CartOrdered.Items().Any(entry => entry.Product?.definition?.id == activePurchaseItem.ID);
        }

        private static string GetAttemptStatus(string failureReason)
        {
            return string.Equals(failureReason, "UserCancelled", StringComparison.Ordinal)
                ? "cancelled"
                : "failed";
        }

        // Calling StoreController.Connect without listeners on StoreController.OnProductsFetched and StoreController.OnProductsFetchedFailed will result in warnings.
        private void OnProductsFetched(List<Product> products)
        {
            if (Monetization.VerboseLogging)
                Debug.Log($"Products fetched successfully for {products.Count} products.");

            m_StoreController.FetchPurchases();

            IAPManager.OnModuleInitialized();
        }

        private void OnProductsFetchedFailed(ProductFetchFailed failure)
        {
            IAPManager.OnInitializationFailed(failure.FailureReason.ToString());
        }
#endif

        /// <summary>
        /// Restores previously purchased products.
        /// </summary>
        public override void RestorePurchases()
        {
#if MODULE_IAP && UNITY_IAP_NEW
            if (!IAPManager.IsInitialized || m_StoreController == null)
            {
                IAPManager.FinishRestorePurchases();
                SystemMessage.ShowMessage("Google Play is not connected. Please try again shortly.");
                return;
            }

            restoredEntitlementCount = 0;
            SystemMessage.ShowLoadingPanel();
            SystemMessage.ChangeLoadingMessage("Checking Google Play purchases..");

#if UNITY_ANDROID
            m_StoreController.FetchPurchases();
#else
            m_StoreController.RestoreTransactions(OnRestored);
#endif
#endif

#if UNITY_EDITOR && !(MODULE_IAP && UNITY_IAP_NEW)
            OnRestored(true, "");
#endif
        }

        private void OnRestored(bool result, string error)
        {
#if MODULE_IAP && UNITY_IAP_NEW
            CompleteRestore(result, restoredEntitlementCount, error);
#endif
        }

        private void CompleteRestore(bool success, int restoredCount, string error)
        {
            if (!IAPManager.IsRestoreInProgress)
                return;

            IAPManager.FinishRestorePurchases();

            if (success)
            {
                SystemMessage.ChangeLoadingMessage(restoredCount > 0
                    ? "Purchases restored!"
                    : "No previous purchases found.");
            }
            else
            {
                string reason = string.IsNullOrWhiteSpace(error) ? "Please try again." : error;
                SystemMessage.ChangeLoadingMessage("Could not restore purchases: " + reason);
            }

            Tween.DelayedCall(1.25f, () =>
            {
                SystemMessage.HideLoadingPanel();
            }, unscaledTime: true);
        }

        /// <summary>
        /// Initiates the purchase of a product.
        /// </summary>
        /// <param name="productKeyType">The key type of the product to purchase.</param>
        public override void BuyProduct(string productKeyType)
        {
#if MODULE_IAP && UNITY_IAP_NEW
            IAPIntegration.TryFlushPendingTransactions();

            if (!IAPManager.IsInitialized)
            {
                SystemMessage.ShowMessage("Network error. Please try again later");
                return;
            }

            SystemMessage.ShowLoadingPanel();
            SystemMessage.ChangeLoadingMessage("Payment in progress..");

            IAPItem item = IAPManager.GetIAPItemByKey(productKeyType);
            if (item != null && m_StoreController.GetProductById(item.ID) != null)
            {
                IAPManager.Safely(() => AnalyticsController.OnIAPClicked(item));
                
                for (int i = purchaseCallbacks.Count - 1; i >= 0; i--)
                {
                    if (purchaseCallbacks[i].ProductKeyType == productKeyType)
                    {
                        purchaseCallbacks.RemoveAt(i);
                    }
                }

                purchaseCallbacks.Add(new PurchaseCallback(productKeyType, (analyticsData) =>
                {
                    AnalyticsController.OnIAPPurchased(analyticsData);
                }));

                activePurchaseItem = item;
                isPurchaseInProgress = true;
                try
                {
                    m_StoreController.PurchaseProduct(item.ID);
                }
                catch (Exception exception)
                {
                    QueueActivePurchaseAttempt("failed", "purchase_start_failed");
                    IAPManager.OnPurchaseFailed(item.Key, NebulaSoft.PurchaseFailureReason.Unknown);
                    ClearPurchaseInProgress();
                    Debug.LogWarning("[IAP Manager]: Could not start purchase: " + exception.Message);
                    SystemMessage.ChangeLoadingMessage("Payment could not start. Please reconnect and try again.");
                    SystemMessage.HideLoadingPanel();
                }
            }
            else
            {
                IAPManager.OnPurchaseFailed(productKeyType, NebulaSoft.PurchaseFailureReason.ProductUnavailable);
                ClearPurchaseInProgress();
                SystemMessage.ChangeLoadingMessage("This product is unavailable.");
                SystemMessage.HideLoadingPanel();
            }
#else
            SystemMessage.ShowMessage("Network error.");
#endif
        }

        /// <summary>
        /// Gets the product data for a specified product key type.
        /// </summary>
        /// <param name="productKeyType">The key type of the product.</param>
        /// <returns>The product data.</returns>
        public override ProductData GetProductData(string productKeyType)
        {
            if (!IAPManager.IsInitialized)
                return null;

#if MODULE_IAP && UNITY_IAP_NEW
            IAPItem item = IAPManager.GetIAPItemByKey(productKeyType);
            if (item != null)
            {
                Product product = m_StoreController.GetProductById(item.ID);
                if (product != null)
                    return new ProductData(product);
            }
#endif

            return null;
        }

        /// <summary>
        /// Checks if a product is subscribed.
        /// </summary>
        /// <param name="productKeyType">The key type of the product.</param>
        /// <returns>True if the product is subscribed, otherwise false.</returns>
        public override bool IsSubscribed(string productKeyType)
        {
#if MODULE_IAP && UNITY_IAP_NEW
            IAPItem item = IAPManager.GetIAPItemByKey(productKeyType);
            if (item != null)
            {
                return entitlementsService.IsSubscriptionActive(item.ID);
            }
#endif

            return false;
        }

        public override bool IsPurchased(string id)
        {
#if MODULE_IAP && UNITY_IAP_NEW
            return entitlementsService.IsOwned(id);
#else
            return false;
#endif
        }

#if MODULE_IAP && UNITY_IAP_NEW
        private void ConfigureGoogleFraudDetection(IGooglePlayStoreExtendedService googlePlayStoreExtendedService)
        {
            if (googlePlayStoreExtendedService == null)
            {
                Debug.Log("Google Play Store Extended Service is not available. Please make sure the project is being built for Android and the Google Play Store.");

                return;
            }

            googlePlayStoreExtendedService.SetObfuscatedAccountId(fraudDetectionData.AccountId);
        }

        private void ConfigureAppleFraudDetection(IAppleStoreExtendedService appleStoreExtendedService)
        {
            if (appleStoreExtendedService == null)
            {
                Debug.Log("App Store Extended Service is not available. Please make sure the project is being built for Android and the Google Play Store.");

                return;
            }

            appleStoreExtendedService.SetAppAccountToken(fraudDetectionData.AccountToken);
        }
#endif

        public override void Dispose()
        {
#if MODULE_IAP && UNITY_IAP_NEW
            if (m_StoreController == null) return;
            entitlementsService?.Dispose();
            m_StoreController.OnPurchasePending -= OnPurchasePending;
            m_StoreController.OnPurchaseConfirmed -= OnPurchaseConfirmed;
            m_StoreController.OnPurchaseFailed -= OnPurchaseFailed;
            m_StoreController.OnStoreDisconnected -= OnStoreDisconnected;
            m_StoreController.OnProductsFetchFailed -= OnProductsFetchedFailed;
            m_StoreController.OnProductsFetched -= OnProductsFetched;
            m_StoreController.OnPurchasesFetched -= OnPurchasesFetched;
            m_StoreController.OnPurchasesFetchFailed -= OnPurchasesFetchFailed;
            m_StoreController.OnPurchaseDeferred -= OnPurchaseDeferred;
#endif
        }

        private static StoreType GetCurrentStore()
        {
#if UNITY_EDITOR
            return StoreType.Fake;
#elif UNITY_ANDROID
            return StoreType.GooglePlay;
#elif UNITY_IOS
            return StoreType.AppleAppStore;
#elif UNITY_STANDALONE_OSX
            return StoreType.MacAppStore;
#else
            return StoreType.NotSpecified;
#endif
        }

        public enum StoreType
        {
            /// <summary>
            /// No store specified.
            /// </summary>
            NotSpecified = 0,

            /// <summary>
            /// A fake store used for testing and Play-In-Editor.
            /// </summary>
            Fake = 1,

            /// <summary>
            /// GooglePlay Store.
            /// </summary>
            GooglePlay = 2,

            /// <summary>
            /// MacOS App Store.
            /// </summary>
            MacAppStore = 3,

            /// <summary>
            /// iOS, tvOS or visionOS App Stores.
            /// </summary>
            AppleAppStore = 4
        }

        [System.Serializable]
        public class FraudDetectionData
        {
            public string AccountId;
            public Guid AccountToken;

            public FraudDetectionData()
            {
                AccountId = User.GetIdSha256Hex();
                AccountToken = User.GetIdAsGuidFromSha256();
            }
        }

#if MODULE_IAP && UNITY_IAP_NEW
        public sealed class EntitlementsService
        {
            private StoreController store;

            // Non-consumables you own (Unity product ids).
            private readonly HashSet<string> owned = new HashSet<string>(StringComparer.Ordinal);

            // Subscriptions by Unity product id.
            private readonly Dictionary<string, SubscriptionStatusSnapshot> subs = new Dictionary<string, SubscriptionStatusSnapshot>(StringComparer.Ordinal);

            // Last known entitlement status per Unity product id (from CheckEntitlement).
            private readonly Dictionary<string, EntitlementStatus> entitlements = new Dictionary<string, EntitlementStatus>(StringComparer.Ordinal);

            public event Action EntitlementsChanged;

            public EntitlementsService(StoreController store)
            {
                this.store = store ?? throw new ArgumentNullException(nameof(store));

                this.store.OnPurchasesFetched += OnPurchasesFetched;
                this.store.OnPurchaseConfirmed += OnPurchaseConfirmed;
                this.store.OnCheckEntitlement += OnCheckEntitlement;

                this.store.ProcessPendingOrdersOnPurchasesFetched(true);
            }

            public void Dispose()
            {
                store.OnPurchasesFetched -= OnPurchasesFetched;
                store.OnPurchaseConfirmed -= OnPurchaseConfirmed;
                store.OnCheckEntitlement -= OnCheckEntitlement;
            }

            public void RefreshAll() => store?.FetchPurchases();
            public bool IsOwned(string unityProductId) => owned.Contains(unityProductId);
            public bool IsSubscriptionActive(string unityProductId) => subs.TryGetValue(unityProductId, out var s) && s.IsActive;
            public SubscriptionStatusSnapshot GetSubscription(string unityProductId) => subs.TryGetValue(unityProductId, out var s) ? s : default;
            public EntitlementStatus GetEntitlementStatus(string unityProductId) => entitlements.TryGetValue(unityProductId, out var st) ? st : EntitlementStatus.Unknown;

            public IReadOnlyCollection<string> OwnedNonConsumables => new ReadOnlyCollection<string>(owned.ToList());
            public IReadOnlyDictionary<string, SubscriptionStatusSnapshot> Subscriptions => new ReadOnlyDictionary<string, SubscriptionStatusSnapshot>(subs);

            private void OnPurchasesFetched(Orders orders)
            {
                owned.Clear();
                subs.Clear();

                if (orders == null) 
                { 
                    RaiseChanged(); 
                    
                    return; 
                }

                foreach (ConfirmedOrder confirmed in orders.ConfirmedOrders)
                    AddOrder(confirmed);

                RaiseChanged();
            }

            private void OnPurchaseConfirmed(Order order)
            {
                if (order is ConfirmedOrder confirmed)
                {
                    AddOrder(confirmed);

                    RaiseChanged();
                }
            }

            private void OnCheckEntitlement(Entitlement entitlement)
            {
                if (entitlement?.Product == null) return;

                string unityId = entitlement.Product.definition?.id;
                if (string.IsNullOrEmpty(unityId)) return;

                entitlements[unityId] = entitlement.Status;

                if (entitlement.Order is ConfirmedOrder co)
                {
                    AddOrder(co);
                    RaiseChanged();
                }
            }

            public bool RequestEntitlementCheck(string unityProductId)
            {
                Product p = store?.GetProductById(unityProductId);
                if (p == null) return false;

                store.CheckEntitlement(p);

                return true;
            }
            
            private void AddOrder(ConfirmedOrder confirmed)
            {
                if (confirmed?.Info?.PurchasedProductInfo == null)
                    return;

                foreach (IPurchasedProductInfo ppi in confirmed.Info.PurchasedProductInfo)
                {
                    string unityId = ResolveUnityProductId(ppi);
                    if (unityId == null)
                        continue;

                    Product product = store.GetProductById(unityId);
                    UnityEngine.Purchasing.ProductType pType = product?.definition?.type ?? UnityEngine.Purchasing.ProductType.Unknown;

                    switch (pType)
                    {
                        case UnityEngine.Purchasing.ProductType.NonConsumable:
                            owned.Add(unityId);
                            break;

                        case UnityEngine.Purchasing.ProductType.Subscription:
                            SubscriptionStatusSnapshot snap = BuildSubscriptionSnapshot(unityId, ppi);
                            subs[unityId] = snap;
                            break;
                    }
                }
            }

            /// <summary>
            /// In different stores, the productId in the receipt may contain either the Unity ID or the store-specific ID.
            /// We make several attempts to match it with the catalog.
            /// </summary>
            private string ResolveUnityProductId(IPurchasedProductInfo ppi)
            {
                if (ppi == null) return null;

                // 1) Direct attempt: is this already the Unity ID?
                if (!string.IsNullOrEmpty(ppi.productId) && store.GetProductById(ppi.productId) != null)
                    return ppi.productId;

                // 2) Attempt using the storeSpecificId.
                ReadOnlyObservableCollection<Product> all = store.GetProducts();
                Product fromStoreSpecific = all.FirstOrDefault(p => string.Equals(p.definition?.storeSpecificId, ppi.productId, StringComparison.OrdinalIgnoreCase));

                if (fromStoreSpecific != null)
                    return fromStoreSpecific.definition.id;

                // 3) For subscriptions: extract the store ID from SubscriptionInfo and match it.
                string storePid = ppi.subscriptionInfo?.GetProductId();
                if (!string.IsNullOrEmpty(storePid))
                {
                    Product bySubStoreId = all.FirstOrDefault(p => string.Equals(p.definition?.storeSpecificId, storePid, StringComparison.OrdinalIgnoreCase));

                    if (bySubStoreId != null)
                        return bySubStoreId.definition.id;
                }

                Debug.LogWarning($"[IapEntitlements] Unknown purchased product id: '{ppi.productId}'. Ensure your catalog IDs match store product IDs.");

                return null;
            }

            private static SubscriptionStatusSnapshot BuildSubscriptionSnapshot(string unityProductId, IPurchasedProductInfo ppi)
            {
                SubscriptionInfo si = ppi.subscriptionInfo;
                if (si == null)
                    return new SubscriptionStatusSnapshot(unityProductId, false, null, null, false, false, null, null, null);

                Result expired = si.IsExpired(); // Result
                bool isActive = expired == Result.False;

                DateTime? expireAt = null;
                try { expireAt = si.GetExpireDate(); }
                catch { /* platform may not provide */ }

                TimeSpan? remaining = null;
                try { remaining = si.GetRemainingTime(); }
                catch { }

                bool autoRenews = false;
                try { autoRenews = si.IsAutoRenewing() == Result.True; }
                catch { }

                bool cancelled = false;
                try { cancelled = si.IsCancelled() == Result.True; }
                catch { }

                DateTime? purchaseDate = null;
                try { purchaseDate = si.GetPurchaseDate(); }
                catch { }

                string storeProductId = null;
                try { storeProductId = si.GetProductId(); }
                catch { }

                string payload = null;
                try { payload = si.GetSubscriptionInfoJsonString(); }
                catch { }

                return new SubscriptionStatusSnapshot(
                    unityProductId,
                    isActive,
                    expireAt,
                    remaining,
                    autoRenews,
                    cancelled,
                    storeProductId,
                    purchaseDate,
                    payload
                );
            }

            private void RaiseChanged() => EntitlementsChanged?.Invoke();
        }

        /// <summary>
        /// Immutable snapshot of a subscription state. Safe to cache or serialize to diagnostics.
        /// </summary>
        public readonly struct SubscriptionStatusSnapshot
        {
            public readonly string UnityProductId;
            public readonly bool IsActive;
            public readonly DateTime? ExpireAtUtc;
            public readonly TimeSpan? Remaining;
            public readonly bool AutoRenews;
            public readonly bool IsCancelled;
            public readonly string StoreProductId;
            public readonly DateTime? PurchaseDateUtc;
            public readonly string RawJson;

            public SubscriptionStatusSnapshot(string unityProductId, bool isActive, DateTime? expireAtUtc, TimeSpan? remaining, bool autoRenews, bool isCancelled, string storeProductId, DateTime? purchaseDateUtc, string rawJson)
            {
                UnityProductId = unityProductId;
                IsActive = isActive;
                ExpireAtUtc = expireAtUtc;
                Remaining = remaining;
                AutoRenews = autoRenews;
                IsCancelled = isCancelled;
                StoreProductId = storeProductId;
                PurchaseDateUtc = purchaseDateUtc;
                RawJson = rawJson;
            }

            public override string ToString() => $"[{UnityProductId}] Active={IsActive}, Expire={ExpireAtUtc:O}, AutoRenews={AutoRenews}, Cancelled={IsCancelled}";
        }

        private class PurchaseCallback
        {
            public string ProductKeyType { get; private set; }
            public PurchaseCallbackDelegate Callback { get; private set; }

            public PurchaseCallback(string productKeyType, PurchaseCallbackDelegate callback)
            {
                ProductKeyType = productKeyType;
                Callback = callback;
            }

            public delegate void PurchaseCallbackDelegate(PurchaseEvent analyticsIAPData);
        }
#endif
    }
}
