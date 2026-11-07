using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

using NebulaSoft.Cloud;

namespace NebulaSoft
{
    /// <summary>
    /// Stores a client-reported IAP history entry after Google Play reports a
    /// successful purchase. This is intentionally not payment verification.
    /// </summary>
    public static class FirebaseIAPTransactionLogger
    {
        private const string PlayersCollection = "Players";
        private const string TransactionsCollection = "Transactions";
        private const string AttemptsCollection = "IAPAttempts";
        private const int TransactionSchemaVersion = 3;
        private const int AttemptSchemaVersion = 1;

        private static bool isFlushing;
        private static bool isFlushingAttempts;

        public static void QueueGooglePlayPurchase(
            string productId,
            string productType,
            string transactionId,
            string profileSnapshotSource = "purchase_callback")
        {
            if (string.IsNullOrWhiteSpace(productId) || string.IsNullOrWhiteSpace(transactionId))
            {
                if (!string.IsNullOrWhiteSpace(productId))
                    Debug.LogWarning("[Firebase IAP] The Google Play order has no transaction ID. Transaction logging was skipped.");
                return;
            }

            string googlePlayOrderId = transactionId.Trim();
            string transactionHash = GetTransactionIdHash(googlePlayOrderId);
            IAPPendingTransactionLog transaction = new IAPPendingTransactionLog
            {
                DocumentId = transactionHash,
                ProductId = productId.Trim(),
                ProductType = string.IsNullOrWhiteSpace(productType) ? "Unknown" : productType.Trim(),
                Platform = "google_play",
                GooglePlayOrderId = googlePlayOrderId,
                TransactionIdHash = transactionHash,
                AppVersion = Application.version ?? string.Empty,
                ExpectedFirebaseUid = GetCurrentFirebaseUid(),
                ProfileSnapshotSource = NormalizeProfileSnapshotSource(profileSnapshotSource)
            };

            CapturePlayerProfileSnapshot(transaction);

            IAPManager.QueuePendingIAPTransaction(transaction);
            TryFlushPendingTransactions();
        }

        public static void QueueGooglePlayPurchaseAttempt(
            string productId,
            string productType,
            string status,
            string failureReason)
        {
            if (string.IsNullOrWhiteSpace(productId))
                return;

            IAPPendingPurchaseAttemptLog attempt = new IAPPendingPurchaseAttemptLog
            {
                DocumentId = Guid.NewGuid().ToString("N"),
                ProductId = productId.Trim(),
                ProductType = string.IsNullOrWhiteSpace(productType) ? "Unknown" : productType.Trim(),
                Platform = "google_play",
                Status = NormalizeAttemptStatus(status),
                FailureReason = string.IsNullOrWhiteSpace(failureReason) ? "unknown" : failureReason.Trim(),
                AppVersion = Application.version ?? string.Empty,
                ExpectedFirebaseUid = GetCurrentFirebaseUid(),
                ProfileSnapshotSource = "attempt_callback"
            };

            CapturePlayerProfileSnapshot(attempt);

            IAPManager.QueuePendingIAPAttempt(attempt);
            TryFlushPendingTransactions();
        }

        public static void TryFlushPendingTransactions()
        {
            if (!isFlushing)
                FlushPendingTransactionsAsync();

            if (!isFlushingAttempts)
                FlushPendingAttemptsAsync();
        }

        public static string GetTransactionIdHash(string transactionId)
        {
            return string.IsNullOrWhiteSpace(transactionId) ? null : Sha256Hex(transactionId.Trim());
        }

