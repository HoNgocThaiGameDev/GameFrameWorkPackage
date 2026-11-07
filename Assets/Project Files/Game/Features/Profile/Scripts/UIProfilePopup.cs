using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NebulaSoft
{
    public class UIProfilePopup : UIPage
    {
        private const float ShowDuration = 0.22f;
        private const float HideDuration = 0.18f;
        private const float ToastVisibleTime = 1.2f;
        private const float SyncSuccessVisibleTime = 0.9f;
        private const string FacebookSignInLabel = "Sign in with Facebook";
        private const string FacebookSignOutLabel = "Sign out";
        private const string FacebookRetrySyncLabel = "Retry sync";

        private enum ProgressSyncRetryStep
        {
            None,
            SignIn,
            Download,
            CommitLocalSelection,
            ApplyFreshCloudSelection,
            SignOut
        }

        public static event System.Action<bool> ProfilePopupVisibilityChanged;
        public static event System.Action<int, int, Sprite, Sprite, string> ProfileSaved;
        public static bool IsProfilePopupVisible { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            IsProfilePopupVisible = false;
            ProfilePopupVisibilityChanged = null;
            ProfileSaved = null;
        }

        [Header("Containers")]
        [SerializeField] RectTransform popupRoot;
        [SerializeField] Image dimOverlayImage;
        [SerializeField] CanvasGroup rootCanvasGroup;
        [SerializeField] CanvasGroup dialogCanvasGroup;
        [SerializeField] RectTransform dialogRoot;
        [SerializeField] GameObject saveProgressDialog;
        [SerializeField] LoginResultDialogView loginResultDialog;
        [SerializeField] SaveProgressFoundDialogView saveProgressFoundDialog;
        [SerializeField] CanvasGroup toastCanvasGroup;
        [SerializeField] RectTransform toastRoot;

        [Header("Profile")]
        [SerializeField] Button closeButton;
        [SerializeField] Button editNameButton;
        [SerializeField] Button saveProgressButton;
        [SerializeField] Button saveButton;
        [SerializeField] TMP_InputField nameInputField;
        [SerializeField] TextMeshProUGUI nameText;
        [SerializeField] Image previewAvatarImage;
        [SerializeField] Image previewFrameImage;
        [SerializeField] Image saveButtonImage;
        [SerializeField] Sprite saveButtonInactiveSprite;
        [SerializeField] Sprite saveButtonActiveSprite;

        [Header("Tabs")]
        [SerializeField] Button avatarTabButton;
        [SerializeField] Button frameTabButton;
        [SerializeField] Image avatarTabBackground;
        [SerializeField] Image frameTabBackground;
        [SerializeField] Sprite tabActiveSprite;
        [SerializeField] Sprite tabInactiveSprite;
        [SerializeField] GameObject avatarContent;
        [SerializeField] GameObject frameContent;

        [Header("Items")]
        [SerializeField] ProfileSelectableItem[] avatarItems;
        [SerializeField] ProfileSelectableItem[] frameItems;
        [SerializeField] Sprite[] avatarSprites;
        [SerializeField] Sprite[] frameSprites;

        [Header("Dialog Buttons")]
        [SerializeField] Button saveProgressCloseButton;
        [SerializeField] Button facebookLoginButton;
        [SerializeField] Button mockSuccessButton;
        [SerializeField] Button mockFailedButton;

        [Header("Overlay")]
        [SerializeField] Color dimOverlayColor = new Color(0f, 0f, 0f, 0.72f);
        [SerializeField] int popupSortingOrder = 32760;

        private int selectedAvatarIndex = 5;
        private int selectedFrameIndex = 5;
        private int savedAvatarIndex = 5;
        private int savedFrameIndex = 5;
        private string savedPlayerName;
        private bool hasPendingProfileChanges;
        private bool suppressDirtyNotifications;
        private TweenCase showTweenCase;
        private TweenCase hideTweenCase;
        private TweenCase toastTweenCase;
        private Coroutine toastRoutine;
        private Coroutine syncSuccessReloadRoutine;
        private PlayerProfileSave profileSave;
        private FirebasePlayerProgress pendingCloudProgress;
        private FirebaseFacebookSignInResult pendingFacebookSignInResult;
        private ProgressSyncRetryStep progressSyncRetryStep;
        private bool progressSyncInProgress;
        private bool missingSaveProgressDialogLogged;
        private TextMeshProUGUI facebookLoginButtonText;

        private void OnDisable()
        {
            SetProfilePopupVisible(false);
            BottomNavigationVisibilityEvents.RequestShow();
        }



        public override void Init()
        {
            SetProfilePopupVisible(false);
            EnsureReferences();
            RegisterButtons();
            LoadSavedProfile();
            RefreshFacebookButtonState();
            InitItems();
            ApplySelection();
            ShowAvatarTab();
            SetProfileDirty(false);
            CloseDialogImmediate();
            HideToastImmediate();
        }

        public override void PlayShowAnimation()
        {
            EnsureReferences();
            BringPopupToFront();
            SetProfilePopupVisible(true);
            BottomNavigationVisibilityEvents.RequestHide();
            AdsManager.HideBanner();
            LoadSavedProfile();
            ApplySelection();
            SetProfileDirty(false);
            showTweenCase.KillActive();
            hideTweenCase.KillActive();

            rootCanvasGroup.alpha = 0f;
            rootCanvasGroup.blocksRaycasts = true;
            popupRoot.localScale = Vector3.one * 0.92f;

            if (!IsTweenSystemReady)
            {
                rootCanvasGroup.alpha = 1f;
                popupRoot.localScale = Vector3.one;
                UIController.OnPageOpened(this);
                return;
            }

            rootCanvasGroup.DOFade(1f, ShowDuration, unscaledTime: true);
            showTweenCase = popupRoot
                .DOScale(Vector3.one, ShowDuration, unscaledTime: true)
                .SetEasing(Ease.Type.BackOut)
                .OnComplete(() =>
                {
                    showTweenCase = null;
                    UIController.OnPageOpened(this);
                });
        }

        public override void PlayHideAnimation()
        {
            showTweenCase.KillActive();
            hideTweenCase.KillActive();

            rootCanvasGroup.blocksRaycasts = false;

            if (!IsTweenSystemReady)
            {
                rootCanvasGroup.alpha = 0f;
                CompleteHide();
                return;
            }

            rootCanvasGroup.DOFade(0f, HideDuration, unscaledTime: true);
            hideTweenCase = popupRoot
                .DOScale(Vector3.one * 0.96f, HideDuration, unscaledTime: true)
                .SetEasing(Ease.Type.CubicIn)
                .OnComplete(CompleteHide);
        }

        private static bool IsTweenSystemReady => Tween.Tweens != null;

        private void CompleteHide()
        {
            popupRoot.localScale = Vector3.one;
            DiscardPendingProfileChanges();
            CloseDialogImmediate();
            HideToastImmediate();
            hideTweenCase = null;
            UIController.OnPageClosed(this);
            SetProfilePopupVisible(false);
            BottomNavigationVisibilityEvents.RequestShow();
            AdsManager.ShowBanner();
        }

        public void ShowAvatarTab()
        {
            SetTabState(true);
        }

        public void ShowFrameTab()
        {
            SetTabState(false);
        }

        public void SelectAvatar(int index)
        {
            if (avatarSprites == null || index < 0 || index >= avatarSprites.Length)
                return;

            selectedAvatarIndex = index;
            ApplySelection();
            SetProfileDirty(true);
        }

        public void SelectFrame(int index)
        {
            if (frameSprites == null || index < 0 || index >= frameSprites.Length)
                return;

            selectedFrameIndex = index;
            ApplySelection();
            SetProfileDirty(true);
        }

        public Sprite GetAvatarSprite(int index)
        {
            return avatarSprites != null && index >= 0 && index < avatarSprites.Length
                ? avatarSprites[index]
                : null;
        }

        public Sprite GetFrameSprite(int index)
        {
            return frameSprites != null && index >= 0 && index < frameSprites.Length
                ? frameSprites[index]
                : null;
        }

        public void OnSaveClicked()
        {
            if (!hasPendingProfileChanges)
                return;

            if (!string.Equals(GetCurrentDisplayedName(), savedPlayerName, System.StringComparison.Ordinal))
            {
                _ = SaveProfileWithUniqueNameAsync();
                return;
            }

            if (SaveProfileLocally())
            {
                CommitSavedProfileState();
                SetProfileDirty(false);
                NotifyProfileSaved();
                ShowToast();
            }
        }

        private async System.Threading.Tasks.Task SaveProfileWithUniqueNameAsync()
        {
            string requestedName = nameInputField != null ? nameInputField.text : GetCurrentDisplayedName();
            if (!FirebasePlayerNameRegistry.TryNormalize(requestedName, out string displayName, out _))
            {
                ShowProfileSaveError(FirebasePlayerNameRegistry.GetValidationError(requestedName));
                return;
            }

            SetDisplayedName(displayName);
            if (!SaveProfileLocally())
            {
                ShowProfileSaveError("Could not save the profile locally.");
                SetProfileDirty(true);
                return;
            }

            await System.Threading.Tasks.Task.CompletedTask;
            profileSave = FirebaseProfileHandler.GetLocalProfile();
            SetDisplayedName(profileSave.PlayerName);
            CommitSavedProfileState();
            SetProfileDirty(false);
            NotifyProfileSaved();
            ShowToast();
        }

        public void OnSaveProgressClicked()
        {
            RefreshFacebookButtonState();
            OpenDialog(saveProgressDialog);
        }

        public void OnFacebookLoginClicked()
        {
            ShowLoginFailed("Cloud sync is not included\nin this offline framework.");
        }

        private void OnProgressSyncRetryClicked()
        {
            if (progressSyncInProgress)
                return;

            switch (progressSyncRetryStep)
            {
                case ProgressSyncRetryStep.Download:
                    _ = RetryPendingProgressDownloadAsync();
                    break;

                case ProgressSyncRetryStep.CommitLocalSelection:
                    _ = CommitLocalSelectionAsync();
                    break;

                case ProgressSyncRetryStep.ApplyFreshCloudSelection:
                    _ = ApplyFreshCloudSelectionAsync();
                    break;

                case ProgressSyncRetryStep.SignOut:
                    _ = SignOutFacebookSyncAsync();
                    break;

                default:
                    _ = SignInAndSyncProgressAsync();
                    break;
            }
        }

        public void UseLocalProgress()
        {
            if (progressSyncInProgress)
                return;

            _ = CommitLocalSelectionAsync();
        }

        public void UseCloudProgress()
        {
            if (progressSyncInProgress)
                return;

            _ = ApplyFreshCloudSelectionAsync();
        }

        public void ShowLoginSuccess()
        {
            OpenLoginResult(success: true);
        }

        public void ShowLoginFailed()
        {
            OpenLoginResult(success: false);
        }

        private void ShowLoginFailed(string message)
        {
            OpenLoginResult(success: false, failureMessage: message);
        }

        private void ShowConnectionFailed()
        {
            OpenLoginResult(success: false, connectionFailure: true);
        }

        private void ShowSyncFailed(string message)
        {
            OpenLoginResult(success: false, failureMessage: message, syncFailure: true);
        }

        private void ShowProgressSyncFailure(FirebaseSyncFailureKind failureKind)
        {
            if (failureKind == FirebaseSyncFailureKind.Offline
                || failureKind == FirebaseSyncFailureKind.Timeout)
            {
                ShowConnectionFailed();
                return;
            }

            string message;
            switch (failureKind)
            {
                case FirebaseSyncFailureKind.Authentication:
                    message = "Your Facebook session expired.\nPlease sign in again.";
                    break;

                case FirebaseSyncFailureKind.PermissionDenied:
                    message = "Cloud sync is temporarily unavailable.\nPlease try again.";
                    break;

                case FirebaseSyncFailureKind.SessionChanged:
                    message = "The Facebook account changed.\nPlease start sync again.";
                    break;

                case FirebaseSyncFailureKind.InvalidState:
                    message = "Your sync session expired.\nPlease start sync again.";
                    break;

                default:
                    message = "We couldn't sync your progress.\nPlease retry.";
                    break;
            }

            ShowSyncFailed(message);
        }

        private static ProgressSyncRetryStep GetRetryStep(
            FirebaseSyncFailureKind failureKind,
            ProgressSyncRetryStep retryStep)
        {
            return failureKind == FirebaseSyncFailureKind.Authentication
                || failureKind == FirebaseSyncFailureKind.SessionChanged
                || failureKind == FirebaseSyncFailureKind.InvalidState
                ? ProgressSyncRetryStep.SignIn
                : retryStep;
        }

        public void CloseDialog()
        {
            CloseDialogImmediate();
        }

        public void Close()
        {
            UIController.HidePage(this);
        }

        private void EnsureReferences()
        {
            if (rootCanvasGroup == null)
                rootCanvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

            if (popupRoot == null)
                popupRoot = transform.Find("Dim Overlay/Profile Panel") as RectTransform;

            if (dimOverlayImage == null)
            {
                Transform overlayTransform = transform.Find("Dim Overlay");
                if (overlayTransform != null)
                    dimOverlayImage = overlayTransform.GetComponent<Image>();
            }

            if (saveButtonImage == null && saveButton != null)
                saveButtonImage = saveButton.targetGraphic as Image ?? saveButton.GetComponent<Image>();

            if (facebookLoginButtonText == null && facebookLoginButton != null)
                facebookLoginButtonText = facebookLoginButton.GetComponentInChildren<TextMeshProUGUI>(true);

            if (saveButtonInactiveSprite == null && saveButtonImage != null)
                saveButtonInactiveSprite = saveButtonImage.sprite;

            if (saveButtonActiveSprite == null)
                saveButtonActiveSprite = ResolveGreenSaveSprite();

            if (loginResultDialog == null && dialogRoot != null)
                loginResultDialog = dialogRoot.GetComponentInChildren<LoginResultDialogView>(true);

            if (saveProgressFoundDialog == null && dialogRoot != null)
                saveProgressFoundDialog = dialogRoot.GetComponentInChildren<SaveProgressFoundDialogView>(true);

            if (saveProgressFoundDialog == null && dialogRoot != null && !missingSaveProgressDialogLogged)
            {
                missingSaveProgressDialogLogged = true;
                Debug.LogError("[Profile] SaveProgressFoundDialogView reference is missing. Assign the edited Save Progress Found prefab under Dialog Root.");
            }

            ConfigureFullScreenOverlay();
        }

        private void RegisterButtons()
        {
            AddClick(closeButton, Close);
            AddClick(saveButton, OnSaveClicked);
            AddClick(saveProgressButton, OnSaveProgressClicked);
            AddClick(avatarTabButton, ShowAvatarTab);
            AddClick(frameTabButton, ShowFrameTab);
            AddClick(saveProgressCloseButton, CloseDialog);
            AddClick(facebookLoginButton, OnFacebookLoginClicked);
            AddClick(mockSuccessButton, ShowLoginSuccess);
            AddClick(mockFailedButton, ShowLoginFailed);
            AddClick(editNameButton, EnableNameEditing);
            loginResultDialog?.Init(CloseDialog, OnProgressSyncRetryClicked);
            saveProgressFoundDialog?.Init(UseLocalProgress, UseCloudProgress);
            UIAudioFeedback.RegisterButtons(transform);
            UIHapticFeedback.RegisterButtons(transform);

            if (nameInputField != null)
            {
                nameInputField.onValueChanged.RemoveListener(OnNameInputChanged);
                nameInputField.onValueChanged.AddListener(OnNameInputChanged);
            }
        }

        private void LoadSavedProfile()
        {
            if (!SaveController.IsSaveLoaded)
            {
                SetProfileDirty(false);
                return;
            }

            profileSave = FirebaseProfileHandler.GetLocalProfile();
            selectedAvatarIndex = ClampIndex(profileSave.AvatarIndex, avatarSprites);
            selectedFrameIndex = ClampIndex(profileSave.FrameIndex, frameSprites);
            SetDisplayedName(profileSave.PlayerName);
            CommitSavedProfileState();
        }

        private void InitItems()
        {
            if (avatarItems != null)
            {
                for (int i = 0; i < avatarItems.Length; i++)
                {
                    Sprite avatar = avatarSprites != null && i < avatarSprites.Length ? avatarSprites[i] : null;
                    Sprite frame = frameSprites != null && selectedFrameIndex < frameSprites.Length ? frameSprites[selectedFrameIndex] : null;
                    avatarItems[i].Init(i, avatar, frame, SelectAvatar);
                }
            }

            if (frameItems != null)
            {
                for (int i = 0; i < frameItems.Length; i++)
                {
                    Sprite frame = frameSprites != null && i < frameSprites.Length ? frameSprites[i] : null;
                    frameItems[i].Init(i, null, frame, SelectFrame);
                }
            }
        }

        private void ApplySelection()
        {
            if (avatarSprites != null && selectedAvatarIndex >= 0 && selectedAvatarIndex < avatarSprites.Length && previewAvatarImage != null)
                previewAvatarImage.sprite = avatarSprites[selectedAvatarIndex];

            if (frameSprites != null && selectedFrameIndex >= 0 && selectedFrameIndex < frameSprites.Length)
            {
                if (previewFrameImage != null)
                    previewFrameImage.sprite = frameSprites[selectedFrameIndex];

                if (avatarItems != null)
                {
                    for (int i = 0; i < avatarItems.Length; i++)
                    {
                        if (avatarItems[i] != null && avatarItems[i].FrameImage != null)
                        {
                            avatarItems[i].FrameImage.sprite = frameSprites[selectedFrameIndex];
                        }
                    }
                }
            }

            RefreshPreviewAvatarFrameLayout();

            if (avatarItems != null)
            {
                for (int i = 0; i < avatarItems.Length; i++)
                    avatarItems[i]?.SetSelected(i == selectedAvatarIndex);
            }

            if (frameItems != null)
            {
                for (int i = 0; i < frameItems.Length; i++)
                    frameItems[i]?.SetSelected(i == selectedFrameIndex);
            }
        }

        private void RefreshPreviewAvatarFrameLayout()
        {
            if (previewFrameImage != null)
            {
                previewFrameImage.enabled = previewFrameImage.sprite != null;
            }

            if (previewAvatarImage == null || previewAvatarImage.sprite == null)
                return;

            previewAvatarImage.enabled = true;
        }

        private bool SaveProfileLocally()
        {
            if (!SaveController.IsSaveLoaded)
                return false;

            if (profileSave == null)
                profileSave = FirebaseProfileHandler.GetLocalProfile();

            string playerName = nameInputField != null ? nameInputField.text : null;
            if (string.IsNullOrWhiteSpace(playerName) && nameText != null)
                playerName = nameText.text;
            if (string.IsNullOrWhiteSpace(playerName))
                playerName = FirebaseProfileHandler.GetDefaultPlayerName();

            if (!FirebasePlayerNameRegistry.TryNormalize(playerName, out string normalizedDisplayName, out _))
            {
                ShowProfileSaveError(FirebasePlayerNameRegistry.GetValidationError(playerName));
                return false;
            }

            playerName = normalizedDisplayName;
            profileSave.PlayerName = playerName;
            profileSave.AvatarIndex = selectedAvatarIndex;
            profileSave.FrameIndex = selectedFrameIndex;
            SetDisplayedName(playerName);
            SaveController.MarkAsSaveIsRequired();
            return true;
        }

        private async System.Threading.Tasks.Task SyncProfileOnlineAsync()
        {
            await System.Threading.Tasks.Task.CompletedTask;
        }

        private async System.Threading.Tasks.Task<bool> EnsureFirebaseConnectionAsync(string stage)
        {
            System.Threading.Tasks.TaskCompletionSource<NetworkCheckResult> completionSource =
                new System.Threading.Tasks.TaskCompletionSource<NetworkCheckResult>();
            StartCoroutine(CheckFirebaseConnectionCoroutine(completionSource));

            NetworkCheckResult result = await completionSource.Task;
            bool isReachable = result != null && result.IsReachable;
            Debug.Log("[Profile] Firebase connection probe: stage=" + stage
                + ", reachable=" + isReachable
                + ", transport=" + (result != null ? result.Transport.ToString() : "Unknown")
                + ", responseCode=" + (result != null ? result.ResponseCode.ToString() : "0")
                + ", timedOut=" + (result != null && result.TimedOut)
                + ", error=" + (result != null ? result.Error : "No result") + ".");
            return isReachable;
        }

        private IEnumerator CheckFirebaseConnectionCoroutine(
            System.Threading.Tasks.TaskCompletionSource<NetworkCheckResult> completionSource)
        {
            NetworkCheckResult result = null;
            NetworkConnection connection = new NetworkConnection(NetworkConnection.ServiceProbeUrl);
            yield return connection.CheckConnectionDetailed(state => result = state);
            completionSource.TrySetResult(result);
        }

        private async System.Threading.Tasks.Task SignInAndSyncProgressAsync()
        {
            if (!SaveController.IsSaveLoaded)
            {
                Debug.LogError("[Profile] Facebook progress sync cannot start because the local save is not loaded.");
                ShowLoginFailed();
                return;
            }

            progressSyncInProgress = true;
            SetProgressButtonsInteractable(false);

            if (!await EnsureFirebaseConnectionAsync("Facebook sign-in"))
            {
                if (this == null)
                    return;

                progressSyncInProgress = false;
                SetProgressButtonsInteractable(true);
                progressSyncRetryStep = ProgressSyncRetryStep.SignIn;
                RefreshFacebookButtonState();
                ShowConnectionFailed();
                return;
            }

            CoinSafeProgress.BeginFacebookAuthTransition();
            CoinSafeProgress.FlushLocalSave();

            FirebaseFacebookSignInResult signInResult = null;
            bool signedIn = false;
            try
            {
                signInResult = await FirebaseAuthHandler.SignInWithFacebookAsync();
                signedIn = signInResult != null
                    && signInResult.IsSignedIn
                    && !string.IsNullOrEmpty(signInResult.UserId);
                if (signedIn)
                    CoinSafeProgress.BeginFacebookResolution(signInResult.UserId);
            }
            catch (System.Exception exception)
            {
                Debug.LogError("[Profile] Unexpected Facebook sign-in error: " + exception.Message);
            }
            finally
            {
                CoinSafeProgress.EndFacebookAuthTransition();
            }

            if (this == null)
                return;

            if (!signedIn)
            {
                Debug.LogError("[Profile] Facebook sign-in failed. Status="
                    + (signInResult != null ? signInResult.Status.ToString() : "null")
                    + ", error=" + (signInResult != null ? signInResult.Error : "No result returned") + ".");
                progressSyncInProgress = false;
                SetProgressButtonsInteractable(true);
                progressSyncRetryStep = ProgressSyncRetryStep.SignIn;
                RefreshFacebookButtonState();
                ShowLoginFailed();
                return;
            }

            pendingFacebookSignInResult = signInResult;
            RefreshFacebookButtonState();

            await DownloadAndResolveProgressAsync(signInResult);
        }

        private async System.Threading.Tasks.Task RetryPendingProgressDownloadAsync()
        {
            string uid = CoinSafeProgress.PendingFacebookUid;
            string authenticatedUid = !FirebaseAuthHandler.IsCurrentUserAnonymous
                ? FirebaseAuthHandler.CurrentUserId
                : null;

            if (!string.IsNullOrEmpty(authenticatedUid)
                && !string.Equals(authenticatedUid, uid, System.StringComparison.Ordinal))
            {
                CoinSafeProgress.BeginFacebookResolution(authenticatedUid);
                FirebaseFacebookSignInResult currentAccountResult = new FirebaseFacebookSignInResult
                {
                    Status = FirebaseFacebookSignInStatus.Success,
                    UserId = authenticatedUid
                };
                pendingFacebookSignInResult = currentAccountResult;
                progressSyncInProgress = true;
                SetProgressButtonsInteractable(false);
                await DownloadAndResolveProgressAsync(currentAccountResult);
                return;
            }

            if (string.IsNullOrEmpty(uid) || !FirebaseAuthHandler.IsCurrentFacebookUser(uid))
            {
                pendingFacebookSignInResult = null;
                progressSyncRetryStep = ProgressSyncRetryStep.SignIn;
                await SignInAndSyncProgressAsync();
                return;
            }

            progressSyncInProgress = true;
            SetProgressButtonsInteractable(false);

            FirebaseFacebookSignInResult signInResult = pendingFacebookSignInResult;
            if (signInResult == null || !string.Equals(signInResult.UserId, uid, System.StringComparison.Ordinal))
            {
                signInResult = new FirebaseFacebookSignInResult
                {
                    Status = FirebaseFacebookSignInStatus.Success,
                    UserId = uid
                };
                pendingFacebookSignInResult = signInResult;
            }

            await DownloadAndResolveProgressAsync(signInResult);
        }

        private async System.Threading.Tasks.Task DownloadAndResolveProgressAsync(FirebaseFacebookSignInResult signInResult)
        {
            if (signInResult == null || string.IsNullOrEmpty(signInResult.UserId))
            {
                progressSyncInProgress = false;
                SetProgressButtonsInteractable(true);
                progressSyncRetryStep = ProgressSyncRetryStep.SignIn;
                ShowLoginFailed();
                return;
            }

            if (!await EnsureFirebaseConnectionAsync("cloud progress download"))
            {
                if (this == null)
                    return;

                progressSyncInProgress = false;
                SetProgressButtonsInteractable(true);
                progressSyncRetryStep = ProgressSyncRetryStep.Download;
                RefreshFacebookButtonState();
                ShowConnectionFailed();
                return;
            }

            Debug.Log("[Profile] Facebook sign-in succeeded. Downloading cloud progress for UID="
                + signInResult.UserId + ".");
            FirebaseProgressDownloadResult downloadResult = await FirebaseProfileHandler.DownloadProgressAsync();
            if (this == null)
                return;

            if (downloadResult == null || downloadResult.Status == FirebaseProgressDownloadStatus.Failed)
            {
                FirebaseSyncFailureKind failureKind = downloadResult != null
                    ? downloadResult.FailureKind
                    : FirebaseSyncFailureKind.Unknown;
                Debug.LogError("[Profile] Facebook authentication succeeded, but downloading progress failed. Error="
                    + (downloadResult != null ? downloadResult.Error : "No result returned") + ".");
                progressSyncInProgress = false;
                SetProgressButtonsInteractable(true);
                progressSyncRetryStep = GetRetryStep(failureKind, ProgressSyncRetryStep.Download);
                RefreshFacebookButtonState();
                ShowProgressSyncFailure(failureKind);
                return;
            }

            FirebasePlayerProgress cloudProgress = downloadResult.Progress;
            if (cloudProgress == null
                || !string.Equals(cloudProgress.Uid, signInResult.UserId, System.StringComparison.Ordinal)
                || !FirebaseAuthHandler.IsCurrentFacebookUser(signInResult.UserId))
            {
                progressSyncInProgress = false;
                SetProgressButtonsInteractable(true);
                progressSyncRetryStep = GetRetryStep(
                    FirebaseSyncFailureKind.SessionChanged,
                    ProgressSyncRetryStep.Download);
                ShowProgressSyncFailure(FirebaseSyncFailureKind.SessionChanged);
                return;
            }

            Debug.Log("[Profile] Cloud download completed. Opening the mandatory Local/Cloud version selection.");
            pendingCloudProgress = cloudProgress;
            progressSyncRetryStep = ProgressSyncRetryStep.None;
            progressSyncInProgress = false;
            SetProgressButtonsInteractable(true);
            OpenSaveProgressFoundDialog(cloudProgress);
        }

        private async System.Threading.Tasks.Task SignOutFacebookSyncAsync()
        {
            progressSyncInProgress = true;
            SetProgressButtonsInteractable(false);

            bool success = await FirebaseAuthHandler.SignOutFacebookSyncAsync();
            if (this == null)
                return;

            progressSyncInProgress = false;
            SetProgressButtonsInteractable(true);
            RefreshFacebookButtonState();

            if (success)
            {
                Debug.Log("[Profile] Facebook progress sync signed out successfully.");
                ClearProgressSyncRetryState();
                return;
            }

            Debug.LogError("[Profile] Facebook progress sync sign-out did not complete cleanly.");
            progressSyncRetryStep = ProgressSyncRetryStep.SignOut;
            ShowLoginFailed("Couldn't sign out completely.\nPlease try again.");
        }

        private void RefreshFacebookButtonState()
        {
            if (facebookLoginButtonText == null && facebookLoginButton != null)
                facebookLoginButtonText = facebookLoginButton.GetComponentInChildren<TextMeshProUGUI>(true);

            if (facebookLoginButtonText != null)
            {
                facebookLoginButtonText.text = "Cloud sync unavailable";
            }
        }

        private async System.Threading.Tasks.Task CommitLocalSelectionAsync()
        {
            string uid = pendingCloudProgress != null
                ? pendingCloudProgress.Uid
                : CoinSafeProgress.PendingFacebookUid;
            if (string.IsNullOrEmpty(uid))
            {
                progressSyncRetryStep = GetRetryStep(
                    FirebaseSyncFailureKind.InvalidState,
                    ProgressSyncRetryStep.Download);
                ShowProgressSyncFailure(FirebaseSyncFailureKind.InvalidState);
                return;
            }

            if (!FirebaseAuthHandler.IsCurrentFacebookUser(uid))
            {
                progressSyncRetryStep = GetRetryStep(
                    FirebaseSyncFailureKind.SessionChanged,
                    ProgressSyncRetryStep.Download);
                ShowProgressSyncFailure(FirebaseSyncFailureKind.SessionChanged);
                return;
            }

            progressSyncInProgress = true;
            SetProgressButtonsInteractable(false);

            if (!await EnsureFirebaseConnectionAsync("uploading local progress"))
            {
                if (this == null)
                    return;

                progressSyncInProgress = false;
                SetProgressButtonsInteractable(true);
                progressSyncRetryStep = ProgressSyncRetryStep.CommitLocalSelection;
                ShowConnectionFailed();
                return;
            }

            FirebaseProgressOperationResult uploadResult = await FirebaseProfileHandler.UploadFullLocalProgressAsync(uid);
            if (this == null)
                return;

            if (uploadResult != null && uploadResult.Succeeded)
            {
                CoinSafeProgress.ResolveFacebookWithLocal(uid, requestCloudSync: false);
                CoinSafeSyncSnapshot coinSafeSnapshot;
                if (CoinSafeProgress.TryGetActiveFacebookSnapshot(out coinSafeSnapshot))
                    CoinSafeProgress.MarkFacebookSnapshotSynced(coinSafeSnapshot.Uid, coinSafeSnapshot.Revision);

                CoinSafeProgress.FlushLocalSave();
                Debug.Log("[Profile] Full local progress commit completed successfully.");
                progressSyncInProgress = false;
                SetProgressButtonsInteractable(true);
                RefreshFacebookButtonState();
                ClearProgressSyncRetryState();
                ShowSyncSuccessAndReload();
                return;
            }

            progressSyncInProgress = false;
            SetProgressButtonsInteractable(true);
            FirebaseSyncFailureKind failureKind = uploadResult != null
                ? uploadResult.FailureKind
                : FirebaseSyncFailureKind.Unknown;
            if (uploadResult != null
                && !string.IsNullOrWhiteSpace(uploadResult.Error)
                && uploadResult.Error.IndexOf("already in use", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                ShowSyncFailed("This player name is already in use.\nPlease choose a different name.");
                return;
            }

            progressSyncRetryStep = GetRetryStep(
                failureKind,
                ProgressSyncRetryStep.CommitLocalSelection);
            ShowProgressSyncFailure(failureKind);
        }

        private async System.Threading.Tasks.Task ApplyFreshCloudSelectionAsync()
        {
            progressSyncInProgress = true;
            SetProgressButtonsInteractable(false);

            string expectedUid = pendingCloudProgress != null ? pendingCloudProgress.Uid : null;
            if (string.IsNullOrEmpty(expectedUid)
                || !FirebaseAuthHandler.IsCurrentFacebookUser(expectedUid))
            {
                progressSyncInProgress = false;
                SetProgressButtonsInteractable(true);
                progressSyncRetryStep = GetRetryStep(
                    FirebaseSyncFailureKind.SessionChanged,
                    ProgressSyncRetryStep.Download);
                ShowProgressSyncFailure(FirebaseSyncFailureKind.SessionChanged);
                return;
            }

            if (!await EnsureFirebaseConnectionAsync("downloading selected cloud progress"))
            {
                if (this == null)
                    return;

                progressSyncInProgress = false;
                SetProgressButtonsInteractable(true);
                progressSyncRetryStep = ProgressSyncRetryStep.ApplyFreshCloudSelection;
                ShowConnectionFailed();
                return;
            }

            FirebaseProgressDownloadResult downloadResult = await FirebaseProfileHandler.DownloadProgressAsync();
            if (this == null)
                return;

            if (downloadResult == null || downloadResult.Status == FirebaseProgressDownloadStatus.Failed)
            {
                FirebaseSyncFailureKind failureKind = downloadResult != null
                    ? downloadResult.FailureKind
                    : FirebaseSyncFailureKind.Unknown;
                progressSyncInProgress = false;
                SetProgressButtonsInteractable(true);
                progressSyncRetryStep = GetRetryStep(
                    failureKind,
                    ProgressSyncRetryStep.ApplyFreshCloudSelection);
                ShowProgressSyncFailure(failureKind);
                return;
            }

            if (downloadResult.Status != FirebaseProgressDownloadStatus.Found
                || downloadResult.Progress == null)
            {
                progressSyncInProgress = false;
                SetProgressButtonsInteractable(true);
                progressSyncRetryStep = ProgressSyncRetryStep.ApplyFreshCloudSelection;
                ShowSyncFailed("Cloud progress wasn't found.\nYour device progress is safe.");
                return;
            }

            FirebasePlayerProgress freshCloudProgress = downloadResult.Progress;
            if (!string.Equals(freshCloudProgress.Uid, expectedUid, System.StringComparison.Ordinal)
                || !FirebaseAuthHandler.IsCurrentFacebookUser(expectedUid))
            {
                progressSyncInProgress = false;
                SetProgressButtonsInteractable(true);
                progressSyncRetryStep = GetRetryStep(
                    FirebaseSyncFailureKind.SessionChanged,
                    ProgressSyncRetryStep.Download);
                ShowProgressSyncFailure(FirebaseSyncFailureKind.SessionChanged);
                return;
            }

            if (!FirebaseProfileHandler.ApplyCloudProgressToLocalSave(freshCloudProgress))
            {
                progressSyncInProgress = false;
                SetProgressButtonsInteractable(true);
                progressSyncRetryStep = GetRetryStep(
                    FirebaseSyncFailureKind.SessionChanged,
                    ProgressSyncRetryStep.ApplyFreshCloudSelection);
                ShowProgressSyncFailure(FirebaseSyncFailureKind.SessionChanged);
                return;
            }

            CoinSafeProgress.FlushLocalSave();
            ClearProgressSyncRetryState();
            progressSyncInProgress = false;
            SetProgressButtonsInteractable(true);
            LoadSavedProfile();
            ApplySelection();
            CommitSavedProfileState();
            SetProfileDirty(false);
            NotifyProfileSaved();
            Debug.Log("[Profile] Cloud progress selected. Showing sync success before reloading through Init loading scene.");
            ShowSyncSuccessAndReload();
        }

        private void ClearProgressSyncRetryState()
        {
            pendingCloudProgress = null;
            pendingFacebookSignInResult = null;
            progressSyncRetryStep = ProgressSyncRetryStep.None;
        }

        private void ReloadThroughLoadingScene()
        {
            syncSuccessReloadRoutine = null;
            CloseDialogImmediate();
            GameLoading.LoadingSceneBuildIndex = -1;
            SceneManager.LoadScene(GameConsts.SCENE_INIT);
        }

        private void ShowSyncSuccessAndReload()
        {
            if (dialogRoot == null || dialogCanvasGroup == null || loginResultDialog == null)
            {
                ReloadThroughLoadingScene();
                return;
            }

            OpenLoginResult(success: true);
            loginResultDialog.ShowSyncSuccess();

            if (syncSuccessReloadRoutine != null)
                StopCoroutine(syncSuccessReloadRoutine);

            syncSuccessReloadRoutine = StartCoroutine(ReloadAfterSyncSuccess());
        }

        private IEnumerator ReloadAfterSyncSuccess()
        {
            yield return new WaitForSecondsRealtime(SyncSuccessVisibleTime);
            ReloadThroughLoadingScene();
        }

        private void SetProgressButtonsInteractable(bool interactable)
        {
            if (facebookLoginButton != null)
                facebookLoginButton.interactable = interactable;

            loginResultDialog?.SetRetryInteractable(interactable);
            saveProgressFoundDialog?.SetInteractable(interactable);
        }

        private void SetDisplayedName(string playerName)
        {
            suppressDirtyNotifications = true;

            if (nameInputField != null)
                nameInputField.text = playerName;

            if (nameText != null)
                nameText.text = playerName;

            suppressDirtyNotifications = false;
        }

        private static int ClampIndex(int index, Sprite[] sprites)
        {
            if (sprites == null || sprites.Length == 0)
                return Mathf.Max(0, index);

            return Mathf.Clamp(index, 0, sprites.Length - 1);
        }

        private void SetTabState(bool avatarActive)
        {
            if (avatarContent != null)
                avatarContent.SetActive(avatarActive);

            if (frameContent != null)
                frameContent.SetActive(!avatarActive);

            if (avatarTabBackground != null)
                avatarTabBackground.sprite = avatarActive ? tabActiveSprite : tabInactiveSprite;

            if (frameTabBackground != null)
                frameTabBackground.sprite = avatarActive ? tabInactiveSprite : tabActiveSprite;
        }

        private void OpenDialog(GameObject targetDialog)
        {
            if (dialogRoot == null || dialogCanvasGroup == null || targetDialog == null)
                return;

            if (targetDialog == saveProgressDialog)
                RefreshFacebookButtonState();

            saveProgressDialog.SetActive(targetDialog == saveProgressDialog);
            saveProgressFoundDialog?.HideImmediate();
            loginResultDialog?.HideImmediate();

            ShowDialogRoot();
        }

        private void OpenSaveProgressFoundDialog(FirebasePlayerProgress cloudProgress)
        {
            if (dialogRoot == null || dialogCanvasGroup == null || saveProgressFoundDialog == null || cloudProgress == null)
                return;

            if (saveProgressDialog != null)
                saveProgressDialog.SetActive(false);

            loginResultDialog?.HideImmediate();
            PlayerProgressSnapshot localSnapshot = FirebaseProfileHandler.GetLocalProgressSnapshot();
            PlayerProgressSnapshot cloudSnapshot = cloudProgress.ToSnapshot();

            saveProgressFoundDialog.Show(localSnapshot, cloudSnapshot);
            ShowDialogRoot();
        }

        private void OpenLoginResult(
            bool success,
            string failureMessage = null,
            bool connectionFailure = false,
            bool syncFailure = false)
        {
            if (dialogRoot == null || dialogCanvasGroup == null || loginResultDialog == null)
                return;

            if (saveProgressDialog != null)
                saveProgressDialog.SetActive(false);

            saveProgressFoundDialog?.HideImmediate();

            if (success)
                loginResultDialog.ShowSuccess();
            else if (connectionFailure)
                loginResultDialog.ShowConnectionFailed();
            else if (syncFailure)
                loginResultDialog.ShowSyncFailed(failureMessage);
            else
                loginResultDialog.ShowFailed(failureMessage);

            ShowDialogRoot();
        }

        private void ShowDialogRoot()
        {
            dialogRoot.gameObject.SetActive(true);
            dialogCanvasGroup.alpha = 0f;
            dialogRoot.localScale = Vector3.one * 0.92f;
            dialogCanvasGroup.DOFade(1f, ShowDuration, unscaledTime: true);
            dialogRoot.DOScale(Vector3.one, ShowDuration, unscaledTime: true).SetEasing(Ease.Type.BackOut);
        }

        private void CloseDialogImmediate()
        {
            if (dialogRoot != null)
                dialogRoot.gameObject.SetActive(false);

            if (saveProgressDialog != null)
                saveProgressDialog.SetActive(false);

            loginResultDialog?.HideImmediate();
            saveProgressFoundDialog?.HideImmediate();

            if (dialogCanvasGroup != null)
                dialogCanvasGroup.alpha = 0f;
        }

        private void ShowToast()
        {
            HideToastImmediate();

            if (toastRoot == null || toastCanvasGroup == null)
                return;

            toastRoot.gameObject.SetActive(true);
            toastCanvasGroup.alpha = 0f;
            toastRoot.anchoredPosition = new Vector2(0f, 40f);
            toastCanvasGroup.DOFade(1f, 0.16f, unscaledTime: true);
            toastTweenCase = toastRoot
                .DOAnchoredPosition(Vector2.zero, 0.16f, unscaledTime: true)
                .SetEasing(Ease.Type.CubicOut);

            toastRoutine = StartCoroutine(HideToastDelayed());
        }

        private static void ShowProfileSaveError(string message)
        {
            string resolvedMessage = string.IsNullOrWhiteSpace(message)
                ? "Could not save name. Please try again."
                : message;
            Debug.LogWarning("[Profile] " + resolvedMessage);
            SystemMessage.ShowMessage(resolvedMessage);
        }

        private IEnumerator HideToastDelayed()
        {
            yield return new WaitForSecondsRealtime(ToastVisibleTime);

            if (toastCanvasGroup != null)
                toastCanvasGroup.DOFade(0f, 0.18f, unscaledTime: true);

            if (toastRoot != null)
            {
                toastTweenCase = toastRoot
                    .DOAnchoredPosition(new Vector2(0f, 40f), 0.18f, unscaledTime: true)
                    .SetEasing(Ease.Type.CubicIn)
                    .OnComplete(HideToastImmediate);
            }
        }

        private void HideToastImmediate()
        {
            toastTweenCase.KillActive();

            if (toastRoutine != null)
            {
                StopCoroutine(toastRoutine);
                toastRoutine = null;
            }

            if (toastRoot != null)
                toastRoot.gameObject.SetActive(false);

            if (toastCanvasGroup != null)
                toastCanvasGroup.alpha = 0f;
        }

        private void EnableNameEditing()
        {
            if (nameInputField == null)
                return;

            nameInputField.interactable = true;
            nameInputField.Select();
            nameInputField.ActivateInputField();
            SetProfileDirty(true);
        }

        private void OnNameInputChanged(string value)
        {
            if (suppressDirtyNotifications)
                return;

            SetProfileDirty(HasProfileChanges());
        }

        private void CommitSavedProfileState()
        {
            savedAvatarIndex = selectedAvatarIndex;
            savedFrameIndex = selectedFrameIndex;
            savedPlayerName = GetCurrentDisplayedName();
        }

        private void NotifyProfileSaved()
        {
            ProfileSaved?.Invoke(
                savedAvatarIndex,
                savedFrameIndex,
                GetAvatarSprite(savedAvatarIndex),
                GetFrameSprite(savedFrameIndex),
                savedPlayerName);
        }

        private void DiscardPendingProfileChanges()
        {
            if (!hasPendingProfileChanges)
                return;

            selectedAvatarIndex = savedAvatarIndex;
            selectedFrameIndex = savedFrameIndex;
            SetDisplayedName(savedPlayerName);
            ApplySelection();
            SetProfileDirty(false);
        }

        private bool HasProfileChanges()
        {
            return selectedAvatarIndex != savedAvatarIndex
                || selectedFrameIndex != savedFrameIndex
                || !string.Equals(GetCurrentDisplayedName(), savedPlayerName, System.StringComparison.Ordinal);
        }

        private string GetCurrentDisplayedName()
        {
            string playerName = nameInputField != null ? nameInputField.text : null;
            if (string.IsNullOrWhiteSpace(playerName) && nameText != null)
                playerName = nameText.text;
            if (string.IsNullOrWhiteSpace(playerName))
                playerName = FirebaseProfileHandler.GetDefaultPlayerName();

            return playerName.Trim();
        }

        private void SetProfileDirty(bool dirty)
        {
            hasPendingProfileChanges = dirty;

            if (saveButton != null)
                saveButton.interactable = dirty;

            if (saveButtonImage != null)
            {
                Sprite targetSprite = dirty ? saveButtonActiveSprite : saveButtonInactiveSprite;
                if (targetSprite != null)
                    saveButtonImage.sprite = targetSprite;

                saveButtonImage.color = Color.white;
            }
        }

        private Sprite ResolveGreenSaveSprite()
        {
            Sprite sprite = GetButtonSprite(facebookLoginButton);
            if (sprite != null)
                return sprite;

            return loginResultDialog != null ? loginResultDialog.RetryButtonSprite : null;
        }

        private static Sprite GetButtonSprite(Button button)
        {
            if (button == null)
                return null;

            Image image = button.targetGraphic as Image ?? button.GetComponent<Image>();
            return image != null ? image.sprite : null;
        }

        private SaveProgressFoundDialogView BuildSaveProgressFoundDialogRuntime(RectTransform parent)
        {
            GameObject holder = new GameObject("Save Progress Found Dialog", typeof(RectTransform), typeof(SaveProgressFoundDialogView));
            holder.transform.SetParent(parent, false);
            StretchToParent(holder.GetComponent<RectTransform>());

            Sprite panelSprite = loginResultDialog != null ? loginResultDialog.PanelSprite : null;
            Sprite titleSprite = GetSpriteFromPath("Dim Overlay/Profile Panel/Title Ribbon");
            Sprite buttonSprite = ResolveGreenSaveSprite();
            Sprite coinSprite = ResolveCoinSprite();
            TextMeshProUGUI templateText = GetComponentInChildren<TextMeshProUGUI>(true);

            GameObject panel = CreateDialogImage("Panel", holder.transform, panelSprite, Image.Type.Sliced);
            CenterRect(panel.GetComponent<RectTransform>(), Vector2.zero, new Vector2(760f, 1120f));

            GameObject titleRibbon = CreateDialogImage("Title Ribbon", panel.transform, titleSprite, Image.Type.Sliced);
            CenterRect(titleRibbon.GetComponent<RectTransform>(), new Vector2(0f, 540f), new Vector2(700f, 126f));
            CreateDialogText("Title", titleRibbon.transform, "SAVE PROGRESS FOUND", 50f, Color.white, Vector2.zero, new Vector2(660f, 96f), templateText);

            CreateDialogText(
                "Message",
                panel.transform,
                "We found another saved\nversion of your game.\nChoose one:",
                52f,
                DialogTextColor(),
                new Vector2(0f, 355f),
                new Vector2(660f, 250f),
                templateText);

            SaveProgressCardRefs device = CreateSaveProgressCard(panel.transform, "Device Card", "Local Version", new Vector2(-185f, -180f), true, panelSprite, buttonSprite, coinSprite, templateText);
            SaveProgressCardRefs server = CreateSaveProgressCard(panel.transform, "Server Card", "Cloud Version", new Vector2(185f, -180f), false, panelSprite, buttonSprite, coinSprite, templateText);

            SaveProgressFoundDialogView view = holder.GetComponent<SaveProgressFoundDialogView>();
            view.ConfigureReferences(
                device.LevelText,
                device.CoinText,
                device.TimestampText,
                device.SelectButton,
                server.LevelText,
                server.CoinText,
                server.TimestampText,
                server.SelectButton);
            view.ConfigureCoinSafeReferences(device.CoinSafeAmountText, server.CoinSafeAmountText);

            holder.SetActive(false);
            return view;
        }

        private SaveProgressCardRefs CreateSaveProgressCard(
            Transform parent,
            string name,
            string title,
            Vector2 position,
            bool isDevice,
            Sprite cardSprite,
            Sprite buttonSprite,
            Sprite coinSprite,
            TextMeshProUGUI templateText)
        {
            GameObject card = CreateDialogImage(name, parent, cardSprite, Image.Type.Sliced);
            CenterRect(card.GetComponent<RectTransform>(), position, new Vector2(330f, 730f));

            CreateDialogText("Card Title", card.transform, title, 42f, isDevice ? new Color32(24, 137, 36, 255) : DialogTextColor(), new Vector2(0f, 300f), new Vector2(280f, 72f), templateText);
            CreateDialogDivider(card.transform, new Vector2(0f, 252f), new Vector2(235f, 3f));
            CreateDialogText("Level Label", card.transform, "Level:", 44f, DialogTextColor(), new Vector2(0f, 188f), new Vector2(270f, 70f), templateText);
            TextMeshProUGUI levelText = CreateDialogText("Level Value", card.transform, "1", 70f, DialogTextColor(), new Vector2(0f, 112f), new Vector2(270f, 92f), templateText);
            CreateDialogText("Collection Label", card.transform, "Progress Box", 44f, DialogTextColor(), new Vector2(0f, 24f), new Vector2(285f, 70f), templateText);
            TextMeshProUGUI coinSafeAmountText = CreateDialogText("Collection Value", card.transform, "0", 70f, DialogTextColor(), new Vector2(0f, -56f), new Vector2(270f, 92f), templateText);

            GameObject coinPill = CreateDialogImage("Coin Pill", card.transform, null, Image.Type.Sliced);
            Image pillImage = coinPill.GetComponent<Image>();
            pillImage.color = new Color32(236, 246, 255, 255);
            CenterRect(coinPill.GetComponent<RectTransform>(), new Vector2(28f, -150f), new Vector2(190f, 58f));

            GameObject coinIcon = CreateDialogImage("Coin Icon", card.transform, coinSprite, Image.Type.Simple);
            CenterRect(coinIcon.GetComponent<RectTransform>(), new Vector2(-98f, -150f), new Vector2(74f, 74f));
            TextMeshProUGUI coinText = CreateDialogText("Coin Value", coinPill.transform, "0", 36f, DialogTextColor(), new Vector2(15f, 0f), new Vector2(140f, 54f), templateText);

            CreateDialogDivider(card.transform, new Vector2(0f, -215f), new Vector2(235f, 3f));
            TextMeshProUGUI timestampText = CreateDialogText("Timestamp", card.transform, isDevice ? "Uploaded Now" : "Last saved: --", 32f, DialogTextColor(), new Vector2(0f, -270f), new Vector2(285f, 100f), templateText);
            Button selectButton = CreateDialogButton("Select Button", card.transform, buttonSprite, "Select", new Vector2(0f, -335f), new Vector2(260f, 94f), templateText);

            return new SaveProgressCardRefs
            {
                LevelText = levelText,
                CoinText = coinText,
                CoinSafeAmountText = coinSafeAmountText,
                TimestampText = timestampText,
                SelectButton = selectButton
            };
        }

        private Button CreateDialogButton(string name, Transform parent, Sprite sprite, string label, Vector2 position, Vector2 size, TextMeshProUGUI templateText)
        {
            GameObject go = CreateDialogImage(name, parent, sprite, Image.Type.Sliced);
            CenterRect(go.GetComponent<RectTransform>(), position, size);

            Button button = go.AddComponent<Button>();
            button.targetGraphic = go.GetComponent<Image>();
            CreateDialogText("Label", go.transform, label, 52f, Color.white, Vector2.zero, size, templateText);
            return button;
        }

        private TextMeshProUGUI CreateDialogText(string name, Transform parent, string text, float fontSize, Color color, Vector2 position, Vector2 size, TextMeshProUGUI templateText)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            CenterRect(go.GetComponent<RectTransform>(), position, size);

            TextMeshProUGUI label = go.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.alignment = TextAlignmentOptions.Center;
            label.color = color;
            label.raycastTarget = false;

            if (templateText != null)
            {
                label.font = templateText.font;
                label.fontSharedMaterial = templateText.fontSharedMaterial;
                label.fontStyle = templateText.fontStyle;
            }

            return label;
        }

        private GameObject CreateDialogImage(string name, Transform parent, Sprite sprite, Image.Type imageType)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);

            Image image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.type = imageType;
            image.color = Color.white;
            image.raycastTarget = true;
            return go;
        }

        private void CreateDialogDivider(Transform parent, Vector2 position, Vector2 size)
        {
            GameObject divider = CreateDialogImage("Divider", parent, null, Image.Type.Simple);
            Image image = divider.GetComponent<Image>();
            image.color = new Color32(219, 221, 231, 255);
            image.raycastTarget = false;
            CenterRect(divider.GetComponent<RectTransform>(), position, size);
        }

        private Sprite GetSpriteFromPath(string path)
        {
            Transform target = transform.Find(path);
            Image image = target != null ? target.GetComponent<Image>() : null;
            return image != null ? image.sprite : null;
        }

        private Sprite ResolveCoinSprite()
        {
            try
            {
                Currency currency = CurrencyController.GetCurrency(CurrencyType.Coins);
                return currency != null ? currency.Icon : null;
            }
            catch (System.Exception)
            {
                return null;
            }
        }

        private static Color32 DialogTextColor()
        {
            return new Color32(83, 103, 158, 255);
        }

        private static void CenterRect(RectTransform rectTransform, Vector2 position, Vector2 size)
        {
            rectTransform.anchorMin = rectTransform.anchorMax = rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = position;
            rectTransform.sizeDelta = size;
            rectTransform.localScale = Vector3.one;
        }

        private void ConfigureFullScreenOverlay()
        {
            if (dimOverlayImage != null)
            {
                dimOverlayImage.color = dimOverlayColor;
                dimOverlayImage.raycastTarget = true;
                StretchToParent(dimOverlayImage.rectTransform);
            }

            RectTransform rectTransform = transform as RectTransform;
            if (rectTransform != null)
                StretchToParent(rectTransform);
        }

        private void BringPopupToFront()
        {
            transform.SetAsLastSibling();

            Canvas targetCanvas = Canvas != null ? Canvas : GetComponent<Canvas>();
            if (targetCanvas == null)
                return;

            targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            targetCanvas.overrideSorting = true;
            targetCanvas.sortingOrder = popupSortingOrder;
        }

        private static void StretchToParent(RectTransform rectTransform)
        {
            if (rectTransform == null)
                return;

            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.localScale = Vector3.one;
        }

        private static void SetProfilePopupVisible(bool visible)
        {
            if (IsProfilePopupVisible == visible)
                return;

            IsProfilePopupVisible = visible;
            ProfilePopupVisibilityChanged?.Invoke(visible);
        }

        private static void AddClick(Button button, UnityEngine.Events.UnityAction callback)
        {
            if (button == null)
                return;

            button.onClick.RemoveListener(callback);
            button.onClick.AddListener(callback);
        }

        private struct SaveProgressCardRefs
        {
            public TextMeshProUGUI LevelText;
            public TextMeshProUGUI CoinText;
            public TextMeshProUGUI CoinSafeAmountText;
            public TextMeshProUGUI TimestampText;
            public Button SelectButton;
        }
    }
}
