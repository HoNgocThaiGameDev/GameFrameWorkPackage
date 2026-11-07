using System.Collections.Generic;
using UnityEngine;

#if MODULE_IAP
using UnityEngine.Purchasing;
#endif

namespace NebulaSoft
{
    [System.Serializable]
    public sealed class IAPPendingTransactionLog
    {
        public string DocumentId;
        public string ProductId;
        public string ProductType;
        public string Platform;
        public string GooglePlayOrderId;
        public string TransactionIdHash;
        public string AppVersion;
        public string ExpectedFirebaseUid;
        public string PlayerNameAtPurchase;
        public int PlayerAvatarAtPurchase;
        public int PlayerFrameAtPurchase;
        public bool HasProfileSnapshot;
        public string ProfileSnapshotSource;
    }

    [System.Serializable]
    public sealed class IAPPendingPurchaseAttemptLog
    {
        public string DocumentId;
        public string ProductId;
        public string ProductType;
        public string Platform;
        public string Status;
        public string FailureReason;
        public string AppVersion;
        public string ExpectedFirebaseUid;
        public string PlayerNameAtAttempt;
        public int PlayerAvatarAtAttempt;
        public int PlayerFrameAtAttempt;
        public bool HasProfileSnapshot;
        public string ProfileSnapshotSource;
    }

    [StaticUnload]
    public static class IAPManager
    {
        public static bool IsInitialized { get; private set; }
        public static bool IsInitializing { get; private set; }
        public static bool IsRestoreInProgress { get; private set; }
        public static string ActivePurchaseKey { get; private set; }
        public static bool IsPurchaseInProgress => !string.IsNullOrEmpty(ActivePurchaseKey);
        public static IAPSettings Settings => settings;
        public static bool IsEnabled => settings != null && settings.IsEnabled && (hasModuleConfiguration || Monetization.IsActive);

        private static IAPSettings settings;
        private static IAPWrapper wrapper;
        private static Save save;
        private static bool hasModuleConfiguration;
        private static readonly HashSet<string> awaitingPresentation = new HashSet<string>();
        private static readonly HashSet<string> fulfilling = new HashSet<string>();

        public static event SimpleCallback Initialized;
        public static event SimpleCallback PurchasesRestored;
        public static event SimpleCallback StateChanged;
        public static event ProductCallback PurchaseCompleted;
        public static event ProductFailCallback PurchaseFailed;
        public static event System.Action<string> ProductPurchaseCompleted;
        public static event System.Action<string, PurchaseFailureReason> ProductPurchaseFailed;
        public static event System.Action<IAPItem> PurchaseRewardPresentationRequested;

        public static void ConfigureModule(IAPSettings moduleSettings)
        {
            if (IsInitialized || IsInitializing) return;
            hasModuleConfiguration = true;
            settings = moduleSettings;
            Notify(StateChanged);
        }

        public static void Init(MonetizationSettings monetizationSettings)
        {
            if (!hasModuleConfiguration && !IsInitialized && !IsInitializing)
                settings = monetizationSettings != null ? monetizationSettings.IAPSettings : null;
            StartInitialization();
        }

        public static void Init(IAPSettings iapSettings)
        {
            ConfigureModule(iapSettings);
            StartInitialization();
        }

        private static async void StartInitialization()
        {
            if (IsInitialized || IsInitializing || !IsEnabled) return;
            if (!SaveController.IsSaveLoaded)
            {
                Debug.LogError("[IAP]: Initialize Save before IAP.");
                return;
            }
            string platform = null;
#if UNITY_ANDROID
            platform = "Android";
#elif UNITY_IOS
            platform = "iOS";
#endif
            var errors = settings.ValidateCatalog(platform);
            if (errors.Count > 0)
            {
                Debug.LogError("[IAP]: Invalid catalog:\n" + string.Join("\n", errors), settings);
                return;
            }
            IsInitializing = true;
            Notify(StateChanged);
            try
            {
                save = SaveController.GetSaveObject<Save>("iapGlobalSave");
                foreach (var item in settings.StoreItems ?? new IAPItem[0]) item.Init();
                wrapper?.Dispose();
                wrapper = GetPlatformWrapper();
                await wrapper.Init(settings);
                IAPIntegration.TryFlushPendingTransactions();
            }
            catch (System.Exception exception)
            {
                OnInitializationFailed(exception.Message);
            }
        }

        public static void OnInitializationFailed(string message)
        {
            IsInitialized = false;
            IsInitializing = false;
            Debug.LogWarning("[IAP]: Initialization failed: " + message);
            Notify(StateChanged);
        }

