using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

using NebulaSoft.Cloud;

namespace NebulaSoft
{
    public sealed class PlayerProgressSnapshot
    {
        public string Uid;
        public string PlayerName;
        public int AvatarIndex;
        public int FrameIndex;
        public int MaxReachedLevelIndex;
        public int DisplayLevelIndex;
        public int RealLevelIndex;
        public int CoinBalance;
        public int CoinSafeAmount;
        public Dictionary<string, int> PowerUpAmounts = new Dictionary<string, int>();
        public int SaveVersion;
        public Dictionary<string, object> FrameworkSave;
        public bool Exists;
        public bool CloudStateKnown;
        public bool HasProfile;
        public bool HasLevelProgress;
        public bool HasCoinBalance;
        public bool HasCoinSafeAmount;
        public bool HasPowerUpAmounts;
        public bool HasUpdatedAt;
        public DateTime UpdatedAtUtc;

        public int DisplayLevelNumber => Mathf.Max(1, MaxReachedLevelIndex + 1);
    }

    public sealed class FirebasePlayerProgress
    {
        public string Uid;
        public string PlayerName;
        public int AvatarIndex;
        public int FrameIndex;
        public int MaxReachedLevelIndex;
        public int DisplayLevelIndex;
        public int RealLevelIndex;
        public int CoinBalance;
        public int CoinSafeAmount;
        public Dictionary<string, int> PowerUpAmounts = new Dictionary<string, int>();
        public int SaveVersion;
        public Dictionary<string, object> FrameworkSave;
        public bool Exists;
        public bool CloudStateKnown;
        public bool HasProfile;
        public bool HasLevelProgress;
        public bool HasCoinBalance;
        public bool HasCoinSafeAmount;
        public bool HasPowerUpAmounts;
        public bool HasUpdatedAt;
        public DateTime UpdatedAtUtc;

        public PlayerProgressSnapshot ToSnapshot()
        {
            return new PlayerProgressSnapshot
            {
                Uid = Uid,
                PlayerName = PlayerName,
                AvatarIndex = AvatarIndex,
                FrameIndex = FrameIndex,
                MaxReachedLevelIndex = MaxReachedLevelIndex,
                DisplayLevelIndex = DisplayLevelIndex,
                RealLevelIndex = RealLevelIndex,
                CoinBalance = CoinBalance,
                CoinSafeAmount = CoinSafeAmount,
                PowerUpAmounts = PowerUpAmounts != null
                    ? new Dictionary<string, int>(PowerUpAmounts)
                    : new Dictionary<string, int>(),
                SaveVersion = SaveVersion,
                FrameworkSave = SaveCloudData.Clone(FrameworkSave),
                Exists = Exists,
                CloudStateKnown = CloudStateKnown,
                HasProfile = HasProfile,
                HasLevelProgress = HasLevelProgress,
                HasCoinBalance = HasCoinBalance,
                HasCoinSafeAmount = HasCoinSafeAmount,
                HasPowerUpAmounts = HasPowerUpAmounts,
                HasUpdatedAt = HasUpdatedAt,
                UpdatedAtUtc = UpdatedAtUtc
            };
        }
    }

    public enum FirebaseProgressDownloadStatus
    {
        Found,
        NotFound,
        Failed
    }

    public enum FirebaseSyncFailureKind
    {
        None,
        Offline,
        Timeout,
        Authentication,
        PermissionDenied,
        SessionChanged,
        InvalidState,
        Unknown
    }

    public sealed class FirebaseProgressOperationResult
    {
        public bool Succeeded;
        public FirebaseSyncFailureKind FailureKind;
        public string Error;
    }

    public sealed class FirebaseProgressDownloadResult
    {
        public FirebaseProgressDownloadStatus Status;
        public FirebasePlayerProgress Progress;
        public FirebaseSyncFailureKind FailureKind;
        public string Error;
    }

    public static class FirebaseProfileHandler
    {
        private const string PlayersCollection = "Players";
        private const int ProgressSaveVersion = 4;

        public static PlayerNameClaimStatus LastProfileNameClaimStatus { get; private set; } = PlayerNameClaimStatus.Failed;
        private const int FirebaseServerTimeoutMs = 15000;

        public static string GetDefaultPlayerName()
        {
            return FirebasePlayerNameRegistry.CreateDefaultName(User.LocalId);
        }

        public static PlayerProfileSave GetLocalProfile()
        {
            PlayerProfileSave save = SaveController.GetSaveObject<PlayerProfileSave>("Player Profile Save");
            if (string.IsNullOrWhiteSpace(save.PlayerName))
                save.PlayerName = GetDefaultPlayerName();

            return save;
        }

        public static int GetCurrentScore()
        {
            if (!SaveController.IsSaveLoaded)
                return 0;

            return Mathf.Max(0, ActiveSession.Current.MaxReachedLevelIndex);
        }

