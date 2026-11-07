using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

using NebulaSoft.Cloud;

#if FACEBOOK
using Facebook.Unity;
#endif

namespace NebulaSoft
{
    public enum FirebaseFacebookSignInStatus
    {
        Success,
        NeedsConflictResolution,
        Cancelled,
        Failed
    }

    public sealed class FirebaseFacebookSignInResult
    {
        public FirebaseFacebookSignInStatus Status;
        public string UserId;
        public bool LinkedAnonymousUser;
        public string Error;

        public bool IsSignedIn
        {
            get
            {
                return Status == FirebaseFacebookSignInStatus.Success
                    || Status == FirebaseFacebookSignInStatus.NeedsConflictResolution;
            }
        }
    }

    public static class FirebaseAuthHandler
    {
        public static ICloudAuthProvider Authentication => CloudServices.IsReady ? CloudServices.Backend?.Auth : null;
        public static ICloudDocumentStore Documents => CloudServices.IsReady ? CloudServices.Backend?.Documents : null;

        private static int sessionGeneration;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            sessionGeneration++;
            CloudServices.Shutdown();
        }

        public static bool IsReady => CloudServices.IsReady;

        public static string CurrentUserId
        {
            get
            {
                return Authentication != null && Authentication.CurrentUser != null ? Authentication.CurrentUser.UserId : null;
            }
        }

        public static bool IsCurrentUserAnonymous
        {
            get
            {
                return Authentication == null || Authentication.CurrentUser == null || Authentication.CurrentUser.IsAnonymous;
            }
        }

        public static bool IsCurrentFacebookUser(string uid)
        {
            return Authentication != null
                && Authentication.CurrentUser != null
                && !Authentication.CurrentUser.IsAnonymous
                && !string.IsNullOrWhiteSpace(uid)
                && string.Equals(Authentication.CurrentUser.UserId, uid.Trim(), System.StringComparison.Ordinal);
        }

        public static bool IsFacebookSyncSignedIn
        {
            get
            {
                if (Authentication != null && Authentication.CurrentUser != null)
                    return !Authentication.CurrentUser.IsAnonymous;

                if (SaveController.IsSaveLoaded)
                {
                    PlayerProfileSave save = SaveController.GetSaveObject<PlayerProfileSave>("Player Profile Save");
                    if (save != null && !string.IsNullOrWhiteSpace(save.FirebaseUid))
                        return true;
                }

                return false;
            }
        }

        public static async Task<bool> EnsureInitializedAsync()
        {
            int runGeneration = sessionGeneration;
            if (CloudServices.Backend == null)
            {
                var backend = NebulaSoft.Integrations.Firebase.FirebaseBackendFactory.Create();
                if (backend == null) return false;
                CloudServices.Configure(backend);
            }
            bool ready = await CloudServices.EnsureInitializedAsync();
            if (!ready && CloudServices.LastInitializationError != null)
                Debug.LogWarning("[Cloud] Initialization failed: " + CloudServices.LastInitializationError.Message);
            return ready && runGeneration == sessionGeneration;
        }

        public static async Task<bool> EnsureSignedInAsync()
        {
            int runGeneration = sessionGeneration;
            if (!await EnsureInitializedAsync() || runGeneration != sessionGeneration)
                return false;

            ICloudAuthProvider auth = Authentication;
            if (auth == null)
                return false;

            if (auth.CurrentUser == null)
                await auth.SignInAnonymouslyAsync();

            if (runGeneration != sessionGeneration || Authentication != auth || auth.CurrentUser == null)
                return false;

            User.SetCustomId(auth.CurrentUser.UserId);
            FirebaseNoAdsEntitlementSync.RestoreForCurrentUser();
            return true;
        }

        public static async Task<FirebaseFacebookSignInResult> SignInWithFacebookAsync()
        {
            int runGeneration = sessionGeneration;
            Debug.Log("[Facebook] Requesting an access token.");
            string accessToken = await RequestFacebookAccessTokenAsync();
            if (runGeneration != sessionGeneration)
            {
                return new FirebaseFacebookSignInResult
                {
                    Status = FirebaseFacebookSignInStatus.Failed,
                    Error = "The authentication session changed."
                };
            }

            if (string.IsNullOrEmpty(accessToken))
            {
                Debug.LogWarning("[Facebook] No access token returned. Login was cancelled or failed before Firebase authentication.");
                return new FirebaseFacebookSignInResult
                {
                    Status = FirebaseFacebookSignInStatus.Cancelled,
                    Error = "Facebook login was cancelled or no access token was returned."
                };
            }

            return await SignInWithFacebookAsync(accessToken);
        }

