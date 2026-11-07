using System;
using System.Collections.Generic;
using UnityEngine;

namespace NebulaSoft
{
    [Serializable]
    public sealed class CoinSafeAccountEntry
    {
        public string OwnerKey;
        public int Amount;
        public bool Dirty;
        public long Revision;
    }

    [Serializable]
    public sealed class CoinSafeSave : ISaveObject
    {
        // Kept only to migrate the original device-wide save.
        public int Amount = -1;
        public int SchemaVersion;
        public string ActiveOwnerKey;
        public string PendingFacebookUid;
        public string PendingSourceOwnerKey;
        public List<CoinSafeAccountEntry> Accounts = new List<CoinSafeAccountEntry>();

        public void Flush()
        {
        }
    }

    public readonly struct CoinSafeSyncSnapshot
    {
        public readonly string Uid;
        public readonly int Amount;
        public readonly long Revision;

        public CoinSafeSyncSnapshot(string uid, int amount, long revision)
        {
            Uid = uid;
            Amount = amount;
            Revision = revision;
        }
    }

    public static class CoinSafeProgress
    {
        public const int InitialAmount = 0;
        public const int ClaimMilestone = 500;

        private const int CurrentSchemaVersion = 2;
        private const string SaveKey = "Coin Safe Progress";
        private const string GuestOwnerPrefix = "guest:";
        private const string FacebookOwnerPrefix = "facebook:";

        private static bool facebookAuthTransitionActive;

        public static event Action AmountChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            AmountChanged = null;
            facebookAuthTransitionActive = false;
        }

        public static int Amount
        {
            get
            {
                CoinSafeSave save = GetSave();
                CoinSafeAccountEntry entry = GetActiveEntry(save);
                return entry != null ? entry.Amount : InitialAmount;
            }
        }

        public static string ActiveOwnerKey
        {
            get
            {
                CoinSafeSave save = GetSave();
                return save != null ? save.ActiveOwnerKey : GetGuestOwnerKey();
            }
        }

        public static string PendingFacebookUid
        {
            get
            {
                CoinSafeSave save = GetSave();
                return save != null ? NormalizeUid(save.PendingFacebookUid) : null;
            }
        }

        public static bool HasPendingFacebookResolution => !string.IsNullOrEmpty(PendingFacebookUid);
        public static bool IsFacebookCloudWriteBlocked => facebookAuthTransitionActive || HasPendingFacebookResolution;
        public static bool HasClaimableReward => Amount >= ClaimMilestone;

        public static void BeginFacebookAuthTransition()
        {
            facebookAuthTransitionActive = true;
        }

        public static void EndFacebookAuthTransition()
        {
            facebookAuthTransitionActive = false;
        }

        public static void AddRewardCoins(int amount)
        {
            if (amount <= 0)
                return;

            CoinSafeSave save = GetSave();
            CoinSafeAccountEntry entry = GetActiveEntry(save);
            if (entry == null)
                return;

            long updatedAmount = (long)entry.Amount + amount;
            SetLocalAmount(entry, updatedAmount > int.MaxValue ? int.MaxValue : (int)updatedAmount);
            SaveController.MarkAsSaveIsRequired();
            AmountChanged?.Invoke();
            CoinSafeCloudSync.RequestCheckpointSync();
        }

        public static void ResetAccumulatedCoins()
        {
            SetAccumulatedCoins(0);
        }

        public static void SetAccumulatedCoins(int amount)
        {
            CoinSafeSave save = GetSave();
            CoinSafeAccountEntry entry = GetActiveEntry(save);
            if (entry == null)
                return;

            amount = Math.Max(0, amount);
            if (entry.Amount == amount)
                return;

            SetLocalAmount(entry, amount);
            SaveController.MarkAsSaveIsRequired();
            AmountChanged?.Invoke();
            CoinSafeCloudSync.RequestCheckpointSync();
        }

        public static void BeginFacebookResolution(string uid)
        {
            CoinSafeSave save = GetSave();
            uid = NormalizeUid(uid);
            if (save == null || string.IsNullOrEmpty(uid))
                return;

            save.PendingFacebookUid = uid;
            save.PendingSourceOwnerKey = save.ActiveOwnerKey;
            SaveController.MarkAsSaveIsRequired();
            FlushLocalSave();
        }

        public static void ResolveFacebookWithLocal(string uid, bool requestCloudSync = true)
        {
            CoinSafeSave save = GetSave();
            uid = NormalizeUid(uid);
            if (save == null || string.IsNullOrEmpty(uid))
                return;

            CoinSafeAccountEntry source = FindEntry(save, ResolvePendingSourceOwnerKey(save));
            int sourceAmount = source != null ? source.Amount : InitialAmount;
            CoinSafeAccountEntry target = GetOrCreateEntry(save, GetFacebookOwnerKey(uid), InitialAmount);

            SetLocalAmount(target, sourceAmount, forceRevision: true);
            save.ActiveOwnerKey = target.OwnerKey;
            ClearPendingResolution(save);
            SaveController.MarkAsSaveIsRequired();
            FlushLocalSave();
            AmountChanged?.Invoke();
            if (requestCloudSync)
                CoinSafeCloudSync.RequestCheckpointSync();
        }