        public static PlayerProgressSnapshot GetLocalProgressSnapshot()
        {
            PlayerProgressSnapshot snapshot = new PlayerProgressSnapshot
            {
                Exists = SaveController.IsSaveLoaded,
                HasProfile = SaveController.IsSaveLoaded,
                HasLevelProgress = SaveController.IsSaveLoaded,
                HasCoinBalance = SaveController.IsSaveLoaded,
                HasCoinSafeAmount = SaveController.IsSaveLoaded,
                HasPowerUpAmounts = SaveController.IsSaveLoaded,
                HasUpdatedAt = false,
                SaveVersion = ProgressSaveVersion,
                FrameworkSave = SaveController.IsSaveLoaded ? SaveController.ExportCloudData() : null,
                CoinBalance = GetLocalCoinBalance(),
                CoinSafeAmount = CoinSafeProgress.Amount,
                PowerUpAmounts = GetLocalPowerUpAmounts()
            };

            if (!SaveController.IsSaveLoaded)
                return snapshot;

            PlayerProfileSave profile = GetLocalProfile();
            LevelSave levelSave = SaveController.GetSaveObject<LevelSave>();
            snapshot.Uid = profile.FirebaseUid;
            snapshot.PlayerName = string.IsNullOrWhiteSpace(profile.PlayerName)
                ? GetDefaultPlayerName()
                : profile.PlayerName.Trim();
            snapshot.AvatarIndex = profile.AvatarIndex;
            snapshot.FrameIndex = profile.FrameIndex;
            snapshot.MaxReachedLevelIndex = Mathf.Max(0, levelSave.MaxReachedLevelIndex);
            snapshot.DisplayLevelIndex = Mathf.Max(0, levelSave.DisplayLevelIndex);
            snapshot.RealLevelIndex = levelSave.RealLevelIndex;
            return snapshot;
        }

        public static async Task<bool> UpdateCurrentProfileAsync()
        {
            if (!SaveController.IsSaveLoaded)
                return false;

            PlayerProfileSave save = GetLocalProfile();
            return await UpdateCurrentProfileAsync(save.PlayerName, save.AvatarIndex, save.FrameIndex);
        }

        public static async Task<bool> UpdateCurrentProfileAsync(string playerName, int avatarIndex, int frameIndex)
        {
            LastProfileNameClaimStatus = PlayerNameClaimStatus.Failed;
            if (CoinSafeProgress.IsFacebookCloudWriteBlocked)
            {
                Debug.Log("[Firebase] Profile upload deferred until the Local/Cloud version is selected.");
                return false;
            }

            try
            {
                if (!await FirebaseAuthHandler.EnsureSignedInAsync())
                    return false;

                string uid = FirebaseAuthHandler.CurrentUserId;
                if (string.IsNullOrEmpty(uid))
                    return false;

                playerName = string.IsNullOrWhiteSpace(playerName) ? GetDefaultPlayerName() : playerName.Trim();
                int maxLevel = GetCurrentScore();

                Dictionary<string, object> data = new Dictionary<string, object>
                {
                    ["uid"] = uid,
                    ["PlayerName"] = playerName,
                    ["PlayerNameLower"] = playerName.ToLowerInvariant(),
                    ["PlayerAvatar"] = avatarIndex,
                    ["PlayerFrame"] = frameIndex,
                    ["score"] = maxLevel,
                    ["maxLevel"] = maxLevel,
                    ["maxReachedLevelIndex"] = maxLevel,
                    ["displayLevelIndex"] = ActiveSession.Current.DisplayLevelIndex,
                    ["realLevelIndex"] = ActiveSession.Current.LevelIndex,
                    ["saveVersion"] = ProgressSaveVersion,
                    ["updatedAt"] = CloudFieldValue.ServerTimestamp
                };

                if (CoinSafeProgress.IsFacebookCloudWriteBlocked
                    || !string.Equals(FirebaseAuthHandler.CurrentUserId, uid, StringComparison.Ordinal))
                {
                    Debug.Log("[Firebase] Profile upload cancelled because Facebook sync started or the authenticated user changed.");
                    return false;
                }

                PlayerNameClaimResult writeResult = await WritePlayerDocumentAsync(uid, playerName, data);
                LastProfileNameClaimStatus = writeResult != null
                    ? writeResult.Status
                    : PlayerNameClaimStatus.Failed;
                if (writeResult == null || !writeResult.Succeeded)
                {
                    Debug.LogWarning("[Firebase] Profile update rejected. Name status=" + LastProfileNameClaimStatus + ".");
                    return false;
                }

                playerName = writeResult.DisplayName;

                PlayerProfileSave save = GetLocalProfile();
                save.FirebaseUid = uid;
                save.PlayerName = playerName;
                save.AvatarIndex = avatarIndex;
                save.FrameIndex = frameIndex;
                SaveController.MarkAsSaveIsRequired();

                FirebaseAuthHandler.MirrorCurrentUserToLocalSave();
                CoinSafeCloudSync.RequestCheckpointSync();
                return true;
            }
            catch (Exception exception)
            {
                LastProfileNameClaimStatus = PlayerNameClaimStatus.Failed;
                Debug.LogWarning("[Firebase] Profile update failed: " + exception.Message);
                return false;
            }
        }