        public static IAPItem GetIAPItemByKey(string key) => settings != null ? settings.FindByKey(key) : null;
        public static IAPItem GetIAPItem(ProductKeyType key) => settings != null ? settings.FindLegacy(key) : null;
        // Compatibility: the string argument to this method is a STORE id.
        public static IAPItem GetIAPItem(string productID)
        {
            if (string.IsNullOrEmpty(productID) || settings == null || settings.StoreItems == null) return null;
            foreach (var item in settings.StoreItems)
                if (item != null && item.ID == productID) return item;
            return null;
        }

        public static void RestorePurchases()
        {
            if (!IsEnabled || !IsInitialized || IsRestoreInProgress || IsPurchaseInProgress) return;
            IsRestoreInProgress = true;
            Notify(StateChanged);
            try { wrapper.RestorePurchases(); }
            catch (System.Exception exception)
            {
                Debug.LogException(exception);
                FinishRestorePurchases();
                Safely(SystemMessage.HideLoadingPanel);
            }
        }

        public static void FinishRestorePurchases()
        {
            IsRestoreInProgress = false;
            Notify(StateChanged);
        }

        public static void SubscribeOnPurchaseModuleInitted(SimpleCallback callback)
        {
            if (IsInitialized) Notify(callback);
            else Initialized += callback;
        }

        public static void BuyProduct(ProductKeyType key) => BuyProduct(GetIAPItem(key)?.Key);
        public static void BuyProduct(string key)
        {
            if (!IsEnabled || !IsInitialized)
            {
                OnPurchaseFailed(key, PurchaseFailureReason.PurchasingUnavailable);
                return;
            }
            if (IsPurchaseInProgress || IsRestoreInProgress)
            {
                OnPurchaseFailed(key, PurchaseFailureReason.ExistingPurchasePending);
                return;
            }
            var item = GetIAPItemByKey(key);
            if (item == null || (item.ProductType != ProductType.Consumable && IsPurchased(key)))
            {
                OnPurchaseFailed(key, PurchaseFailureReason.ProductUnavailable);
                return;
            }
            ActivePurchaseKey = key;
            Notify(StateChanged);
            try { wrapper.BuyProduct(key); }
            catch (System.Exception exception)
            {
                Debug.LogException(exception);
                OnPurchaseFailed(key, PurchaseFailureReason.Unknown);
                FinishPurchase();
                Safely(SystemMessage.HideLoadingPanel);
            }
        }

        public static ProductData GetProductData(ProductKeyType key) => GetProductData(GetIAPItem(key)?.Key);
        public static ProductData GetProductData(string key) => IsEnabled && IsInitialized && GetIAPItemByKey(key) != null ? wrapper.GetProductData(key) : null;
        public static string GetProductLocalPriceString(ProductKeyType key) => GetProductData(key)?.GetLocalPrice() ?? string.Empty;
        public static string GetProductLocalPriceString(string key) => GetProductData(key)?.GetLocalPrice() ?? string.Empty;
        public static bool IsSubscribed(ProductKeyType key) => IsSubscribed(GetIAPItem(key)?.Key);
        public static bool IsSubscribed(string key) => IsEnabled && IsInitialized && GetIAPItemByKey(key) != null && wrapper.IsSubscribed(key);
        public static bool IsPurchased(ProductKeyType key) => IsPurchased(GetIAPItem(key)?.Key);
        public static bool IsPurchased(string key)
        {
            var item = GetIAPItemByKey(key);
            if (!IsInitialized || item == null) return false;
            if (item.ProductType == ProductType.Subscription) return IsSubscribed(key);
            if (item.ProductType == ProductType.Consumable) return false;
            return item.TimesPurchased > 0 || wrapper.IsPurchased(item.ID);
        }

        public static void OnModuleInitialized()
        {
            IsInitializing = false;
            IsInitialized = true;
            Notify(Initialized);
            Notify(StateChanged);
        }

        public static void FinishPurchase()
        {
            ActivePurchaseKey = null;
            Notify(StateChanged);
        }