        public static async Task<FirebaseFacebookSignInResult> SignInWithFacebookAsync(string accessToken)
        {
            int runGeneration = sessionGeneration;
            FirebaseFacebookSignInResult result = new FirebaseFacebookSignInResult
            {
                Status = FirebaseFacebookSignInStatus.Failed
            };

            if (string.IsNullOrEmpty(accessToken))
            {
                result.Error = "Facebook access token is empty.";
                return result;
            }

            try
            {
                Debug.Log("[Firebase] Starting Facebook credential authentication.");
                if (!await EnsureInitializedAsync() || runGeneration != sessionGeneration)
                {
                    result.Error = "Firebase is not initialized.";
                    Debug.LogWarning("[Firebase] Facebook authentication stopped because Firebase initialization failed.");
                    return result;
                }

                ICloudAuthProvider auth = Authentication;
                if (auth == null)
                {
                    result.Error = "Firebase Authentication is unavailable.";
                    return result;
                }

                bool hadAnonymousUser = auth.CurrentUser != null && auth.CurrentUser.IsAnonymous;
                Debug.Log("[Firebase] Current user before Facebook authentication: "
                    + (auth.CurrentUser != null ? auth.CurrentUser.UserId : "none")
                    + ", anonymous=" + hadAnonymousUser + ".");

                if (auth.CurrentUser != null)
                {
                    try
                    {
                        CloudUser linkedUser = await auth.LinkFacebookAsync(accessToken);
                        if (runGeneration != sessionGeneration || Authentication != auth)
                        {
                            result.Error = "The authentication session changed.";
                            return result;
                        }

                        ApplySignedInUser(linkedUser);
                        result.Status = FirebaseFacebookSignInStatus.Success;
                        result.UserId = linkedUser != null ? linkedUser.UserId : CurrentUserId;
                        result.LinkedAnonymousUser = hadAnonymousUser;
                        MirrorCurrentUserToLocalSave();
                        Debug.Log("[Firebase] Facebook credential linked successfully. UID=" + result.UserId + ".");
                        return result;
                    }
                    catch (System.Exception exception)
                    {
                        if (runGeneration != sessionGeneration || Authentication != auth)
                        {
                            result.Error = "The authentication session changed.";
                            return result;
                        }

                        if (!IsCredentialAlreadyInUse(exception))
                            throw;

                        Debug.LogWarning("[Firebase] Facebook credential belongs to an existing Firebase user. Signing in to that account. Link error type="
                            + exception.GetType().Name + ", message=" + exception.Message + ".");
                    }
                }

                CloudUser signedInUser = await auth.SignInFacebookAsync(accessToken);
                if (runGeneration != sessionGeneration || Authentication != auth)
                {
                    result.Error = "The authentication session changed.";
                    return result;
                }

                ApplySignedInUser(signedInUser);
                result.Status = hadAnonymousUser
                    ? FirebaseFacebookSignInStatus.NeedsConflictResolution
                    : FirebaseFacebookSignInStatus.Success;
                result.UserId = signedInUser != null ? signedInUser.UserId : CurrentUserId;
                result.LinkedAnonymousUser = false;
                MirrorCurrentUserToLocalSave();
                Debug.Log("[Firebase] Facebook sign-in completed. UID=" + result.UserId
                    + ", status=" + result.Status + ".");
                return result;
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning("[Firebase] Facebook sign-in failed: " + exception.Message);
                result.Status = FirebaseFacebookSignInStatus.Failed;
                result.Error = exception.Message;
                return result;
            }
        }

        public static async Task<bool> SignOutFacebookSyncAsync()
        {
            bool success = true;
            bool firebaseAccountDetached = false;

            await CoinSafeCloudSync.FlushActiveFacebookAsync(CoinSafeCloudSync.LogoutFlushTimeout);

#if FACEBOOK
            try
            {
                if (FB.IsInitialized && FB.IsLoggedIn)
                {
                    FB.LogOut();
                    Debug.Log("[Facebook] Logged out from Facebook SDK.");
                }
            }
            catch (System.Exception exception)
            {
                success = false;
                Debug.LogWarning("[Facebook] Logout failed: " + exception.Message);
            }
#endif

            try
            {
                if (!await EnsureInitializedAsync() || Authentication == null)
                {
                    // With no backend installed there is no native account to detach (SDK-free template).
                    firebaseAccountDetached = CloudServices.Backend == null;
                    success = firebaseAccountDetached;
                    if (!firebaseAccountDetached)
                        Debug.LogWarning("[Firebase] Facebook sync sign-out could not verify the current Firebase session.");
                }
                else
                {
                    Authentication.SignOut();
                    firebaseAccountDetached = true;
                    Debug.Log("[Firebase] Signed out from Facebook-linked Firebase user.");

                    await Authentication.SignInAnonymouslyAsync();
                    CloudUser anonymousUser = Authentication.CurrentUser;
                    if (anonymousUser == null || !anonymousUser.IsAnonymous)
                    {
                        success = false;
                        firebaseAccountDetached = anonymousUser == null;
                        Debug.LogWarning("[Firebase] Anonymous session was not established after Facebook sign-out.");
                    }
                    else
                    {
                        ApplySignedInUser(anonymousUser);
                        Debug.Log("[Firebase] Signed in anonymously after Facebook sign-out. UID="
                            + anonymousUser.UserId + ".");
                    }
                }
            }
            catch (System.Exception exception)
            {
                success = false;
                firebaseAccountDetached = Authentication == null
                    || Authentication.CurrentUser == null
                    || Authentication.CurrentUser.IsAnonymous;
                Debug.LogWarning("[Firebase] Facebook sync sign-out failed: " + exception.Message);
            }

            if (firebaseAccountDetached)
            {
                ClearFacebookSyncFromLocalSave();
            }
            else
            {
                success = false;
                Debug.LogWarning("[Firebase] Keeping the current local progress active because Firebase sign-out was not confirmed.");
            }

            return success && firebaseAccountDetached;
        }