        public static void ResolveFacebookWithCloud(
            string uid,
            int amount,
            bool requiresUpload = false,
            bool requestCloudSync = true)
        {
            CoinSafeSave save = GetSave();
            uid = NormalizeUid(uid);
            if (save == null || string.IsNullOrEmpty(uid))
                return;

            CoinSafeAccountEntry target = GetOrCreateEntry(save, GetFacebookOwnerKey(uid), InitialAmount);
            int normalizedAmount = Math.Max(0, amount);
            bool stateChanged = target.Amount != normalizedAmount || target.Dirty != requiresUpload;

            target.Amount = normalizedAmount;
            target.Dirty = requiresUpload;
            if (stateChanged)
                target.Revision++;

            save.ActiveOwnerKey = target.OwnerKey;
            ClearPendingResolution(save);
            SaveController.MarkAsSaveIsRequired();
            FlushLocalSave();
            AmountChanged?.Invoke();

            if (requiresUpload && requestCloudSync)
                CoinSafeCloudSync.RequestCheckpointSync();
        }

        public static bool IsActiveFacebookOwner(string uid)
        {
            CoinSafeSave save = GetSave();
            return save != null
                && string.Equals(GetFacebookUid(save.ActiveOwnerKey), NormalizeUid(uid), StringComparison.Ordinal);
        }

        public static bool ActivateCachedFacebookOwner(string uid, bool requestCloudSync = true)
        {
            CoinSafeSave save = GetSave();
            uid = NormalizeUid(uid);
            if (save == null || string.IsNullOrEmpty(uid))
                return false;

            if (!string.IsNullOrEmpty(save.PendingFacebookUid))
                return string.Equals(save.PendingFacebookUid, uid, StringComparison.Ordinal);

            string ownerKey = GetFacebookOwnerKey(uid);
            bool ownerChanged = !string.Equals(save.ActiveOwnerKey, ownerKey, StringComparison.Ordinal);
            GetOrCreateEntry(save, ownerKey, InitialAmount);
            save.ActiveOwnerKey = ownerKey;
            SaveController.MarkAsSaveIsRequired();

            if (ownerChanged)
                AmountChanged?.Invoke();

            if (requestCloudSync)
                CoinSafeCloudSync.RequestCheckpointSync();

            return true;
        }

        public static bool TryGetActiveFacebookSnapshot(out CoinSafeSyncSnapshot snapshot)
        {
            snapshot = default;
            CoinSafeSave save = GetSave();
            if (save == null || !string.IsNullOrEmpty(save.PendingFacebookUid))
                return false;

            string uid = GetFacebookUid(save.ActiveOwnerKey);
            CoinSafeAccountEntry entry = GetActiveEntry(save);
            if (string.IsNullOrEmpty(uid) || entry == null)
                return false;

            snapshot = new CoinSafeSyncSnapshot(uid, entry.Amount, entry.Revision);
            return true;
        }

        public static bool TryGetDirtyFacebookSnapshot(string uid, out CoinSafeSyncSnapshot snapshot)
        {
            snapshot = default;
            CoinSafeSave save = GetSave();
            uid = NormalizeUid(uid);
            if (save == null || string.IsNullOrEmpty(uid))
                return false;

            CoinSafeAccountEntry entry = FindEntry(save, GetFacebookOwnerKey(uid));
            if (entry == null || !entry.Dirty)
                return false;

            snapshot = new CoinSafeSyncSnapshot(uid, entry.Amount, entry.Revision);
            return true;
        }

        public static bool IsFacebookSnapshotDirty(string uid, long revision)
        {
            CoinSafeSave save = GetSave();
            CoinSafeAccountEntry entry = FindEntry(save, GetFacebookOwnerKey(NormalizeUid(uid)));
            return entry != null && entry.Dirty && entry.Revision == revision;
        }

        public static void MarkFacebookSnapshotSynced(string uid, long revision)
        {
            CoinSafeSave save = GetSave();
            CoinSafeAccountEntry entry = FindEntry(save, GetFacebookOwnerKey(NormalizeUid(uid)));
            if (entry == null || entry.Revision != revision)
                return;

            entry.Dirty = false;
            SaveController.MarkAsSaveIsRequired();
        }

        public static void FlushLocalSave()
        {
            if (SaveController.IsSaveLoaded)
                SaveController.Save(forceSave: true, useThreads: false);
        }

        private static CoinSafeSave GetSave()
        {
            if (!SaveController.IsSaveLoaded)
                return null;

            CoinSafeSave save = SaveController.GetSaveObject<CoinSafeSave>(SaveKey);
            EnsureMigrated(save);
            return save;
        }