        public static async Task<bool> SubmitCurrentProgressAsync()
        {
            try
            {
                if (!SaveController.IsSaveLoaded)
                    return false;

                return await UploadProgressAsync();
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[Firebase] Progress submit failed: " + exception.Message);
                return false;
            }
        }

        public static async Task<bool> UploadProgressAsync(string expectedUid = null)
        {
            FirebaseProgressOperationResult result = await UploadLocalProgressAsync(
                expectedUid,
                allowPendingFacebookResolution: false);
            return result.Succeeded;
        }

        public static async Task<FirebaseProgressOperationResult> UploadFullLocalProgressAsync(string expectedUid)
        {
            return await UploadLocalProgressAsync(expectedUid, allowPendingFacebookResolution: true);
        }

        private static async Task<FirebaseProgressOperationResult> UploadLocalProgressAsync(
            string expectedUid,
            bool allowPendingFacebookResolution)
        {
            if (!allowPendingFacebookResolution && CoinSafeProgress.IsFacebookCloudWriteBlocked)
            {
                Debug.Log("[Firebase] Progress upload deferred until the Local/Cloud version is selected.");
                return CreateFailure(FirebaseSyncFailureKind.InvalidState, "A Local/Cloud selection is required.");
            }

            try
            {
                string normalizedExpectedUid = string.IsNullOrWhiteSpace(expectedUid)
                    ? null
                    : expectedUid.Trim();

                if (!SaveController.IsSaveLoaded)
                    return CreateFailure(FirebaseSyncFailureKind.InvalidState, "The local save is unavailable.");

                if (!await FirebaseAuthHandler.EnsureSignedInAsync())
                    return CreateFailure(FirebaseSyncFailureKind.Authentication, "Firebase authentication is unavailable.");

                string uid = FirebaseAuthHandler.CurrentUserId;
                if (string.IsNullOrEmpty(uid))
                    return CreateFailure(FirebaseSyncFailureKind.Authentication, "Firebase user ID is unavailable.");

                if (normalizedExpectedUid != null
                    && !FirebaseAuthHandler.IsCurrentFacebookUser(normalizedExpectedUid))
                {
                    Debug.LogWarning("[Firebase] Progress upload skipped because the authenticated Facebook user changed.");
                    return CreateFailure(FirebaseSyncFailureKind.SessionChanged, "The authenticated Facebook account changed.");
                }

                PlayerProfileSave profile = GetLocalProfile();
                LevelSave levelSave = SaveController.GetSaveObject<LevelSave>();
                string playerName = string.IsNullOrWhiteSpace(profile.PlayerName)
                    ? GetDefaultPlayerName()
                    : profile.PlayerName.Trim();

                Dictionary<string, object> data = new Dictionary<string, object>
                {
                    ["uid"] = uid,
                    ["PlayerName"] = playerName,
                    ["PlayerNameLower"] = playerName.ToLowerInvariant(),
                    ["PlayerAvatar"] = profile.AvatarIndex,
                    ["PlayerFrame"] = profile.FrameIndex,
                    ["score"] = Mathf.Max(0, levelSave.MaxReachedLevelIndex),
                    ["maxLevel"] = Mathf.Max(0, levelSave.MaxReachedLevelIndex),
                    ["maxReachedLevelIndex"] = Mathf.Max(0, levelSave.MaxReachedLevelIndex),
                    ["displayLevelIndex"] = Mathf.Max(0, levelSave.DisplayLevelIndex),
                    ["realLevelIndex"] = levelSave.RealLevelIndex,
                    ["saveVersion"] = ProgressSaveVersion,
                    ["updatedAt"] = CloudFieldValue.ServerTimestamp
                };

                SaveCloudData.AddToUpdate(data, SaveController.ExportCloudData());

                if (allowPendingFacebookResolution)
                {
                    data["coinBalance"] = GetLocalCoinBalance();
                    data["coinSafeAmount"] = CoinSafeProgress.Amount;
                    data["coinSafeUpdatedAt"] = CloudFieldValue.ServerTimestamp;
                    data["powerUps"] = GetLocalPowerUpAmounts();
                }

                PlayerNameClaimResult writeResult = await WritePlayerDocumentAsync(uid, playerName, data);
                if (!writeResult.Succeeded)
                {
                    FirebaseSyncFailureKind failureKind = writeResult.Status == PlayerNameClaimStatus.Taken
                        ? FirebaseSyncFailureKind.InvalidState
                        : FirebaseSyncFailureKind.Unknown;
                    return CreateFailure(failureKind, writeResult.Status == PlayerNameClaimStatus.Taken
                        ? "This player name is already in use."
                        : "The player profile could not be saved.");
                }

                playerName = writeResult.DisplayName;

                if (normalizedExpectedUid != null
                    && !FirebaseAuthHandler.IsCurrentFacebookUser(normalizedExpectedUid))
                {
                    Debug.LogWarning("[Firebase] Progress upload completed for the original user, but the active Facebook user changed before local confirmation.");
                    return CreateFailure(FirebaseSyncFailureKind.SessionChanged, "The authenticated Facebook account changed before confirmation.");
                }

                profile.FirebaseUid = uid;
                profile.PlayerName = playerName;
                SaveController.MarkAsSaveIsRequired();
                FirebaseAuthHandler.MirrorCurrentUserToLocalSave();
                if (!allowPendingFacebookResolution)
                    CoinSafeCloudSync.RequestCheckpointSync();
                return CreateSuccess();
            }
            catch (Exception exception)
            {
                FirebaseSyncFailureKind failureKind = ClassifyFailure(exception);
                Debug.LogWarning("[Firebase] Progress upload failed. Kind=" + failureKind + ", error=" + exception.Message);
                return CreateFailure(failureKind, exception.Message);
            }
        }

