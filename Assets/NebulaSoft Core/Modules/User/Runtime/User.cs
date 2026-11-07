using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace NebulaSoft
{
    public static class User
    {
        private const string LocalIdKey = "user_local_id_v1";

        private static string localId;
        private static string customId;

        /// <summary>Stable local ID (generated once and stored in PlayerPrefs).</summary>
        public static string LocalId => localId ??= LoadOrCreateLocalId();

        /// <summary>Custom ID (e.g., Firebase UID). Not persisted unless you save it manually.</summary>
        public static string CustomId => customId;

        /// <summary>Whether a custom ID is set.</summary>
        public static bool HasCustomId => !string.IsNullOrWhiteSpace(customId);

        /// <summary>Main ID for the system: uses CustomId if present, otherwise LocalId.</summary>
        public static string GetId() => HasCustomId ? customId : LocalId;

        public static void SetCustomId(string customId)
        {
            User.customId = Sanitize(customId);
        }

        /// <summary>SHA-256 hash in hex (64 characters) of GetId(). Useful for Google ObfuscatedAccountId.</summary>
        public static string GetIdSha256Hex() => Sha256Hex(GetId());

        /// <summary>
        /// Guid created from the first 16 bytes of SHA-256 hash of GetId(). Useful for Apple AppAccountToken.
        /// </summary>
        public static Guid GetIdAsGuidFromSha256()
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(GetId() ?? ""));
                byte[] guidBytes = new byte[16];

                Buffer.BlockCopy(hash, 0, guidBytes, 0, 16);

                return new Guid(guidBytes);
            }
        }

        private static string LoadOrCreateLocalId()
        {
            string id = PlayerPrefs.GetString(LocalIdKey, null);
            if (string.IsNullOrEmpty(id))
            {
                id = Guid.NewGuid().ToString("N");

                PlayerPrefs.SetString(LocalIdKey, id);
                PlayerPrefs.Save();
            }

            return id;
        }

        private static string Sanitize(string s)
        {
            s = s?.Trim();

            return string.IsNullOrEmpty(s) ? null : s;
        }

        private static string Sha256Hex(string s)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(s ?? "");
                byte[] hash = sha.ComputeHash(bytes);

                StringBuilder sb = new StringBuilder(hash.Length * 2);
                foreach (byte b in hash) sb.Append(b.ToString("x2"));

                return sb.ToString();
            }
        }
    }
}
