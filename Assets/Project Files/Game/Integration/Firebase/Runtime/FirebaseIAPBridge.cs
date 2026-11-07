using UnityEngine;

namespace NebulaSoft
{
    internal static class FirebaseIAPBridge
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            IAPIntegration.FlushRequested -= FirebaseIAPTransactionLogger.TryFlushPendingTransactions;
            IAPIntegration.RestoreEntitlementsRequested -= FirebaseNoAdsEntitlementSync.RestoreForCurrentUser;
            IAPIntegration.PurchaseLogRequested -= FirebaseIAPTransactionLogger.QueueGooglePlayPurchase;
            IAPIntegration.PurchaseAttemptLogRequested -= FirebaseIAPTransactionLogger.QueueGooglePlayPurchaseAttempt;
            IAPIntegration.FlushRequested += FirebaseIAPTransactionLogger.TryFlushPendingTransactions;
            IAPIntegration.RestoreEntitlementsRequested += FirebaseNoAdsEntitlementSync.RestoreForCurrentUser;
            IAPIntegration.PurchaseLogRequested += FirebaseIAPTransactionLogger.QueueGooglePlayPurchase;
            IAPIntegration.PurchaseAttemptLogRequested += FirebaseIAPTransactionLogger.QueueGooglePlayPurchaseAttempt;
        }
    }
}