        public static async Task<bool> UploadCoinSafeAmountAsync(string expectedUid, int amount)
        {
            try
            {
                if (CoinSafeProgress.IsFacebookCloudWriteBlocked
                    || string.IsNullOrWhiteSpace(expectedUid)
                    || !await FirebaseAuthHandler.EnsureSignedInAsync())
                    return false;

                string uid = FirebaseAuthHandler.CurrentUserId;
                if (FirebaseAuthHandler.IsCurrentUserAnonymous
                    || !string.Equals(uid, expectedUid.Trim(), StringComparison.Ordinal))
                {
                    Debug.LogWarning("[Firebase] Coin Safe upload skipped because the authenticated user changed.");
                    return false;
                }

                Dictionary<string, object> data = new Dictionary<string, object>
                {
                    ["uid"] = uid,
                    ["coinSafeAmount"] = Mathf.Max(0, amount),
                    ["coinSafeUpdatedAt"] = CloudFieldValue.ServerTimestamp
                };

                if (CoinSafeProgress.IsFacebookCloudWriteBlocked
                    || !FirebaseAuthHandler.IsCurrentFacebookUser(expectedUid))
                {
                    Debug.LogWarning("[Firebase] Coin Safe upload cancelled because Facebook sync started or the authenticated user changed.");
                    return false;
                }

                var document = FirebaseAuthHandler.Documents.Collection(PlayersCollection).Document(uid);
                await FirebaseAuthHandler.Documents.RunTransactionAsync(async transaction =>
                {
                    var snapshot = await transaction.GetSnapshotAsync(document);
                    SaveCloudData.ValidateWrite(snapshot.Exists ? snapshot.ToDictionary() : null,
                        new Dictionary<string, object> { { "saveVersion", ProgressSaveVersion } });
                    if (!FirebaseAuthHandler.IsCurrentFacebookUser(expectedUid))
                        throw new InvalidOperationException("The authenticated user changed during the cloud write.");
                    transaction.Set(document, data, CloudWriteMode.Merge);
                });
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[Firebase] Coin Safe upload failed: " + exception.Message);
                return false;
            }
        }