        // Adapter entrypoint: reward + persisted deduplication precede UI callbacks.
        public static bool TryFulfillPurchase(IAPItem item, string transactionHash)
        {
            if (item == null || save == null || string.IsNullOrWhiteSpace(transactionHash)) return false;
            if (!fulfilling.Add(transactionHash)) return false;
            try
            {
                using (CurrencyController.DeferNotifications())
                {
                    if (IsIAPTransactionFulfilled(transactionHash))
                    {
                        SaveController.SaveChecked();
                        PresentPurchaseOnce(item, transactionHash);
                        return true;
                    }
                    if (save.PendingFulfillments == null) save.PendingFulfillments = new List<PendingFulfillment>();
                    var progress = save.PendingFulfillments.Find(value => value.TransactionHash == transactionHash);
                    if (progress == null)
                    {
                        progress = new PendingFulfillment { TransactionHash = transactionHash, ProductKey = item.Key };
                        save.PendingFulfillments.Add(progress);
                    }
                    if (progress.ProductKey != item.Key)
                        throw new System.InvalidOperationException("A transaction cannot fulfill two different products.");
                    item.ApplyPurchaseRewards(progress.NextRewardIndex, next =>
                    {
                        progress.NextRewardIndex = next;
                        SaveController.MarkAsSaveIsRequired();
                        SaveController.SaveChecked();
                    });
                    item.RecordPurchase();
                    MarkIAPTransactionFulfilled(transactionHash);
                    save.PendingFulfillments.Remove(progress);
                    bool firstPurchase = !save.FirstPurchase;
                    save.FirstPurchase = true;
                    awaitingPresentation.Add(transactionHash);
                    SaveController.SaveChecked();
                    if (firstPurchase) Safely(() => AnalyticsController.TrackEvent(AnalyticsEventType.IAPFirstPurchase));
                    PresentPurchaseOnce(item, transactionHash);
                    return true;
                }
            }
            finally { fulfilling.Remove(transactionHash); }
        }

        private static void PresentPurchaseOnce(IAPItem item, string transactionHash)
        {
            if (!awaitingPresentation.Remove(transactionHash)) return;
            item.RewardsSet?.NotifyRewardReceived();
            NotifyProductCompleted(item);
        }

        public static void OnPurchaseCompleted(IAPItem item)
        {
            // Kept for callers without a store transaction. Adapters use TryFulfillPurchase.
            TryFulfillPurchase(item, "local_" + System.Guid.NewGuid().ToString("N"));
        }

        private static void NotifyProductCompleted(IAPItem item)
        {
            if (item.HasLegacyKey) Notify(PurchaseCompleted, item.ProductKeyType);
            Notify(ProductPurchaseCompleted, item.Key);
            Notify(PurchaseRewardPresentationRequested, item);
            Notify(StateChanged);
        }

        public static void OnPurchasesRestored()
        {
            Notify(PurchasesRestored);
            Safely(IAPIntegration.RestoreEntitlements);
            Notify(StateChanged);
        }

        public static void QueuePendingIAPTransaction(IAPPendingTransactionLog transaction)
        {
            if (save == null || transaction == null || string.IsNullOrWhiteSpace(transaction.DocumentId))
                return;

            if (save.PendingTransactions == null)
                save.PendingTransactions = new List<IAPPendingTransactionLog>();

            if (save.PendingTransactions.Exists(entry => entry != null && entry.DocumentId == transaction.DocumentId))
                return;

            save.PendingTransactions.Add(transaction);
            SaveController.MarkAsSaveIsRequired();
            SaveController.Save(true, false);
        }

        public static List<IAPPendingTransactionLog> GetPendingIAPTransactions()
        {
            if (save == null || save.PendingTransactions == null)
                return new List<IAPPendingTransactionLog>();

            return new List<IAPPendingTransactionLog>(save.PendingTransactions);
        }

        public static void MarkIAPTransactionLogged(string documentId)
        {
            if (save == null || save.PendingTransactions == null || string.IsNullOrWhiteSpace(documentId))
                return;

            int removedCount = save.PendingTransactions.RemoveAll(entry => entry != null && entry.DocumentId == documentId);
            if (removedCount > 0)
                SaveController.MarkAsSaveIsRequired();
        }

        public static void QueuePendingIAPAttempt(IAPPendingPurchaseAttemptLog attempt)
        {
            if (save == null || attempt == null || string.IsNullOrWhiteSpace(attempt.DocumentId))
                return;

            if (save.PendingAttempts == null)
                save.PendingAttempts = new List<IAPPendingPurchaseAttemptLog>();

            if (save.PendingAttempts.Exists(entry => entry != null && entry.DocumentId == attempt.DocumentId))
                return;

            save.PendingAttempts.Add(attempt);
            SaveController.MarkAsSaveIsRequired();
            SaveController.Save(true, false);
        }