        private static async void FlushPendingTransactionsAsync()
        {
            isFlushing = true;
            try
            {
                List<IAPPendingTransactionLog> pendingTransactions = IAPManager.GetPendingIAPTransactions();
                if (pendingTransactions.Count == 0)
                    return;

                if (!await FirebaseAuthHandler.EnsureSignedInAsync())
                {
                    Debug.LogWarning("[Firebase IAP] Transaction log deferred because Firebase Auth is unavailable.");
                    return;
                }

                string uid = FirebaseAuthHandler.CurrentUserId;
                if (string.IsNullOrWhiteSpace(uid) || FirebaseAuthHandler.Documents == null)
                {
                    Debug.LogWarning("[Firebase IAP] Transaction log deferred because Firestore is unavailable.");
                    return;
                }

                foreach (IAPPendingTransactionLog transaction in pendingTransactions)
                {
                    if (transaction == null || string.IsNullOrWhiteSpace(transaction.DocumentId))
                        continue;

                    if (!string.IsNullOrWhiteSpace(transaction.ExpectedFirebaseUid)
                        && !string.Equals(transaction.ExpectedFirebaseUid, uid, StringComparison.Ordinal))
                    {
                        Debug.LogWarning("[Firebase IAP] Transaction log is waiting for its original Firebase account.");
                        continue;
                    }

                    if (!await TryWriteTransactionAsync(uid, transaction))
                        break;

                    IAPManager.MarkIAPTransactionLogged(transaction.DocumentId);
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[Firebase IAP] Transaction log upload deferred (" + exception.GetType().Name + ").");
            }
            finally
            {
                isFlushing = false;
            }
        }

        private static async Task<bool> TryWriteTransactionAsync(string uid, IAPPendingTransactionLog transaction)
        {
            // Pending entries created before schema v3 only contain the hash, so
            // their raw Google Play order ID cannot be reconstructed. Do not keep
            // retrying them forever; a later Restore can still log owned
            // non-consumables with their order ID.
            if (string.IsNullOrWhiteSpace(transaction.GooglePlayOrderId))
            {
                Debug.LogWarning("[Firebase IAP] Skipped a legacy transaction log without a Google Play order ID.");
                return true;
            }

            CloudDocumentReference playerDocument = FirebaseAuthHandler.Documents.Collection(PlayersCollection).Document(uid);
            CloudDocumentReference transactionDocument = FirebaseAuthHandler.Documents
                .Collection(TransactionsCollection)
                .Document(transaction.DocumentId);

            ResolvePlayerProfileSnapshot(
                transaction,
                out string playerName,
                out int playerAvatar,
                out int playerFrame,
                out string profileSnapshotSource);
            Dictionary<string, object> transactionData = new Dictionary<string, object>
            {
                ["uid"] = uid,
                ["playerNameAtPurchase"] = playerName,
                ["playerAvatarAtPurchase"] = playerAvatar,
                ["playerFrameAtPurchase"] = playerFrame,
                ["profileSnapshotSource"] = profileSnapshotSource,
                ["purchaseStatus"] = "succeeded",
                ["productId"] = transaction.ProductId,
                ["productType"] = transaction.ProductType,
                ["platform"] = transaction.Platform,
                ["googlePlayOrderId"] = transaction.GooglePlayOrderId,
                ["transactionIdHash"] = transaction.TransactionIdHash,
                ["recordedAt"] = CloudFieldValue.ServerTimestamp,
                ["appVersion"] = transaction.AppVersion,
                ["schemaVersion"] = TransactionSchemaVersion
            };

            try
            {
                // SetAsync creates a new document without requiring a read of a
                // path that the current Firestore Rules intentionally hide.
                await transactionDocument.SetAsync(transactionData);
            }
            catch (Exception writeException)
            {
                Debug.LogWarning("[Firebase IAP] Transaction create did not complete ("
                    + writeException.GetType().Name + "). Checking for an existing record.");

                CloudDocumentSnapshot existingTransaction;
                try
                {
                    existingTransaction = await transactionDocument.GetSnapshotAsync(CloudReadSource.Server);
                }
                catch (Exception duplicateCheckException)
                {
                    Debug.LogWarning("[Firebase IAP] Transaction duplicate-check failed ("
                        + duplicateCheckException.GetType().Name + ").");
                    return false;
                }

                if (!existingTransaction.Exists)
                {
                    Debug.LogWarning("[Firebase IAP] Transaction create was not confirmed and will retry.");
                    return false;
                }

                if (!DocumentBelongsToUid(existingTransaction, uid))
                {
                    Debug.LogWarning("[Firebase IAP] Transaction hash is already associated with a different Firebase user.");
                    return false;
                }
            }

            if (!await FirebaseNoAdsEntitlementSync.TryCreateNoAdsEntitlementAsync(uid, transaction))
                return false;

            // Keep the player's normal profile current without using Firestore as a
            // coin source. A failure here does not invalidate the local purchase.
            await FirebaseProfileHandler.UpdateCurrentProfileAsync();

            Dictionary<string, object> profileData = new Dictionary<string, object>
            {
                ["uid"] = uid,
                ["lastIapProductId"] = transaction.ProductId,
                ["lastIapRecordedAt"] = CloudFieldValue.ServerTimestamp,
                ["updatedAt"] = CloudFieldValue.ServerTimestamp
            };

            await playerDocument.SetAsync(profileData, CloudWriteMode.Merge);
            return true;
        }

        private static async void FlushPendingAttemptsAsync()
        {
            isFlushingAttempts = true;
            try
            {
                List<IAPPendingPurchaseAttemptLog> pendingAttempts = IAPManager.GetPendingIAPAttempts();
                if (pendingAttempts.Count == 0)
                    return;

                if (!await FirebaseAuthHandler.EnsureSignedInAsync())
                    return;

                string uid = FirebaseAuthHandler.CurrentUserId;
                if (string.IsNullOrWhiteSpace(uid) || FirebaseAuthHandler.Documents == null)
                    return;

                foreach (IAPPendingPurchaseAttemptLog attempt in pendingAttempts)
                {
                    if (attempt == null || string.IsNullOrWhiteSpace(attempt.DocumentId))
                        continue;

                    if (!string.IsNullOrWhiteSpace(attempt.ExpectedFirebaseUid)
                        && !string.Equals(attempt.ExpectedFirebaseUid, uid, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (!await TryWritePurchaseAttemptAsync(uid, attempt))
                        break;

                    IAPManager.MarkIAPAttemptLogged(attempt.DocumentId);
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[Firebase IAP] Purchase attempt upload deferred: " + exception.Message);
            }
            finally
            {
                isFlushingAttempts = false;
            }
        }

        private static async Task<bool> TryWritePurchaseAttemptAsync(string uid, IAPPendingPurchaseAttemptLog attempt)
        {
            CloudDocumentReference attemptDocument = FirebaseAuthHandler.Documents
                .Collection(AttemptsCollection)
                .Document(attempt.DocumentId);

            CloudDocumentSnapshot existingAttempt = await attemptDocument.GetSnapshotAsync(CloudReadSource.Server);
            if (!existingAttempt.Exists)
            {
                ResolvePlayerProfileSnapshot(
                    attempt,
                    out string playerName,
                    out int playerAvatar,
                    out int playerFrame,
                    out string profileSnapshotSource);
                Dictionary<string, object> attemptData = new Dictionary<string, object>
                {
                    ["uid"] = uid,
                    ["playerNameAtAttempt"] = playerName,
                    ["playerAvatarAtAttempt"] = playerAvatar,
                    ["playerFrameAtAttempt"] = playerFrame,
                    ["profileSnapshotSource"] = profileSnapshotSource,
                    ["productId"] = attempt.ProductId,
                    ["productType"] = attempt.ProductType,
                    ["platform"] = attempt.Platform,
                    ["attemptId"] = attempt.DocumentId,
                    ["status"] = attempt.Status,
                    ["failureReason"] = attempt.FailureReason,
                    ["attemptedAt"] = CloudFieldValue.ServerTimestamp,
                    ["appVersion"] = attempt.AppVersion,
                    ["schemaVersion"] = AttemptSchemaVersion
                };

                await attemptDocument.SetAsync(attemptData);
            }
            else if (!DocumentBelongsToUid(existingAttempt, uid))
            {
                Debug.LogWarning("[Firebase IAP] Purchase attempt ID is already associated with a different Firebase user.");
                return false;
            }

            return true;
        }

        private static bool DocumentBelongsToUid(CloudDocumentSnapshot transaction, string uid)
        {
            Dictionary<string, object> data = transaction.ToDictionary();
            return data.TryGetValue("uid", out object transactionUid)
                && string.Equals(transactionUid as string, uid, StringComparison.Ordinal);
        }

        private static void CapturePlayerProfileSnapshot(IAPPendingTransactionLog transaction)
        {
            if (transaction == null || !SaveController.IsSaveLoaded)
                return;

            PlayerProfileSave profile = FirebaseProfileHandler.GetLocalProfile();
            transaction.PlayerNameAtPurchase = GetPlayerName(profile);
            transaction.PlayerAvatarAtPurchase = Mathf.Max(0, profile.AvatarIndex);
            transaction.PlayerFrameAtPurchase = Mathf.Max(0, profile.FrameIndex);
            transaction.HasProfileSnapshot = true;
        }

        private static void CapturePlayerProfileSnapshot(IAPPendingPurchaseAttemptLog attempt)
        {
            if (attempt == null || !SaveController.IsSaveLoaded)
                return;

            PlayerProfileSave profile = FirebaseProfileHandler.GetLocalProfile();
            attempt.PlayerNameAtAttempt = GetPlayerName(profile);
            attempt.PlayerAvatarAtAttempt = Mathf.Max(0, profile.AvatarIndex);
            attempt.PlayerFrameAtAttempt = Mathf.Max(0, profile.FrameIndex);
            attempt.HasProfileSnapshot = true;
        }

        private static void ResolvePlayerProfileSnapshot(
            IAPPendingTransactionLog transaction,
            out string playerName,
            out int playerAvatar,
            out int playerFrame,
            out string profileSnapshotSource)
        {
            if (transaction != null && transaction.HasProfileSnapshot)
            {
                playerName = string.IsNullOrWhiteSpace(transaction.PlayerNameAtPurchase)
                    ? FirebaseProfileHandler.GetDefaultPlayerName()
                    : transaction.PlayerNameAtPurchase.Trim();
                playerAvatar = Mathf.Max(0, transaction.PlayerAvatarAtPurchase);
                playerFrame = Mathf.Max(0, transaction.PlayerFrameAtPurchase);
                profileSnapshotSource = NormalizeProfileSnapshotSource(transaction.ProfileSnapshotSource);
                return;
            }

            PlayerProfileSave profile = SaveController.IsSaveLoaded
                ? FirebaseProfileHandler.GetLocalProfile()
                : null;
            playerName = GetPlayerName(profile);
            playerAvatar = profile != null ? Mathf.Max(0, profile.AvatarIndex) : 5;
            playerFrame = profile != null ? Mathf.Max(0, profile.FrameIndex) : 5;
            profileSnapshotSource = "pending_recovery";
        }

        private static void ResolvePlayerProfileSnapshot(
            IAPPendingPurchaseAttemptLog attempt,
            out string playerName,
            out int playerAvatar,
            out int playerFrame,
            out string profileSnapshotSource)
        {
            if (attempt != null && attempt.HasProfileSnapshot)
            {
                playerName = string.IsNullOrWhiteSpace(attempt.PlayerNameAtAttempt)
                    ? FirebaseProfileHandler.GetDefaultPlayerName()
                    : attempt.PlayerNameAtAttempt.Trim();
                playerAvatar = Mathf.Max(0, attempt.PlayerAvatarAtAttempt);
                playerFrame = Mathf.Max(0, attempt.PlayerFrameAtAttempt);
                profileSnapshotSource = "attempt_callback";
                return;
            }

            PlayerProfileSave profile = SaveController.IsSaveLoaded
                ? FirebaseProfileHandler.GetLocalProfile()
                : null;
            playerName = GetPlayerName(profile);
            playerAvatar = profile != null ? Mathf.Max(0, profile.AvatarIndex) : 5;
            playerFrame = profile != null ? Mathf.Max(0, profile.FrameIndex) : 5;
            profileSnapshotSource = "pending_recovery";
        }

        private static string NormalizeProfileSnapshotSource(string source)
        {
            return source == "restore_callback" ? "restore_callback" : "purchase_callback";
        }

        private static string NormalizeAttemptStatus(string status)
        {
            if (status == "cancelled" || status == "store_disconnected")
                return status;

            return "failed";
        }

        private static string GetPlayerName(PlayerProfileSave profile)
        {
            return profile != null && !string.IsNullOrWhiteSpace(profile.PlayerName)
                ? profile.PlayerName.Trim()
                : FirebaseProfileHandler.GetDefaultPlayerName();
        }

        private static string GetCurrentFirebaseUid()
        {
            return FirebaseAuthHandler.CurrentUserId;
        }

        private static string Sha256Hex(string value)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(value));
                StringBuilder result = new StringBuilder(hash.Length * 2);
                for (int index = 0; index < hash.Length; index++)
                    result.Append(hash[index].ToString("x2"));
                return result.ToString();
            }
        }
    }
}