        public static async Task<FirebaseProgressDownloadResult> DownloadProgressAsync()
        {
            try
            {
                if (!await FirebaseAuthHandler.EnsureSignedInAsync())
                {
                    return new FirebaseProgressDownloadResult
                    {
                        Status = FirebaseProgressDownloadStatus.Failed,
                        FailureKind = FirebaseSyncFailureKind.Authentication,
                        Error = "Firebase authentication is unavailable."
                    };
                }

                string uid = FirebaseAuthHandler.CurrentUserId;
                if (string.IsNullOrEmpty(uid))
                {
                    return new FirebaseProgressDownloadResult
                    {
                        Status = FirebaseProgressDownloadStatus.Failed,
                        FailureKind = FirebaseSyncFailureKind.Authentication,
                        Error = "Firebase user ID is unavailable."
                    };
                }

                Debug.Log("[Firebase] Downloading player progress. UID=" + uid + ".");
                CloudDocumentSnapshot snapshot = await AwaitWithTimeout(
                    FirebaseAuthHandler.Documents
                        .Collection(PlayersCollection)
                        .Document(uid)
                        .GetSnapshotAsync(CloudReadSource.Server),
                    "downloading player progress from the server");

                if (!FirebaseAuthHandler.IsCurrentFacebookUser(uid))
                {
                    return new FirebaseProgressDownloadResult
                    {
                        Status = FirebaseProgressDownloadStatus.Failed,
                        FailureKind = FirebaseSyncFailureKind.SessionChanged,
                        Error = "The authenticated Facebook account changed during download."
                    };
                }

                if (!snapshot.Exists)
                {
                    Debug.Log("[Firebase] No cloud progress document found. UID=" + uid + ".");
                    return new FirebaseProgressDownloadResult
                    {
                        Status = FirebaseProgressDownloadStatus.NotFound,
                        Progress = CreateEmptyProgress(uid)
                    };
                }

                FirebasePlayerProgress progress = CreateProgressFromSnapshot(uid, snapshot);
                Debug.Log("[Firebase] Cloud progress loaded: uid=" + progress.Uid
                    + ", hasLevelProgress=" + progress.HasLevelProgress
                    + ", maxReachedLevelIndex=" + progress.MaxReachedLevelIndex
                    + ", displayLevelIndex=" + progress.DisplayLevelIndex
                    + ", realLevelIndex=" + progress.RealLevelIndex
                    + ", hasCoinBalance=" + progress.HasCoinBalance
                    + ", coinBalance=" + progress.CoinBalance
                    + ", hasCoinSafeAmount=" + progress.HasCoinSafeAmount
                    + ", coinSafeAmount=" + progress.CoinSafeAmount
                    + ", saveVersion=" + progress.SaveVersion + ".");
                return new FirebaseProgressDownloadResult
                {
                    Status = FirebaseProgressDownloadStatus.Found,
                    Progress = progress
                };
            }
            catch (Exception exception)
            {
                FirebaseSyncFailureKind failureKind = ClassifyFailure(exception);
                Debug.LogWarning("[Firebase] Progress download failed. Kind=" + failureKind + ", error=" + exception.Message);
                return new FirebaseProgressDownloadResult
                {
                    Status = FirebaseProgressDownloadStatus.Failed,
                    FailureKind = failureKind,
                    Error = exception.Message
                };
            }
        }

        public static bool HasProgressConflict(FirebasePlayerProgress cloudProgress)
        {
            if (!SaveController.IsSaveLoaded || cloudProgress == null || !cloudProgress.CloudStateKnown)
                return false;
            if (cloudProgress.SaveVersion > ProgressSaveVersion) return true;

            PlayerProfileSave localProfile = GetLocalProfile();
            LevelSave localLevel = SaveController.GetSaveObject<LevelSave>();

            bool profileConflict = cloudProgress.HasProfile
                && (!string.Equals(NormalizeName(localProfile.PlayerName), NormalizeName(cloudProgress.PlayerName), StringComparison.Ordinal)
                    || localProfile.AvatarIndex != cloudProgress.AvatarIndex
                    || localProfile.FrameIndex != cloudProgress.FrameIndex);
            bool levelConflict = cloudProgress.HasLevelProgress
                && (localLevel.MaxReachedLevelIndex != cloudProgress.MaxReachedLevelIndex
                    || localLevel.DisplayLevelIndex != cloudProgress.DisplayLevelIndex
                    || localLevel.RealLevelIndex != cloudProgress.RealLevelIndex);
            bool coinConflict = cloudProgress.HasCoinBalance
                && GetLocalCoinBalance() != cloudProgress.CoinBalance;
            bool coinSafeConflict = cloudProgress.HasCoinSafeAmount
                && CoinSafeProgress.Amount != cloudProgress.CoinSafeAmount;
            bool powerUpConflict = cloudProgress.HasPowerUpAmounts
                && !PowerUpAmountsEqual(GetLocalPowerUpAmounts(), cloudProgress.PowerUpAmounts);
            bool frameworkConflict = false;
            if (cloudProgress.FrameworkSave != null)
            {
                try { frameworkConflict = SaveCloudData.HasConflict(SaveController.ExportCloudData(), cloudProgress.FrameworkSave); }
                catch (Exception exception)
                {
                    Debug.LogWarning("[Firebase] Cloud schema requires attention before syncing: " + exception.Message);
                    frameworkConflict = true;
                }
            }
            bool hasConflict = profileConflict || levelConflict || coinConflict || coinSafeConflict || powerUpConflict || frameworkConflict;

            Debug.Log("[Firebase] Progress conflict check: localMaxReachedLevelIndex=" + localLevel.MaxReachedLevelIndex
                + ", localDisplayLevelIndex=" + localLevel.DisplayLevelIndex
                + ", localRealLevelIndex=" + localLevel.RealLevelIndex
                + ", localCoinBalance=" + GetLocalCoinBalance()
                + ", localCoinSafeAmount=" + CoinSafeProgress.Amount
                + ", cloudMaxReachedLevelIndex=" + cloudProgress.MaxReachedLevelIndex
                + ", cloudDisplayLevelIndex=" + cloudProgress.DisplayLevelIndex
                + ", cloudRealLevelIndex=" + cloudProgress.RealLevelIndex
                + ", cloudCoinBalance=" + cloudProgress.CoinBalance
                + ", cloudCoinSafeAmount=" + cloudProgress.CoinSafeAmount
                + ", result=" + hasConflict + ".");
            return hasConflict;
        }