        private static async Task<string> RequestFacebookAccessTokenAsync()
        {
#if FACEBOOK
            TaskCompletionSource<string> tokenSource = new TaskCompletionSource<string>();

            System.Action login = () =>
            {
                Debug.Log("[Facebook] Opening Facebook login.");
                FB.LogInWithReadPermissions(new List<string> { "public_profile" }, result =>
                {
                    if (result == null)
                    {
                        Debug.LogWarning("[Facebook] Login callback returned no result.");
                        tokenSource.TrySetResult(null);
                        return;
                    }

                    if (result.Cancelled)
                    {
                        Debug.LogWarning("[Facebook] Login was cancelled by the user.");
                        tokenSource.TrySetResult(null);
                        return;
                    }

                    if (!string.IsNullOrEmpty(result.Error))
                    {
                        Debug.LogError("[Facebook] Login failed: " + result.Error);
                        tokenSource.TrySetResult(null);
                        return;
                    }

                    AccessToken token = AccessToken.CurrentAccessToken;
                    Debug.Log(token != null
                        ? "[Facebook] Access token received successfully."
                        : "[Facebook] Login succeeded but AccessToken.CurrentAccessToken is null.");
                    tokenSource.TrySetResult(token != null ? token.TokenString : null);
                });
            };

            if (FB.IsInitialized)
            {
                Debug.Log("[Facebook] SDK is already initialized.");
                login();
            }
            else
            {
                Debug.Log("[Facebook] Initializing SDK.");
                FB.Init(() =>
                {
                    if (!FB.IsInitialized)
                    {
                        Debug.LogError("[Facebook] SDK initialization callback completed, but FB.IsInitialized is false.");
                        tokenSource.TrySetResult(null);
                        return;
                    }

                    Debug.Log("[Facebook] SDK initialized successfully.");
                    login();
                }, isGameShown => { });
            }

            return await tokenSource.Task;
#else
            await Task.CompletedTask;
            Debug.LogWarning("[Facebook] SDK is not enabled. Import Facebook SDK for Unity and add the FACEBOOK scripting define.");
            return null;
#endif
        }

        private static bool IsCredentialAlreadyInUse(System.Exception exception)
        {
            return exception is CloudException cloud && cloud.Error == CloudError.CredentialAlreadyInUse;
        }

        private static void ApplySignedInUser(CloudUser user)
        {
            if (user != null)
            {
                User.SetCustomId(user.UserId);
                FirebaseNoAdsEntitlementSync.RestoreForCurrentUser();
            }
        }

        public static void MirrorCurrentUserToLocalSave()
        {
            if (Authentication == null || Authentication.CurrentUser == null || !SaveController.IsSaveLoaded)
                return;

            PlayerProfileSave save = SaveController.GetSaveObject<PlayerProfileSave>("Player Profile Save");
            User.SetCustomId(Authentication.CurrentUser.UserId);
            save.FirebaseUid = Authentication.CurrentUser.IsAnonymous ? null : Authentication.CurrentUser.UserId;
            SaveController.MarkAsSaveIsRequired();
        }

        private static void ClearFacebookSyncFromLocalSave()
        {
            if (!SaveController.IsSaveLoaded)
                return;

            PlayerProfileSave save = SaveController.GetSaveObject<PlayerProfileSave>("Player Profile Save");
            if (save == null)
                return;

            save.FirebaseUid = null;
            SaveController.MarkAsSaveIsRequired();
            Debug.Log("[Profile] Facebook sync UID cleared from local save.");
        }
    }
}
