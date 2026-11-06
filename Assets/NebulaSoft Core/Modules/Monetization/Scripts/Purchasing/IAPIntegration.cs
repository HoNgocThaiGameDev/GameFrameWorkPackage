using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace NebulaSoft
{
    /// <summary>Optional application integrations. Core IAP has no dependency on a game or cloud provider.</summary>
    [StaticUnload]
    public static class IAPIntegration
    {
        public static event Action FlushRequested;
        public static event Action RestoreEntitlementsRequested;
        public static event Action<string, string, string, string> PurchaseLogRequested;
        public static event Action<string, string, string, string> PurchaseAttemptLogRequested;

        public static void TryFlushPendingTransactions() => Notify(FlushRequested);
        public static void RestoreEntitlements() => Notify(RestoreEntitlementsRequested);
        public static void QueueGooglePlayPurchase(string productId, string type, string transactionId, string source) =>
            Notify(PurchaseLogRequested, productId, type, transactionId, source);
        public static void QueueGooglePlayPurchaseAttempt(string productId, string type, string status, string reason) =>
            Notify(PurchaseAttemptLogRequested, productId, type, status, reason);

        public static string GetTransactionIdHash(string transactionId)
        {
            if (string.IsNullOrWhiteSpace(transactionId)) return null;
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(transactionId.Trim()));
                var result = new StringBuilder(bytes.Length * 2);
                foreach (byte value in bytes) result.Append(value.ToString("x2"));
                return result.ToString();
            }
        }

        private static void Notify(Delegate callbacks, params object[] arguments)
        {
            if (callbacks == null) return;
            foreach (var callback in callbacks.GetInvocationList())
            {
                try { callback.DynamicInvoke(arguments); }
                catch (Exception exception) { Debug.LogException(exception); }
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void UnloadStatic()
        {
            FlushRequested = null;
            RestoreEntitlementsRequested = null;
            PurchaseLogRequested = null;
            PurchaseAttemptLogRequested = null;
        }
    }
}