        public static bool ApplyCloudProgressToLocalSave(FirebasePlayerProgress cloudProgress)
        {
            if (!SaveController.IsSaveLoaded || cloudProgress == null || !cloudProgress.Exists || !cloudProgress.CloudStateKnown
                || string.IsNullOrEmpty(cloudProgress.Uid)
                || !FirebaseAuthHandler.IsCurrentFacebookUser(cloudProgress.Uid))
                return false;

            try
            {
                if (cloudProgress.SaveVersion > ProgressSaveVersion)
                    throw new InvalidOperationException("Cloud progress is newer than this client. Restore is blocked.");
                if (cloudProgress.FrameworkSave != null) SaveController.ApplyCloudData(cloudProgress.FrameworkSave);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[Firebase] Cloud restore was blocked before applying progress: " + exception.Message);
                return false;
            }

            PlayerProfileSave profile = GetLocalProfile();
            profile.FirebaseUid = cloudProgress.Uid;
            if (cloudProgress.HasProfile)
            {
                profile.PlayerName = string.IsNullOrWhiteSpace(cloudProgress.PlayerName)
                    ? GetDefaultPlayerName()
                    : cloudProgress.PlayerName.Trim();
                profile.AvatarIndex = Mathf.Max(0, cloudProgress.AvatarIndex);
                profile.FrameIndex = Mathf.Max(0, cloudProgress.FrameIndex);
            }

            LevelSave levelSave = SaveController.GetSaveObject<LevelSave>();
            if (cloudProgress.HasLevelProgress)
            {
                levelSave.MaxReachedLevelIndex = Mathf.Max(0, cloudProgress.MaxReachedLevelIndex);
                levelSave.DisplayLevelIndex = Mathf.Max(0, cloudProgress.DisplayLevelIndex);
                levelSave.RealLevelIndex = Mathf.Max(-1, cloudProgress.RealLevelIndex);
                levelSave.FirstStart = true;
                levelSave.IsPlayingRandomLevel = false;
            }

            if (cloudProgress.HasCoinBalance)
                CurrencyController.Set(CurrencyType.Coins, Mathf.Max(0, cloudProgress.CoinBalance));

            if (cloudProgress.HasPowerUpAmounts)
                ApplyLocalPowerUpAmounts(cloudProgress.PowerUpAmounts);

            if (cloudProgress.HasCoinSafeAmount)
            {
                CoinSafeProgress.ResolveFacebookWithCloud(
                    cloudProgress.Uid,
                    cloudProgress.CoinSafeAmount,
                    requiresUpload: false,
                    requestCloudSync: false);
            }

            SaveController.MarkAsSaveIsRequired();
            return true;
        }

        private static FirebasePlayerProgress CreateEmptyProgress(string uid)
        {
            return new FirebasePlayerProgress
            {
                Uid = uid,
                PlayerName = GetDefaultPlayerName(),
                AvatarIndex = 5,
                FrameIndex = 5,
                MaxReachedLevelIndex = 0,
                DisplayLevelIndex = 0,
                RealLevelIndex = 0,
                CoinBalance = 0,
                CoinSafeAmount = CoinSafeProgress.InitialAmount,
                PowerUpAmounts = new Dictionary<string, int>(),
                SaveVersion = 0,
                Exists = false,
                CloudStateKnown = true,
                HasProfile = false,
                HasLevelProgress = false,
                HasCoinBalance = false,
                HasCoinSafeAmount = false,
                HasPowerUpAmounts = false
            };
        }

