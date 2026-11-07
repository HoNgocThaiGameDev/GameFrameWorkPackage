using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

using NebulaSoft.Cloud;

namespace NebulaSoft
{
    public enum PlayerNameClaimStatus
    {
        Succeeded,
        Invalid,
        Taken,
        Failed
    }

    public sealed class PlayerNameClaimResult
    {
        public PlayerNameClaimStatus Status;
        public string DisplayName;
        public string NormalizedName;
        public string NameKey;

        public bool Succeeded => Status == PlayerNameClaimStatus.Succeeded;
    }

    /// <summary>
    /// Keeps the globally-visible player name unique without involving purchase data.
    /// </summary>
    public static class FirebasePlayerNameRegistry
    {
        private const string NamesCollection = "PlayerNames";
        private const int MinNameLength = 3;
        private const int MaxNameLength = 16;

        public static string CreateDefaultName(string seed, int attempt = 0)
        {
            string compactSeed = string.IsNullOrWhiteSpace(seed)
                ? Guid.NewGuid().ToString("N")
                : seed.Replace("-", string.Empty);
            string suffix = attempt > 0 ? "_" + (attempt + 1) : string.Empty;
            int availableSeedLength = Math.Max(1, MaxNameLength - "Player".Length - suffix.Length);
            string name = "Player" + compactSeed.Substring(0, Math.Min(availableSeedLength, compactSeed.Length)) + suffix;
            return TryNormalize(name, out string displayName, out _) ? displayName : "Player" + Guid.NewGuid().ToString("N").Substring(0, 10);
        }

        public static bool TryNormalize(string value, out string displayName, out string normalizedName)
        {
            return TryNormalizeInternal(value, out displayName, out normalizedName, out _);
        }

        public static string GetValidationError(string value)
        {
            TryNormalizeInternal(value, out _, out _, out string validationError);
            return validationError;
        }

        private static bool TryNormalizeInternal(
            string value,
            out string displayName,
            out string normalizedName,
            out string validationError)
        {
            displayName = string.Empty;
            normalizedName = string.Empty;
            validationError = null;
            if (string.IsNullOrWhiteSpace(value))
            {
                validationError = "Enter a player name.";
                return false;
            }

            StringBuilder builder = new StringBuilder();
            bool previousWhitespace = false;
            foreach (char character in value.Trim().Normalize(NormalizationForm.FormC))
            {
                if (char.IsControl(character))
                {
                    validationError = "Name contains unsupported characters.";
                    return false;
                }

                if (char.IsWhiteSpace(character))
                {
                    if (!previousWhitespace)
                        builder.Append(' ');

                    previousWhitespace = true;
                    continue;
                }

                previousWhitespace = false;
                builder.Append(character);
            }

            displayName = builder.ToString().Trim();
            int textElementCount = new StringInfo(displayName).LengthInTextElements;
            if (textElementCount < MinNameLength)
            {
                displayName = string.Empty;
                validationError = "Name must contain at least 3 characters.";
                return false;
            }

            if (textElementCount > MaxNameLength)
            {
                displayName = string.Empty;
                validationError = "Name can contain at most 16 characters.";
                return false;
            }

            normalizedName = displayName.ToLowerInvariant().Normalize(NormalizationForm.FormC);
            return true;
        }

        public static string ComputeKey(string normalizedName)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(normalizedName ?? string.Empty));
                StringBuilder builder = new StringBuilder(bytes.Length * 2);
                for (int index = 0; index < bytes.Length; index++)
                    builder.Append(bytes[index].ToString("x2"));

                return builder.ToString();
            }
        }

        public static async Task<PlayerNameClaimResult> ClaimAndWritePlayerAsync(
            ICloudDocumentStore firestore,
            CloudDocumentReference playerDocument,
            string uid,
            string requestedName,
            System.Collections.Generic.Dictionary<string, object> playerData)
        {
            if (!TryNormalize(requestedName, out string displayName, out string normalizedName)
                || firestore == null
                || playerDocument == null
                || string.IsNullOrWhiteSpace(uid)
                || playerData == null)
            {
                return new PlayerNameClaimResult { Status = PlayerNameClaimStatus.Invalid };
            }

            string nameKey = ComputeKey(normalizedName);
            CloudDocumentReference nameDocument = firestore.Collection(NamesCollection).Document(nameKey);
            PlayerNameClaimStatus status = PlayerNameClaimStatus.Failed;

            try
            {
                await firestore.RunTransactionAsync(async transaction =>
                {
                    CloudDocumentSnapshot playerSnapshot = await transaction.GetSnapshotAsync(playerDocument);
                    CloudDocumentSnapshot requestedNameSnapshot = await transaction.GetSnapshotAsync(nameDocument);
                    SaveCloudData.ValidateWrite(playerSnapshot.Exists ? playerSnapshot.ToDictionary() : null, playerData);

                    string existingOwner = GetString(requestedNameSnapshot, "uid");
                    if (requestedNameSnapshot.Exists && !string.Equals(existingOwner, uid, StringComparison.Ordinal))
                    {
                        status = PlayerNameClaimStatus.Taken;
                        return;
                    }

                    string previousNameKey = GetString(playerSnapshot, "playerNameKey");
                    CloudDocumentReference previousNameDocument = null;
                    CloudDocumentSnapshot previousNameSnapshot = null;
                    if (!string.IsNullOrWhiteSpace(previousNameKey) && !string.Equals(previousNameKey, nameKey, StringComparison.Ordinal))
                    {
                        previousNameDocument = firestore.Collection(NamesCollection).Document(previousNameKey);
                        previousNameSnapshot = await transaction.GetSnapshotAsync(previousNameDocument);
                    }

                    if (!requestedNameSnapshot.Exists)
                    {
                        transaction.Set(nameDocument, new System.Collections.Generic.Dictionary<string, object>
                        {
                            ["uid"] = uid,
                            ["displayName"] = displayName,
                            ["normalizedName"] = normalizedName,
                            ["claimedAt"] = CloudFieldValue.ServerTimestamp,
                            ["updatedAt"] = CloudFieldValue.ServerTimestamp,
                            ["schemaVersion"] = 1
                        });
                    }

                    System.Collections.Generic.Dictionary<string, object> writeData =
                        new System.Collections.Generic.Dictionary<string, object>(playerData)
                        {
                            ["uid"] = uid,
                            ["PlayerName"] = displayName,
                            ["PlayerNameLower"] = normalizedName,
                            ["playerNameKey"] = nameKey
                        };
                    if (!playerSnapshot.Exists)
                        writeData["createdAt"] = CloudFieldValue.ServerTimestamp;

                    transaction.Set(playerDocument, writeData, CloudWriteMode.Merge);

                    if (previousNameSnapshot != null
                        && previousNameSnapshot.Exists
                        && string.Equals(GetString(previousNameSnapshot, "uid"), uid, StringComparison.Ordinal))
                    {
                        transaction.Delete(previousNameDocument);
                    }

                    status = PlayerNameClaimStatus.Succeeded;
                });
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogWarning("[Firebase] Player name claim failed: " + exception.Message);
                status = PlayerNameClaimStatus.Failed;
            }

            return new PlayerNameClaimResult
            {
                Status = status,
                DisplayName = displayName,
                NormalizedName = normalizedName,
                NameKey = nameKey
            };
        }

        private static string GetString(CloudDocumentSnapshot snapshot, string field)
        {
            string value;
            return snapshot != null && snapshot.TryGetValue(field, out value) ? value : null;
        }
    }
}
