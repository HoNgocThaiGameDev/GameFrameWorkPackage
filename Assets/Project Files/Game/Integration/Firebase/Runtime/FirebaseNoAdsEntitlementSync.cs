using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

using NebulaSoft.Cloud;

namespace NebulaSoft
{
    /// <summary>
    /// Mirrors the locally reported No Ads purchase to Firestore and restores the
    /// entitlement for the currently signed-in Firebase user. Google Play remains
    /// the fallback when no cloud entitlement document exists.
    /// </summary>
    public static class FirebaseNoAdsEntitlementSync
    {
        private const string EntitlementsCollection = "NoAdsEntitlements";
        private const int SchemaVersion = 1;

        private static bool hasPendingCloudState;
        private static bool pendingDocumentExists;
        private static bool pendingNoAds;

        public static bool GrantsNoAds(string productId)
        {
            if (string.IsNullOrWhiteSpace(productId) || !IAPManager.IsInitialized)
                return false;

            return IsProductId(ProductKeyType.NoAds, productId)
                || IsProductId(ProductKeyType.StarterPack, productId);
        }

        public static void RestoreForCurrentUser()
        {
            if (!Application.isPlaying || FirebaseAuthHandler.Documents == null)
                return;

            string uid = FirebaseAuthHandler.CurrentUserId;
            if (string.IsNullOrWhiteSpace(uid))
                return;

            RestoreForCurrentUserAsync(uid);
        }

        public static void ApplyPendingCloudState()
        {
            if (!hasPendingCloudState)
                return;

            ApplyCloudState(pendingDocumentExists, pendingNoAds);
        }

        public static async Task<bool> TryCreateNoAdsEntitlementAsync(
            string uid,
            IAPPendingTransactionLog transaction)
        {
            if (transaction == null || !GrantsNoAds(transaction.ProductId))
                return true;

            if (string.IsNullOrWhiteSpace(uid)
                || string.IsNullOrWhiteSpace(transaction.TransactionIdHash)
                || FirebaseAuthHandler.Documents == null
                || !string.Equals(FirebaseAuthHandler.CurrentUserId, uid, StringComparison.Ordinal))
            {
                return false;
            }

            try
            {
                CloudDocumentReference entitlementDocument = FirebaseAuthHandler.Documents
                    .Collection(EntitlementsCollection)
                    .Document(uid);

                await FirebaseAuthHandler.Documents.RunTransactionAsync(async firestoreTransaction =>
                {
                    CloudDocumentSnapshot snapshot = await firestoreTransaction.GetSnapshotAsync(entitlementDocument);
                    if (snapshot.Exists)
                        return;

                    firestoreTransaction.Set(entitlementDocument, new Dictionary<string, object>
                    {
                        ["uid"] = uid,
                        ["noAds"] = true,
                        ["sourceProductId"] = transaction.ProductId,
                        ["sourceTransactionIdHash"] = transaction.TransactionIdHash,
                        ["platform"] = transaction.Platform,
                        ["grantedAt"] = CloudFieldValue.ServerTimestamp,
                        ["schemaVersion"] = SchemaVersion
                    });
                });

                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[Firebase IAP] No Ads entitlement upload deferred: " + exception.Message);
                return false;
            }
        }

        private static async void RestoreForCurrentUserAsync(string uid)
        {
            try
            {
                CloudDocumentReference entitlementDocument = FirebaseAuthHandler.Documents
                    .Collection(EntitlementsCollection)
                    .Document(uid);

                CloudDocumentSnapshot snapshot = await entitlementDocument.GetSnapshotAsync();
                if (!string.Equals(FirebaseAuthHandler.CurrentUserId, uid, StringComparison.Ordinal))
                    return;

                if (!snapshot.Exists)
                {
                    SetPendingCloudState(documentExists: false, noAds: false);
                    return;
                }

                Dictionary<string, object> data = snapshot.ToDictionary();
                if (!data.TryGetValue("noAds", out object rawNoAds) || !(rawNoAds is bool noAds))
                {
                    Debug.LogWarning("[Firebase IAP] No Ads entitlement document has an invalid noAds value.");
                    return;
                }

                SetPendingCloudState(documentExists: true, noAds: noAds);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[Firebase IAP] No Ads entitlement restore deferred: " + exception.Message);
            }
        }

        private static bool IsProductId(ProductKeyType productKey, string productId)
        {
            IAPItem item = IAPManager.GetIAPItem(productKey);
            return item != null && string.Equals(item.ID, productId, StringComparison.Ordinal);
        }

        private static void SetPendingCloudState(bool documentExists, bool noAds)
        {
            hasPendingCloudState = true;
            pendingDocumentExists = documentExists;
            pendingNoAds = noAds;
            ApplyCloudState(documentExists, noAds);
        }

        private static void ApplyCloudState(bool documentExists, bool noAds)
        {
            if (!SaveController.IsSaveLoaded || AdsManager.Settings == null)
                return;

            if (documentExists)
            {
                AdsManager.SetNoAdsEntitlement(noAds);
                return;
            }

            if (!HasGooglePlayNoAdsEntitlement())
                AdsManager.SetNoAdsEntitlement(false);
        }

        private static bool HasGooglePlayNoAdsEntitlement()
        {
            return IAPManager.IsInitialized
                && (IAPManager.IsPurchased(ProductKeyType.NoAds)
                    || IAPManager.IsPurchased(ProductKeyType.StarterPack));
        }
    }
}