        private static FirebasePlayerProgress CreateProgressFromSnapshot(string uid, CloudDocumentSnapshot snapshot)
        {
            FirebasePlayerProgress progress = new FirebasePlayerProgress
            {
                Uid = uid,
                Exists = snapshot.Exists,
                PlayerName = GetString(snapshot, "PlayerName", GetDefaultPlayerName()),
                AvatarIndex = GetInt(snapshot, "PlayerAvatar", 5),
                FrameIndex = GetInt(snapshot, "PlayerFrame", 5),
                MaxReachedLevelIndex = GetInt(snapshot, "maxReachedLevelIndex", GetInt(snapshot, "maxLevel", GetInt(snapshot, "score", 0))),
                DisplayLevelIndex = GetInt(snapshot, "displayLevelIndex", 0),
                RealLevelIndex = GetSignedInt(snapshot, "realLevelIndex", 0, -1),
                CoinBalance = GetInt(snapshot, "coinBalance", 0),
                CoinSafeAmount = GetInt(snapshot, "coinSafeAmount", CoinSafeProgress.InitialAmount),
                PowerUpAmounts = GetIntMap(snapshot, "powerUps"),
                SaveVersion = GetInt(snapshot, "saveVersion", 0),
                FrameworkSave = snapshot.TryGetValue<Dictionary<string, object>>(SaveCloudData.Field, out var frameworkSave) ? frameworkSave : null
            };

            progress.CloudStateKnown = true;
            progress.HasProfile = ContainsField(snapshot, "PlayerName")
                || ContainsField(snapshot, "PlayerAvatar")
                || ContainsField(snapshot, "PlayerFrame");
            progress.HasLevelProgress = ContainsField(snapshot, "maxReachedLevelIndex")
                || ContainsField(snapshot, "maxLevel")
                || ContainsField(snapshot, "score")
                || ContainsField(snapshot, "displayLevelIndex")
                || ContainsField(snapshot, "realLevelIndex");
            progress.HasCoinBalance = ContainsField(snapshot, "coinBalance");
            progress.HasCoinSafeAmount = ContainsField(snapshot, "coinSafeAmount");
            progress.HasPowerUpAmounts = ContainsField(snapshot, "powerUps");
            progress.HasUpdatedAt = TryGetUpdatedAt(snapshot, out progress.UpdatedAtUtc);

            return progress;
        }

        private static Dictionary<string, int> GetIntMap(CloudDocumentSnapshot snapshot, string field)
        {
            Dictionary<string, int> result = new Dictionary<string, int>();
            Dictionary<string, object> rawMap;
            if (snapshot == null || !snapshot.TryGetValue(field, out rawMap) || rawMap == null)
                return result;

            foreach (KeyValuePair<string, object> pair in rawMap)
            {
                int amount;
                if (pair.Value is long longValue)
                    amount = (int)Math.Max(0L, Math.Min(longValue, int.MaxValue));
                else if (pair.Value is int intValue)
                    amount = Mathf.Max(0, intValue);
                else
                    continue;

                result[pair.Key] = amount;
            }

            return result;
        }

        private static bool TryGetUpdatedAt(CloudDocumentSnapshot snapshot, out DateTime updatedAtUtc)
        {
            updatedAtUtc = default;
            if (snapshot != null && snapshot.TryGetValue<DateTime>("updatedAt", out var dateTime))
            {
                updatedAtUtc = dateTime.ToUniversalTime();
                return true;
            }
            return false;
        }

        private static bool ContainsField(CloudDocumentSnapshot snapshot, string field)
        {
            object value;
            return snapshot != null && snapshot.TryGetValue(field, out value);
        }

        private static string GetString(CloudDocumentSnapshot snapshot, string field, string fallback)
        {
            string value;
            return snapshot != null && snapshot.TryGetValue(field, out value) && !string.IsNullOrWhiteSpace(value)
                ? value
                : fallback;
        }

        private static int GetInt(CloudDocumentSnapshot snapshot, string field, int fallback)
        {
            long longValue;
            if (snapshot != null && snapshot.TryGetValue(field, out longValue))
                return (int)Math.Max(0L, Math.Min(longValue, int.MaxValue));

            int intValue;
            if (snapshot != null && snapshot.TryGetValue(field, out intValue))
                return Mathf.Max(0, intValue);

            return fallback;
        }

        private static int GetSignedInt(CloudDocumentSnapshot snapshot, string field, int fallback, int minValue)
        {
            long longValue;
            if (snapshot != null && snapshot.TryGetValue(field, out longValue))
                return (int)Math.Max((long)minValue, Math.Min(longValue, int.MaxValue));

            int intValue;
            if (snapshot != null && snapshot.TryGetValue(field, out intValue))
                return Mathf.Clamp(intValue, minValue, int.MaxValue);

            return fallback;
        }