        public static List<IAPPendingPurchaseAttemptLog> GetPendingIAPAttempts()
        {
            if (save == null || save.PendingAttempts == null)
                return new List<IAPPendingPurchaseAttemptLog>();

            return new List<IAPPendingPurchaseAttemptLog>(save.PendingAttempts);
        }

        public static void MarkIAPAttemptLogged(string documentId)
        {
            if (save == null || save.PendingAttempts == null || string.IsNullOrWhiteSpace(documentId))
                return;

            int removedCount = save.PendingAttempts.RemoveAll(entry => entry != null && entry.DocumentId == documentId);
            if (removedCount > 0)
                SaveController.MarkAsSaveIsRequired();
        }

        public static bool IsIAPTransactionFulfilled(string transactionHash) => save != null && !string.IsNullOrWhiteSpace(transactionHash) && save.FulfilledTransactionHashes != null && save.FulfilledTransactionHashes.Contains(transactionHash);

        public static void MarkIAPTransactionFulfilled(string transactionHash)
        {
            if (save == null || string.IsNullOrWhiteSpace(transactionHash)) return;
            if (save.FulfilledTransactionHashes == null) save.FulfilledTransactionHashes = new List<string>();
            if (!save.FulfilledTransactionHashes.Contains(transactionHash)) save.FulfilledTransactionHashes.Add(transactionHash);
            // Retain transaction ids: evicting an unconfirmed order can grant it again.
            SaveController.MarkAsSaveIsRequired();
        }

        public static bool IsPayableUser() => save != null && save.FirstPurchase;
        public static void OnPurchaseFailed(ProductKeyType key, PurchaseFailureReason reason) => OnPurchaseFailed(GetIAPItem(key)?.Key, reason);
        public static void OnPurchaseFailed(string key, PurchaseFailureReason reason)
        {
            var item = GetIAPItemByKey(key);
            if (item != null && item.HasLegacyKey) Notify(PurchaseFailed, item.ProductKeyType, reason);
            Notify(ProductPurchaseFailed, key, reason);
        }

        internal static void Safely(System.Action callback)
        {
            try { callback?.Invoke(); }
            catch (System.Exception exception) { Debug.LogException(exception); }
        }

        private static void Notify(System.Delegate callbacks, params object[] args)
        {
            if (callbacks == null) return;
            foreach (var callback in callbacks.GetInvocationList())
            {
                if (callback.Target is UnityEngine.Object target && target == null) continue;
                Safely(() => callback.DynamicInvoke(args));
            }
        }

        private static IAPWrapper GetPlatformWrapper()
        {
#if MODULE_IAP
#if UNITY_IAP_NEW
            return settings.UseFakeStore ? (IAPWrapper)new DummyIAPWrapper() : new UnityIAP5Wrapper();
#else
            return new UnityIAPWrapper();
#endif
#else
            return new DummyIAPWrapper();
#endif
        }

        public static void Shutdown() => UnloadStatic();

        private static void UnloadStatic()
        {
            wrapper?.Dispose();
            IsInitialized = false;
            IsInitializing = false;
            IsRestoreInProgress = false;
            ActivePurchaseKey = null;
            wrapper = null;
            settings = null;
            save = null;
            hasModuleConfiguration = false;
            awaitingPresentation.Clear();
            fulfilling.Clear();
            Initialized = null;
            PurchasesRestored = null;
            StateChanged = null;
            PurchaseCompleted = null;
            PurchaseFailed = null;
            ProductPurchaseCompleted = null;
            ProductPurchaseFailed = null;
            PurchaseRewardPresentationRequested = null;
        }

        public delegate void ProductCallback(ProductKeyType productKeyType);
        public delegate void ProductFailCallback(ProductKeyType productKeyType, PurchaseFailureReason failureReason);

        [System.Serializable]
        public sealed class PendingFulfillment
        {
            public string TransactionHash;
            public string ProductKey;
            public int NextRewardIndex;
        }

        [System.Serializable]
        public class Save : ISaveObject
        {
            public bool FirstPurchase = false;
            public List<IAPPendingTransactionLog> PendingTransactions = new List<IAPPendingTransactionLog>();
            public List<IAPPendingPurchaseAttemptLog> PendingAttempts = new List<IAPPendingPurchaseAttemptLog>();
            public List<string> FulfilledTransactionHashes = new List<string>();
            public List<PendingFulfillment> PendingFulfillments = new List<PendingFulfillment>();

            public void Flush()
            {

            }
        }
    }
}