        private static void EnsureMigrated(CoinSafeSave save)
        {
            if (save == null)
                return;

            bool changed = false;
            if (save.Accounts == null)
            {
                save.Accounts = new List<CoinSafeAccountEntry>();
                changed = true;
            }

            string guestOwnerKey = GetGuestOwnerKey();
            if (save.SchemaVersion < CurrentSchemaVersion)
            {
                int legacyAmount = save.Amount >= 0 ? save.Amount : InitialAmount;
                if (FindEntry(save, guestOwnerKey) == null)
                    save.Accounts.Add(CreateEntry(guestOwnerKey, legacyAmount));

                save.Amount = -1;
                save.SchemaVersion = CurrentSchemaVersion;
                save.ActiveOwnerKey = guestOwnerKey;
                changed = true;
            }

            if (FindEntry(save, guestOwnerKey) == null)
            {
                save.Accounts.Add(CreateEntry(guestOwnerKey, InitialAmount));
                changed = true;
            }

            for (int i = save.Accounts.Count - 1; i >= 0; i--)
            {
                CoinSafeAccountEntry entry = save.Accounts[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.OwnerKey))
                {
                    save.Accounts.RemoveAt(i);
                    changed = true;
                    continue;
                }

                if (entry.Amount < 0)
                {
                    entry.Amount = InitialAmount;
                    changed = true;
                }

                if (entry.Revision < 0)
                {
                    entry.Revision = 0;
                    changed = true;
                }
            }

            if (string.IsNullOrEmpty(save.ActiveOwnerKey) || FindEntry(save, save.ActiveOwnerKey) == null)
            {
                save.ActiveOwnerKey = guestOwnerKey;
                changed = true;
            }

            if (changed)
                SaveController.MarkAsSaveIsRequired();
        }

        private static CoinSafeAccountEntry GetActiveEntry(CoinSafeSave save)
        {
            if (save == null)
                return null;

            CoinSafeAccountEntry entry = FindEntry(save, save.ActiveOwnerKey);
            return entry ?? GetOrCreateEntry(save, GetGuestOwnerKey(), InitialAmount);
        }

        private static CoinSafeAccountEntry GetOrCreateEntry(CoinSafeSave save, string ownerKey, int initialAmount)
        {
            CoinSafeAccountEntry entry = FindEntry(save, ownerKey);
            if (entry != null)
                return entry;

            entry = CreateEntry(ownerKey, initialAmount);
            save.Accounts.Add(entry);
            SaveController.MarkAsSaveIsRequired();
            return entry;
        }

        private static CoinSafeAccountEntry CreateEntry(string ownerKey, int amount)
        {
            return new CoinSafeAccountEntry
            {
                OwnerKey = ownerKey,
                Amount = Math.Max(0, amount),
                Dirty = false,
                Revision = 0
            };
        }

        private static CoinSafeAccountEntry FindEntry(CoinSafeSave save, string ownerKey)
        {
            if (save == null || save.Accounts == null || string.IsNullOrEmpty(ownerKey))
                return null;

            for (int i = 0; i < save.Accounts.Count; i++)
            {
                CoinSafeAccountEntry entry = save.Accounts[i];
                if (entry != null && string.Equals(entry.OwnerKey, ownerKey, StringComparison.Ordinal))
                    return entry;
            }

            return null;
        }

        private static void SetLocalAmount(CoinSafeAccountEntry entry, int amount, bool forceRevision = false)
        {
            amount = Math.Max(0, amount);
            if (!forceRevision && entry.Amount == amount && entry.Dirty)
                return;

            entry.Amount = amount;
            entry.Dirty = true;
            entry.Revision++;
        }

        private static string ResolvePendingSourceOwnerKey(CoinSafeSave save)
        {
            return !string.IsNullOrEmpty(save.PendingSourceOwnerKey)
                ? save.PendingSourceOwnerKey
                : save.ActiveOwnerKey;
        }

        private static void ClearPendingResolution(CoinSafeSave save)
        {
            save.PendingFacebookUid = null;
            save.PendingSourceOwnerKey = null;
        }

        private static string GetGuestOwnerKey()
        {
            return GuestOwnerPrefix + User.LocalId;
        }

        private static string GetFacebookOwnerKey(string uid)
        {
            uid = NormalizeUid(uid);
            return string.IsNullOrEmpty(uid) ? null : FacebookOwnerPrefix + uid;
        }

        private static string GetFacebookUid(string ownerKey)
        {
            return !string.IsNullOrEmpty(ownerKey)
                && ownerKey.StartsWith(FacebookOwnerPrefix, StringComparison.Ordinal)
                ? NormalizeUid(ownerKey.Substring(FacebookOwnerPrefix.Length))
                : null;
        }

        private static string NormalizeUid(string uid)
        {
            uid = uid?.Trim();
            return string.IsNullOrEmpty(uid) ? null : uid;
        }
    }
}