        private static async Task<PlayerNameClaimResult> WritePlayerDocumentAsync(
            string uid,
            string requestedName,
            Dictionary<string, object> playerData)
        {
            bool retryGeneratedDefault = string.IsNullOrWhiteSpace(requestedName)
                || string.Equals(requestedName.Trim(), GetDefaultPlayerName(), StringComparison.Ordinal);
            string candidate = string.IsNullOrWhiteSpace(requestedName) ? GetDefaultPlayerName() : requestedName.Trim();
            PlayerNameClaimResult result = null;

            for (int attempt = 0; attempt < 5; attempt++)
            {
                result = await AwaitWithTimeout(
                    FirebasePlayerNameRegistry.ClaimAndWritePlayerAsync(
                        FirebaseAuthHandler.Documents,
                        FirebaseAuthHandler.Documents.Collection(PlayersCollection).Document(uid),
                        uid,
                        candidate,
                        playerData),
                    "claiming the player name");

                if (result == null || result.Status != PlayerNameClaimStatus.Taken || !retryGeneratedDefault)
                    return result ?? new PlayerNameClaimResult { Status = PlayerNameClaimStatus.Failed };

                candidate = FirebasePlayerNameRegistry.CreateDefaultName(User.LocalId, attempt + 1);
            }

            return result ?? new PlayerNameClaimResult { Status = PlayerNameClaimStatus.Failed };
        }

        private static FirebaseProgressOperationResult CreateSuccess()
        {
            return new FirebaseProgressOperationResult
            {
                Succeeded = true,
                FailureKind = FirebaseSyncFailureKind.None
            };
        }

        private static FirebaseProgressOperationResult CreateFailure(FirebaseSyncFailureKind failureKind, string error)
        {
            return new FirebaseProgressOperationResult
            {
                Succeeded = false,
                FailureKind = failureKind,
                Error = error
            };
        }

        private static FirebaseSyncFailureKind ClassifyFailure(Exception exception)
        {
            if (exception is TimeoutException) return FirebaseSyncFailureKind.Timeout;
            if (exception is CloudException cloud)
            {
                switch (cloud.Error)
                {
                    case CloudError.Timeout: return FirebaseSyncFailureKind.Timeout;
                    case CloudError.PermissionDenied: return FirebaseSyncFailureKind.PermissionDenied;
                    case CloudError.Offline: return FirebaseSyncFailureKind.Offline;
                    case CloudError.Authentication: return FirebaseSyncFailureKind.Authentication;
                }
            }
            return FirebaseSyncFailureKind.Unknown;
        }

        private static async Task AwaitWithTimeout(Task task, string operation)
        {
            Task completed = await Task.WhenAny(task, Task.Delay(FirebaseServerTimeoutMs));
            if (completed != task)
                throw new TimeoutException("[Firebase] Timed out while " + operation + ".");

            await task;
        }

        private static async Task<T> AwaitWithTimeout<T>(Task<T> task, string operation)
        {
            Task completed = await Task.WhenAny(task, Task.Delay(FirebaseServerTimeoutMs));
            if (completed != task)
                throw new TimeoutException("[Firebase] Timed out while " + operation + ".");

            return await task;
        }

        private static string NormalizeName(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private static int GetLocalCoinBalance()
        {
            try
            {
                return Mathf.Max(0, CurrencyController.Get(CurrencyType.Coins));
            }
            catch (Exception)
            {
                return 0;
            }
        }

        private static Dictionary<string, int> GetLocalPowerUpAmounts()
        {
            Dictionary<string, int> amounts = new Dictionary<string, int>();
            foreach (PUType powerUpType in (PUType[])Enum.GetValues(typeof(PUType)))
            {
                int amount = 0;
                try
                {
                    PUSave save = SaveController.GetSaveObject<PUSave>("powerUp_" + powerUpType);
                    amount = save != null ? Mathf.Max(0, save.Amount) : 0;
                }
                catch (Exception)
                {
                    amount = 0;
                }

                amounts[powerUpType.ToString()] = amount;
            }

            return amounts;
        }

        private static void ApplyLocalPowerUpAmounts(Dictionary<string, int> amounts)
        {
            if (amounts == null)
                return;

            foreach (PUType powerUpType in (PUType[])Enum.GetValues(typeof(PUType)))
            {
                int amount;
                if (!amounts.TryGetValue(powerUpType.ToString(), out amount))
                    amount = 0;

                amount = Mathf.Max(0, amount);
                try
                {
                    PUSave save = SaveController.GetSaveObject<PUSave>("powerUp_" + powerUpType);
                    if (save != null)
                        save.Amount = amount;
                }
                catch (Exception)
                {
                }
            }
        }

        private static bool PowerUpAmountsEqual(
            Dictionary<string, int> first,
            Dictionary<string, int> second)
        {
            first = first ?? new Dictionary<string, int>();
            second = second ?? new Dictionary<string, int>();
            foreach (PUType powerUpType in (PUType[])Enum.GetValues(typeof(PUType)))
            {
                string key = powerUpType.ToString();
                int firstAmount = first.ContainsKey(key) ? first[key] : 0;
                int secondAmount = second.ContainsKey(key) ? second[key] : 0;
                if (firstAmount != secondAmount)
                    return false;
            }

            return true;
        }

    }
}
