using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NebulaSoft
{
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
    public class LeaderboardPanelView : MonoBehaviour
    {
        private const int SortingOrder = 500;
        private const float HideDuration = 0.26f;
        private const float HideScreenMargin = 40f;
        private const float CardAnimationDuration = 0.22f;
        private const float RowAnimationDuration = 0.24f;
        private const float CardStagger = 0.045f;
        private const float RowStagger = 0.035f;
        private const int MaxAnimatedRows = 12;
        private const float AutoScrollUnitsPerSecond = 1200f;
        private const float MaxAutoScrollDuration = 2.4f;
        private const float DesignWidth = 828f;
        private const float DesignHeight = 1792f;
        private const float RowsInitialViewportHeight = 560f;
        private const int OfflineCountdownMaxDots = 3;

        [SerializeField] bool autoScrollToCurrentPlayer = true;
        [SerializeField] int leaguesCurrentPlayerFallbackRank = 8;
        [SerializeField] int globalCurrentPlayerFallbackRank = 96;
        [SerializeField] int onlineGlobalLimit = 100;
        [SerializeField] int onlineLeagueLimit = 30;
        [SerializeField] PlayerElement globalRowPrefab;
        [SerializeField] PlayerElement leagueRowPrefab;
        [SerializeField, Range(0f, 1f)] float currentPlayerViewportPosition = 0.5f;
        [SerializeField] float scrollToCurrentPlayerStartDelay = 0.28f;
        [SerializeField] float scrollToCurrentPlayerDuration = 1.25f;

        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private TweenCase showTweenCase;
        private TweenCase hideTweenCase;
        private Coroutine scrollToCurrentPlayerCoroutine;
        private Vector2 shownPosition;
        private bool initialized;
        private bool tabButtonsRegistered;
        private GameObject globalContent;
        private GameObject leaguesContent;
        private RectTransform structuredLayout;
        private LeaderboardTab selectedTab = LeaderboardTab.Leagues;
        private bool globalRefreshInProgress;
        private bool leagueRefreshInProgress;
        private bool onlineRefreshEnabled = true;
        private Coroutine countdownCoroutine;
        private TMP_Text globalCountdownText;
        private TMP_Text leagueCountdownText;
        private bool seasonReloadRequested;
        private bool connectivityRefreshInProgress;
        private int offlineCountdownDotIndex = -1;
        private readonly List<PlayerElement> globalRows = new List<PlayerElement>();
        private readonly List<PlayerElement> leagueRows = new List<PlayerElement>();
        private readonly List<TweenCase> contentTweens = new List<TweenCase>();
        private readonly Dictionary<RectTransform, Vector2> savedPositions = new Dictionary<RectTransform, Vector2>();
        private readonly Dictionary<RectTransform, Vector3> savedScales = new Dictionary<RectTransform, Vector3>();

        public bool IsVisible => gameObject.activeSelf;

        private enum LeaderboardTab
        {
            Global,
            Leagues
        }

        private void Awake()
        {
            Initialize();
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                initialized = false;
                Initialize();
            }
        }

        private void OnRectTransformDimensionsChange()
        {
            NormalizeSafeAreaRectTransform();

            if (structuredLayout == null)
                CacheTabContent();

            NormalizeDesignedLayout();
        }

        private void OnDestroy()
        {
            showTweenCase.KillActive();
            hideTweenCase.KillActive();
            StopScrollToCurrentPlayerCoroutine();
            StopCountdownCoroutine();
            KillContentTweens();
        }

        public void Show(bool loadOnlinePlayers = true)
        {
            Initialize();
            onlineRefreshEnabled = loadOnlinePlayers;
            showTweenCase.KillActive();
            hideTweenCase.KillActive();
            NormalizeDesignedLayout();
            Vector2 startPosition = shownPosition + Vector2.right * (GetScreenWidth() + HideScreenMargin);
            rectTransform.anchoredPosition = startPosition;
            rectTransform.localScale = Vector3.one;
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            gameObject.SetActive(true);
            ApplySelectedTab(false);
            StartCountdownCoroutine();
            if (onlineRefreshEnabled)
            {
                RenderCachedLeaderboards();
                _ = RefreshLeaderboardsAsync();
            }
            else
            {
                ClearLeaderboardRows();
            }

            showTweenCase = rectTransform
                .DOAnchoredPosition(shownPosition, HideDuration, unscaledTime: true)
                .SetEasing(Ease.Type.CubicOut)
                .OnComplete(() =>
                {
                    rectTransform.anchoredPosition = shownPosition;
                    canvasGroup.interactable = true;
                    canvasGroup.blocksRaycasts = true;
                    showTweenCase = null;
                    if (onlineRefreshEnabled)
                        ScheduleScrollToCurrentPlayer();
                });
        }

        public void Hide(SimpleCallback onComplete = null)
        {
            Initialize();

            if (!gameObject.activeSelf)
            {
                onComplete?.Invoke();
                return;
            }

            hideTweenCase.KillActive();
            showTweenCase.KillActive();
            StopScrollToCurrentPlayerCoroutine();
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            Vector2 targetPosition = shownPosition + Vector2.right * (GetScreenWidth() + HideScreenMargin);
            hideTweenCase = rectTransform
                .DOAnchoredPosition(targetPosition, HideDuration, unscaledTime: true)
                .SetEasing(Ease.Type.CubicIn)
                .OnComplete(() =>
                {
                    rectTransform.anchoredPosition = shownPosition;
                    canvasGroup.interactable = true;
                    canvasGroup.blocksRaycasts = true;
                    hideTweenCase = null;
                    gameObject.SetActive(false);
                    StopCountdownCoroutine();
                    onComplete?.Invoke();
                });
        }

        private float GetScreenWidth()
        {
            float screenWidth = rectTransform.rect.width;
            if (screenWidth <= 0f)
            {
                Canvas canvas = GetComponentInParent<Canvas>();
                screenWidth = canvas != null && canvas.scaleFactor > 0f
                    ? canvas.pixelRect.width / canvas.scaleFactor
                    : Screen.width;
            }

            return screenWidth;
        }

        private void Initialize()
        {
            if (initialized)
                return;

            rectTransform = (RectTransform)transform;
            canvasGroup = GetComponent<CanvasGroup>();
            NormalizeRootRectTransform();
            NormalizeSafeAreaRectTransform();
            CacheTabContent();
            NormalizeDesignedLayout();
            ConfigureScrollViews();

            Canvas canvas = GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.overrideSorting = true;
                canvas.sortingOrder = SortingOrder;
            }

            shownPosition = rectTransform.anchoredPosition;
            RegisterTabButtons();
            ApplySelectedTab(false);
            initialized = true;
        }

        private void NormalizeRootRectTransform()
        {
            rectTransform.localScale = Vector3.one;
            rectTransform.localRotation = Quaternion.identity;
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;
        }

        private void NormalizeSafeAreaRectTransform()
        {
            RectTransform safeArea = transform.Find("Safe Area") as RectTransform;
            if (safeArea == null)
                return;

            safeArea.localScale = Vector3.one;
            safeArea.localRotation = Quaternion.identity;
            safeArea.anchorMin = Vector2.zero;
            safeArea.anchorMax = Vector2.one;
            safeArea.pivot = new Vector2(0.5f, 0.5f);
            safeArea.offsetMin = Vector2.zero;
            safeArea.offsetMax = Vector2.zero;
            safeArea.anchoredPosition = Vector2.zero;
        }

        public void ShowGlobalTab()
        {
            SelectTab(LeaderboardTab.Global);
        }

        public void ShowLeaguesTab()
        {
            SelectTab(LeaderboardTab.Leagues);
        }

        private void SelectTab(LeaderboardTab tab)
        {
            Initialize();
            selectedTab = tab;
            ApplySelectedTab(true);
        }

        private void ApplySelectedTab(bool animate)
        {
            if (globalContent != null)
                globalContent.SetActive(selectedTab == LeaderboardTab.Global);

            if (leaguesContent != null)
                leaguesContent.SetActive(selectedTab == LeaderboardTab.Leagues);

            ConfigureScrollViews();

            if (animate)
            {
                if (onlineRefreshEnabled)
                    _ = RefreshSelectedLeaderboardAsync();
                else
                    ClearLeaderboardRows();

                AnimateSelectedTabContent();
                if (onlineRefreshEnabled)
                    ScheduleScrollToCurrentPlayer();
            }
        }

        private async System.Threading.Tasks.Task RefreshLeaderboardsAsync()
        {
            if (!onlineRefreshEnabled)
                return;

            await RefreshGlobalLeaderboardAsync();

            if (!onlineRefreshEnabled)
                return;

            await RefreshLeagueLeaderboardAsync();
        }

        private System.Threading.Tasks.Task RefreshSelectedLeaderboardAsync()
        {
            if (!onlineRefreshEnabled)
                return System.Threading.Tasks.Task.CompletedTask;

            RenderCachedSelectedLeaderboard();
            return selectedTab == LeaderboardTab.Global
                ? RefreshGlobalLeaderboardAsync()
                : RefreshLeagueLeaderboardAsync();
        }

        private void RenderCachedLeaderboards()
        {
            if (!Application.isPlaying)
                return;

            if (LocalLeaderboardService.TryGetCachedGlobalPlayers(out List<LeaderboardEntry> globalEntries))
                ApplyEntriesToRows(globalContent, globalRows, GetGlobalRowPrefab(), globalEntries);
            else
                ClearPodium(globalContent);

            if (LocalLeaderboardService.TryGetCachedLeaguePlayers(out List<LeaderboardEntry> leagueEntries))
                ApplyEntriesToRows(leaguesContent, leagueRows, GetLeagueRowPrefab(), leagueEntries);
            else
                ClearPodium(leaguesContent);
        }

        private void RenderCachedSelectedLeaderboard()
        {
            if (!Application.isPlaying)
                return;

            if (selectedTab == LeaderboardTab.Global)
            {
                if (LocalLeaderboardService.TryGetCachedGlobalPlayers(out List<LeaderboardEntry> entries))
                    ApplyEntriesToRows(globalContent, globalRows, GetGlobalRowPrefab(), entries);
                else
                    ClearPodium(globalContent);
            }
            else
            {
                if (LocalLeaderboardService.TryGetCachedLeaguePlayers(out List<LeaderboardEntry> entries))
                    ApplyEntriesToRows(leaguesContent, leagueRows, GetLeagueRowPrefab(), entries);
                else
                    ClearPodium(leaguesContent);
            }
        }

        private async System.Threading.Tasks.Task RefreshGlobalLeaderboardAsync()
        {
            if (!Application.isPlaying || !onlineRefreshEnabled || globalContent == null || globalRefreshInProgress)
                return;

            globalRefreshInProgress = true;
            try
            {
                List<LeaderboardEntry> entries = await LocalLeaderboardService.GetTopPlayersAsync(onlineGlobalLimit);
                if (this == null || !onlineRefreshEnabled || globalContent == null)
                    return;

                RectTransform rowsContent = FindRowsContent(globalContent.transform);
                PlayerElement prefab = GetGlobalRowPrefab();
                if (rowsContent == null || prefab == null)
                    return;

                ApplyEntriesToRows(globalContent, globalRows, prefab, entries);

                ConfigureScrollViews();
                if (selectedTab == LeaderboardTab.Global)
                    ScheduleScrollToCurrentPlayer();
            }
            finally
            {
                globalRefreshInProgress = false;
            }
        }

        private async System.Threading.Tasks.Task RefreshLeagueLeaderboardAsync()
        {
            if (!Application.isPlaying || !onlineRefreshEnabled || leaguesContent == null || leagueRefreshInProgress)
                return;

            leagueRefreshInProgress = true;
            try
            {
                List<LeaderboardEntry> entries = await LocalLeaderboardService.GetCurrentLeaguePlayersAsync(onlineLeagueLimit);
                if (this == null || !onlineRefreshEnabled || leaguesContent == null)
                    return;

                RectTransform rowsContent = FindRowsContent(leaguesContent.transform);
                PlayerElement prefab = GetLeagueRowPrefab();
                if (rowsContent == null || prefab == null)
                    return;

                ApplyEntriesToRows(leaguesContent, leagueRows, prefab, entries);

                ConfigureScrollViews();
                if (selectedTab == LeaderboardTab.Leagues)
                    ScheduleScrollToCurrentPlayer();
            }
            finally
            {
                leagueRefreshInProgress = false;
            }
        }

        private static UIProfilePopup FindSceneProfilePopup()
        {
            UIProfilePopup[] profilePopups = Resources.FindObjectsOfTypeAll<UIProfilePopup>();
            for (int i = 0; i < profilePopups.Length; i++)
            {
                UIProfilePopup profilePopup = profilePopups[i];
                if (profilePopup != null && profilePopup.gameObject.scene.IsValid())
                    return profilePopup;
            }

            return null;
        }

        private PlayerElement GetGlobalRowPrefab()
        {
            return globalRowPrefab != null ? globalRowPrefab : leagueRowPrefab;
        }

        private PlayerElement GetLeagueRowPrefab()
        {
            return leagueRowPrefab != null ? leagueRowPrefab : globalRowPrefab;
        }

        private void EnsureRowCount(List<PlayerElement> rows, RectTransform rowsContent, PlayerElement prefab, int requiredCount)
        {
            rows.RemoveAll(row => row == null);
            while (rows.Count < requiredCount)
            {
                PlayerElement row = Instantiate(prefab, rowsContent);
                row.gameObject.SetActive(false);
                rows.Add(row);
            }
        }

        private void ApplyEntriesToRows(GameObject content, List<PlayerElement> rows, PlayerElement prefab, List<LeaderboardEntry> entries)
        {
            if (content == null || prefab == null)
                return;

            RectTransform rowsContent = FindRowsContent(content.transform);
            if (rowsContent == null)
                return;

            entries = entries ?? new List<LeaderboardEntry>();
            UIProfilePopup profilePopup = FindSceneProfilePopup();
            ApplyEntriesToPodium(content, entries, profilePopup);
            EnsureRowCount(rows, rowsContent, prefab, entries.Count);

            for (int i = 0; i < rows.Count; i++)
            {
                bool hasEntry = i < entries.Count;
                PlayerElement row = rows[i];
                row.gameObject.SetActive(hasEntry);
                if (!hasEntry)
                    continue;

                LeaderboardEntry entry = entries[i];
                row.Apply(
                    entry,
                    profilePopup != null ? profilePopup.GetAvatarSprite(entry.AvatarIndex) : null,
                    profilePopup != null ? profilePopup.GetFrameSprite(entry.FrameIndex) : null);
            }

            ConfigureScrollViews();
        }

        private void ApplyEntriesToPodium(GameObject content, List<LeaderboardEntry> entries, UIProfilePopup profilePopup)
        {
            if (content == null)
                return;

            RectTransform podium = FindFirstChildContaining(content.transform, "Podium");
            if (podium == null)
                return;

            ApplyPodiumCard(podium.Find("First Place"), GetEntry(entries, 0), profilePopup);
            ApplyPodiumCard(podium.Find("Second Place"), GetEntry(entries, 1), profilePopup);
            ApplyPodiumCard(podium.Find("Third Place"), GetEntry(entries, 2), profilePopup);
        }

        private static LeaderboardEntry GetEntry(List<LeaderboardEntry> entries, int index)
        {
            return entries != null && index >= 0 && index < entries.Count ? entries[index] : null;
        }

        private static void ApplyPodiumCard(Transform card, LeaderboardEntry entry, UIProfilePopup profilePopup)
        {
            if (card == null)
                return;

            bool hasEntry = entry != null;
            card.gameObject.SetActive(hasEntry);
            if (!hasEntry)
                return;

            string playerName = string.IsNullOrWhiteSpace(entry.PlayerName) ? "Guest" : entry.PlayerName;
            SetPodiumText(card, "Player Name", playerName);
            SetPodiumText(card, "Score Value", entry.Score.ToString());

            if (profilePopup != null)
            {
                SetPodiumSprite(card, "Avatar Portrait", profilePopup.GetAvatarSprite(entry.AvatarIndex));
                SetPodiumSprite(card, "Avatar Frame", profilePopup.GetFrameSprite(entry.FrameIndex));
            }

            ArrangePodiumAvatarFrame(card);
        }

        private static void SetPodiumText(Transform card, string objectName, string value)
        {
            TMP_Text[] tmpTexts = card.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < tmpTexts.Length; i++)
            {
                if (tmpTexts[i] != null && tmpTexts[i].gameObject.name == objectName)
                    tmpTexts[i].text = value;
            }

            Text[] legacyTexts = card.GetComponentsInChildren<Text>(true);
            for (int i = 0; i < legacyTexts.Length; i++)
            {
                if (legacyTexts[i] != null && legacyTexts[i].gameObject.name == objectName)
                    legacyTexts[i].text = value;
            }
        }

        private static void SetPodiumSprite(Transform card, string objectName, Sprite sprite)
        {
            if (sprite == null)
                return;

            Image[] images = card.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                if (images[i] != null && images[i].gameObject.name == objectName)
                    images[i].sprite = sprite;
            }
        }

        private static void ArrangePodiumAvatarFrame(Transform card)
        {
            Image frame = FindChildImage(card, "Avatar Frame");
            Image portrait = FindChildImage(card, "Avatar Portrait");

            if (frame != null)
            {
                Transform portraitContainer = portrait != null ? portrait.transform.parent : null;
                if (portraitContainer != null && frame.transform.parent == portraitContainer.parent)
                {
                    int frameIndex = frame.transform.GetSiblingIndex();
                    int portraitContainerIndex = portraitContainer.GetSiblingIndex();
                    if (frameIndex < portraitContainerIndex)
                        frame.transform.SetSiblingIndex(portraitContainerIndex);
                    else if (frameIndex > portraitContainerIndex + 1)
                        frame.transform.SetSiblingIndex(portraitContainerIndex + 1);
                }
                else
                    frame.transform.SetAsFirstSibling();
            }

            if (portrait != null)
                portrait.transform.SetAsLastSibling();
        }

        private static Image FindChildImage(Transform root, string objectName)
        {
            if (root == null)
                return null;

            Image[] images = root.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                if (images[i] != null && images[i].gameObject.name == objectName)
                    return images[i];
            }

            return null;
        }

        private void ClearPodium(GameObject content)
        {
            ApplyEntriesToPodium(content, null, null);
        }

        private void ClearLeaderboardRows()
        {
            SetRowsActive(globalRows, false);
            SetRowsActive(leagueRows, false);
            ClearPodium(globalContent);
            ClearPodium(leaguesContent);
            ConfigureScrollViews();
        }

        private static void SetRowsActive(List<PlayerElement> rows, bool active)
        {
            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i] != null)
                    rows[i].gameObject.SetActive(active);
            }
        }

        private void CacheTabContent()
        {
            structuredLayout = transform.Find("Safe Area/Leaderboard Structured Layout") as RectTransform;
            if (structuredLayout == null)
                return;

            if (globalContent == null)
            {
                Transform globalContentTransform = structuredLayout.Find("Global Content");
                if (globalContentTransform != null)
                    globalContent = globalContentTransform.gameObject;
            }

            if (leaguesContent == null)
            {
                Transform leaguesContentTransform = structuredLayout.Find("Leagues Content");
                if (leaguesContentTransform != null)
                    leaguesContent = leaguesContentTransform.gameObject;
            }

            globalCountdownText = FindCountdownText(globalContent);
            leagueCountdownText = FindCountdownText(leaguesContent);
        }

        private TMP_Text FindCountdownText(GameObject content)
        {
            if (content == null)
                return null;

            TMP_Text[] texts = content.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                TMP_Text text = texts[i];
                if (text != null && text.gameObject.name == "Time Text")
                    return text;
            }

            return null;
        }

        private void StartCountdownCoroutine()
        {
            StopCountdownCoroutine();
            seasonReloadRequested = false;
            countdownCoroutine = StartCoroutine(UpdateCountdownLoop());
        }

        private void StopCountdownCoroutine()
        {
            if (countdownCoroutine == null)
                return;

            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
        }

        private System.Collections.IEnumerator UpdateCountdownLoop()
        {
            while (gameObject.activeInHierarchy)
            {
                TryResumeOnlineRefreshIfReachable();
                UpdateCountdownText();
                yield return new WaitForSecondsRealtime(1f);
            }

            countdownCoroutine = null;
        }

        private void UpdateCountdownText()
        {
            if (ShouldShowOfflineCountdown())
            {
                seasonReloadRequested = false;
                SetOfflineCountdownText();
                return;
            }

            offlineCountdownDotIndex = -1;
            TimeSpan remaining = LocalLeaderboardService.GetCurrentSeasonRemaining();
            if (remaining <= TimeSpan.Zero)
            {
                SetCountdownText("Season ended");
                if (onlineRefreshEnabled && !seasonReloadRequested && Application.isPlaying)
                {
                    seasonReloadRequested = true;
                    _ = ReloadAfterSeasonEndedAsync();
                }

                return;
            }

            seasonReloadRequested = false;
            SetCountdownText(FormatCountdown(remaining));
        }

        private bool ShouldShowOfflineCountdown()
        {
            return !onlineRefreshEnabled
                || !IsInternetReachable();
        }

        private void SetOfflineCountdownText()
        {
            offlineCountdownDotIndex = (offlineCountdownDotIndex + 1) % OfflineCountdownMaxDots;
            SetCountdownText(new string('.', offlineCountdownDotIndex + 1));
        }

        private void TryResumeOnlineRefreshIfReachable()
        {
            if (!Application.isPlaying
                || onlineRefreshEnabled
                || connectivityRefreshInProgress
                || !IsInternetReachable())
            {
                return;
            }

            _ = ResumeOnlineRefreshAsync();
        }

        private async System.Threading.Tasks.Task ResumeOnlineRefreshAsync()
        {
            if (connectivityRefreshInProgress || onlineRefreshEnabled || !IsInternetReachable())
                return;

            connectivityRefreshInProgress = true;
            onlineRefreshEnabled = true;
            offlineCountdownDotIndex = -1;
            RenderCachedLeaderboards();
            UpdateCountdownText();

            try
            {
                await RefreshLeaderboardsAsync();
                if (this == null || !gameObject.activeInHierarchy)
                    return;

                RenderCachedSelectedLeaderboard();
                ScheduleScrollToCurrentPlayer();
            }
            finally
            {
                connectivityRefreshInProgress = false;
            }
        }

        private static bool IsInternetReachable()
        {
            return Application.internetReachability != NetworkReachability.NotReachable;
        }

        private async System.Threading.Tasks.Task ReloadAfterSeasonEndedAsync()
        {
            if (!onlineRefreshEnabled)
            {
                seasonReloadRequested = false;
                return;
            }

            await RefreshLeaderboardsAsync();
            seasonReloadRequested = false;
            UpdateCountdownText();
        }

        private void SetCountdownText(string value)
        {
            if (globalCountdownText != null)
                globalCountdownText.text = value;

            if (leagueCountdownText != null)
                leagueCountdownText.text = value;
        }

        private static string FormatCountdown(TimeSpan remaining)
        {
            return string.Format(
                "{0:00}d {1:00}h {2:00}m",
                Mathf.FloorToInt((float)remaining.TotalDays),
                remaining.Hours,
                remaining.Minutes);
        }

        private void NormalizeDesignedLayout()
        {
            if (structuredLayout == null)
                return;

            RectTransform parentRect = structuredLayout.parent as RectTransform;
            if (parentRect == null)
                return;

            float parentWidth = parentRect.rect.width;
            float parentHeight = parentRect.rect.height;
            if (parentWidth <= 0f || parentHeight <= 0f)
                return;

            float horizontalScale = parentWidth / DesignWidth;
            float verticalScale = parentHeight / DesignHeight;
            structuredLayout.anchorMin = new Vector2(0.5f, 0.5f);
            structuredLayout.anchorMax = new Vector2(0.5f, 0.5f);
            structuredLayout.pivot = new Vector2(0.5f, 0.5f);
            structuredLayout.sizeDelta = new Vector2(DesignWidth, DesignHeight);
            structuredLayout.anchoredPosition = Vector2.zero;
            structuredLayout.localScale = new Vector3(horizontalScale, verticalScale, 1f);
            structuredLayout.localRotation = Quaternion.identity;
        }

        private void RegisterTabButtons()
        {
            if (tabButtonsRegistered)
                return;

            Button[] buttons = GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];
                string buttonName = button.gameObject.name;

                if (buttonName == "Global" || buttonName == "Global Hitbox")
                {
                    button.onClick.RemoveListener(ShowGlobalTab);
                    button.onClick.AddListener(ShowGlobalTab);
                    UIAudioFeedback.RegisterButton(button);
                    UIHapticFeedback.RegisterButton(button);
                }
                else if (buttonName == "Leagues" || buttonName == "Leagues Hitbox")
                {
                    button.onClick.RemoveListener(ShowLeaguesTab);
                    button.onClick.AddListener(ShowLeaguesTab);
                    UIAudioFeedback.RegisterButton(button);
                    UIHapticFeedback.RegisterButton(button);
                }
            }

            tabButtonsRegistered = true;
        }

        private void ConfigureScrollViews()
        {
            ConfigureScrollView("Safe Area/Leaderboard Structured Layout/Global Content/04 Leaderboard Rows/Rows Scroll View");
            ConfigureScrollView("Safe Area/Leaderboard Structured Layout/Leagues Content/05 League Rows Scroll Area/League Rows Scroll View");
        }

        private void ConfigureScrollView(string scrollViewPath)
        {
            Transform scrollViewTransform = transform.Find(scrollViewPath);
            if (scrollViewTransform == null)
                return;

            bool shouldLimitRowsViewport = scrollViewPath.Contains("Leagues Content")
                || scrollViewPath.Contains("Global Content");
            RectTransform scrollViewRect = scrollViewTransform as RectTransform;
            RectTransform scrollAreaRect = scrollViewRect != null ? scrollViewRect.parent as RectTransform : null;
            if (shouldLimitRowsViewport)
                EnsureRowsInitialViewportHeight(scrollAreaRect, scrollViewRect);

            ScrollRect scrollRect = scrollViewTransform.GetComponent<ScrollRect>();
            if (scrollRect == null)
                scrollRect = scrollViewTransform.gameObject.AddComponent<ScrollRect>();

            RectTransform viewport = scrollViewTransform.Find("Viewport") as RectTransform;
            if (viewport == null)
                return;

            if (shouldLimitRowsViewport)
                SetRectHeight(viewport, RowsInitialViewportHeight);

            RectTransform content = viewport.Find("Content") as RectTransform;
            if (content == null)
                return;

            Image viewportRaycast = viewport.GetComponent<Image>();
            if (viewportRaycast == null)
                viewportRaycast = viewport.gameObject.AddComponent<Image>();

            viewportRaycast.color = new Color(1f, 1f, 1f, 0f);
            viewportRaycast.raycastTarget = true;

            if (viewport.GetComponent<RectMask2D>() == null)
                viewport.gameObject.AddComponent<RectMask2D>();

            VerticalLayoutGroup layoutGroup = content.GetComponent<VerticalLayoutGroup>();
            if (layoutGroup == null)
                layoutGroup = content.gameObject.AddComponent<VerticalLayoutGroup>();

            layoutGroup.childAlignment = TextAnchor.UpperCenter;
            layoutGroup.childControlWidth = false;
            layoutGroup.childControlHeight = false;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.spacing = 18f;

            ContentSizeFitter sizeFitter = content.GetComponent<ContentSizeFitter>();
            if (sizeFitter == null)
                sizeFitter = content.gameObject.AddComponent<ContentSizeFitter>();

            sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            content.anchorMin = new Vector2(0.5f, 1f);
            content.anchorMax = new Vector2(0.5f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = new Vector2(0f, Mathf.Max(0f, content.anchoredPosition.y));

            scrollRect.viewport = viewport;
            scrollRect.content = content;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;
            scrollRect.inertia = true;
            scrollRect.scrollSensitivity = 45f;

            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            EnsureRowsContentHeight(content, layoutGroup);
        }

        private void EnsureRowsInitialViewportHeight(RectTransform scrollAreaRect, RectTransform scrollViewRect)
        {
            SetRectHeight(scrollAreaRect, RowsInitialViewportHeight);
            SetRectHeight(scrollViewRect, RowsInitialViewportHeight);

            if (scrollAreaRect == null)
                return;

            RectTransform rowsBackground = scrollAreaRect.Find("Rows Background") as RectTransform;
            SetRectHeight(rowsBackground, RowsInitialViewportHeight);
        }

        private void SetRectHeight(RectTransform rect, float height)
        {
            if (rect == null)
                return;

            Vector2 size = rect.sizeDelta;
            float anchoredParentHeight = 0f;
            RectTransform parentRect = rect.parent as RectTransform;
            if (parentRect != null)
                anchoredParentHeight = parentRect.rect.height * (rect.anchorMax.y - rect.anchorMin.y);

            float targetSizeDeltaY = height - anchoredParentHeight;
            if (Mathf.Approximately(size.y, targetSizeDeltaY))
                return;

            size.y = targetSizeDeltaY;
            rect.sizeDelta = size;
        }

        private void AnimateSelectedTabContent()
        {
            KillContentTweens();

            GameObject content = selectedTab == LeaderboardTab.Global ? globalContent : leaguesContent;
            if (content == null || !content.activeInHierarchy)
                return;

            AnimateCards(content);
            AnimateRows(content);
        }

        private void AnimateCards(GameObject content)
        {
            RectTransform podium = FindFirstChildContaining(content.transform, "Podium");
            if (podium == null)
                return;

            int animatedIndex = 0;
            for (int i = 0; i < podium.childCount; i++)
            {
                RectTransform card = podium.GetChild(i) as RectTransform;
                if (card == null || !card.gameObject.activeSelf || card.name.Contains("Background"))
                    continue;

                float delay = animatedIndex * CardStagger;
                PrepareScaleTarget(card, Vector3.one * 0.86f);
                PrepareFadeTarget(card, 0f);

                contentTweens.Add(card.DOScale(GetSavedScale(card), CardAnimationDuration, delay, unscaledTime: true)
                    .SetEasing(Ease.Type.BackOut));

                CanvasGroup canvasGroup = GetOrCreateCanvasGroup(card.gameObject);
                contentTweens.Add(canvasGroup.DOFade(1f, CardAnimationDuration * 0.7f, delay, unscaledTime: true)
                    .SetEasing(Ease.Type.CubicOut));

                animatedIndex++;
            }
        }

        private void AnimateRows(GameObject content)
        {
            RectTransform rowsContent = FindRowsContent(content.transform);
            if (rowsContent == null)
                return;

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(rowsContent);

            ScrollRect scrollRect = rowsContent.GetComponentInParent<ScrollRect>();
            if (scrollRect != null)
                scrollRect.verticalNormalizedPosition = 1f;

            int animatedIndex = 0;
            for (int i = 0; i < rowsContent.childCount; i++)
            {
                RectTransform row = rowsContent.GetChild(i) as RectTransform;
                if (row == null || !row.gameObject.activeSelf || !row.name.StartsWith("Row"))
                    continue;

                CanvasGroup canvasGroup = GetOrCreateCanvasGroup(row.gameObject);
                row.localScale = GetSavedScale(row);
                canvasGroup.alpha = 1f;

                if (animatedIndex >= MaxAnimatedRows)
                    continue;

                float delay = animatedIndex * RowStagger;
                PrepareScaleTarget(row, GetSavedScale(row) * 0.96f);
                PrepareFadeTarget(row, 0f);

                contentTweens.Add(row.DOScale(GetSavedScale(row), RowAnimationDuration, delay, unscaledTime: true)
                    .SetEasing(Ease.Type.BackOut));

                contentTweens.Add(canvasGroup.DOFade(1f, RowAnimationDuration * 0.85f, delay, unscaledTime: true)
                    .SetEasing(Ease.Type.CubicOut));

                animatedIndex++;
            }
        }

        private RectTransform FindRowsContent(Transform content)
        {
            ScrollRect[] scrollRects = content.GetComponentsInChildren<ScrollRect>(true);
            for (int i = 0; i < scrollRects.Length; i++)
            {
                if (scrollRects[i].content != null && scrollRects[i].content.name == "Content")
                    return scrollRects[i].content;
            }

            return null;
        }

        private RectTransform FindFirstChildContaining(Transform root, string namePart)
        {
            RectTransform[] children = root.GetComponentsInChildren<RectTransform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i].name.Contains(namePart))
                    return children[i];
            }

            return null;
        }

        private void PrepareScaleTarget(RectTransform target, Vector3 startScale)
        {
            savedScales[target] = GetSavedScale(target);
            target.localScale = startScale;
        }

        private void PreparePositionTarget(RectTransform target, Vector2 startPosition)
        {
            savedPositions[target] = GetSavedPosition(target);
            target.anchoredPosition = startPosition;
        }

        private void PrepareFadeTarget(RectTransform target, float startAlpha)
        {
            CanvasGroup canvasGroup = GetOrCreateCanvasGroup(target.gameObject);
            canvasGroup.alpha = startAlpha;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        private Vector2 GetSavedPosition(RectTransform target)
        {
            if (!savedPositions.TryGetValue(target, out Vector2 savedPosition))
            {
                savedPosition = target.anchoredPosition;
                savedPositions.Add(target, savedPosition);
            }

            return savedPosition;
        }

        private Vector3 GetSavedScale(RectTransform target)
        {
            if (!savedScales.TryGetValue(target, out Vector3 savedScale))
            {
                savedScale = target.localScale;
                savedScales.Add(target, savedScale);
            }

            return savedScale;
        }

        private CanvasGroup GetOrCreateCanvasGroup(GameObject target)
        {
            CanvasGroup canvasGroup = target.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = target.AddComponent<CanvasGroup>();

            return canvasGroup;
        }

        private void KillContentTweens()
        {
            for (int i = 0; i < contentTweens.Count; i++)
                contentTweens[i].KillActive();

            contentTweens.Clear();
        }

        private void ScheduleScrollToCurrentPlayer()
        {
            if (!autoScrollToCurrentPlayer || !Application.isPlaying || !gameObject.activeInHierarchy)
                return;

            if (scrollToCurrentPlayerCoroutine != null)
                return;

            scrollToCurrentPlayerCoroutine = StartCoroutine(ScrollToCurrentPlayerAfterLayout());
        }

        private void StopScrollToCurrentPlayerCoroutine()
        {
            if (scrollToCurrentPlayerCoroutine == null)
                return;

            StopCoroutine(scrollToCurrentPlayerCoroutine);
            scrollToCurrentPlayerCoroutine = null;
        }

        private System.Collections.IEnumerator ScrollToCurrentPlayerAfterLayout()
        {
            yield return null;
            yield return new WaitForEndOfFrame();
            if (scrollToCurrentPlayerStartDelay > 0f)
                yield return new WaitForSecondsRealtime(scrollToCurrentPlayerStartDelay);

            ConfigureScrollViews();
            Canvas.ForceUpdateCanvases();
            yield return ScrollToCurrentPlayerAnimated();
            scrollToCurrentPlayerCoroutine = null;
        }

        private System.Collections.IEnumerator ScrollToCurrentPlayerAnimated()
        {
            GameObject selectedContent = selectedTab == LeaderboardTab.Global ? globalContent : leaguesContent;
            if (selectedContent == null || !selectedContent.activeInHierarchy)
                yield break;

            RectTransform rowsContent = FindRowsContent(selectedContent.transform);
            if (rowsContent == null)
                yield break;

            ScrollRect scrollRect = rowsContent.GetComponentInParent<ScrollRect>();
            if (scrollRect == null || scrollRect.viewport == null || scrollRect.content == null)
                yield break;

            LayoutRebuilder.ForceRebuildLayoutImmediate(rowsContent);
            VerticalLayoutGroup layoutGroup = rowsContent.GetComponent<VerticalLayoutGroup>();
            EnsureRowsContentHeight(rowsContent, layoutGroup);

            PlayerElement currentPlayerElement = FindCurrentPlayerElement(rowsContent);
            if (currentPlayerElement == null)
                yield break;

            float targetScrollY;
            float maxScrollY;
            if (!TryGetElementScrollY(scrollRect, currentPlayerElement.RectTransform, out targetScrollY, out maxScrollY))
                yield break;

            scrollRect.StopMovement();
            float startScrollY = Mathf.Clamp(scrollRect.content.anchoredPosition.y, 0f, maxScrollY);
            float scrollDistance = Mathf.Abs(targetScrollY - startScrollY);
            float scrollDuration = Mathf.Clamp(
                scrollDistance / AutoScrollUnitsPerSecond,
                scrollToCurrentPlayerDuration,
                MaxAutoScrollDuration);

            if (scrollToCurrentPlayerDuration <= 0f || scrollDistance <= 0.5f)
            {
                SetScrollY(scrollRect, targetScrollY, maxScrollY);
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < scrollDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / scrollDuration);
                float easedT = t * t * (3f - 2f * t);
                SetScrollY(scrollRect, Mathf.Lerp(startScrollY, targetScrollY, easedT), maxScrollY);
                yield return null;
            }

            SetScrollY(scrollRect, targetScrollY, maxScrollY);
        }

        private PlayerElement FindCurrentPlayerElement(RectTransform rowsContent)
        {
            PlayerElement[] playerElements = rowsContent.GetComponentsInChildren<PlayerElement>(true);
            if (playerElements == null || playerElements.Length == 0)
                return null;

            for (int i = 0; i < playerElements.Length; i++)
                playerElements[i]?.RefreshCachedData();

            for (int i = 0; i < playerElements.Length; i++)
            {
                if (playerElements[i] != null && playerElements[i].IsCurrentPlayer)
                    return playerElements[i];
            }

            string currentPlayerName = GetCurrentPlayerName();
            if (!string.IsNullOrWhiteSpace(currentPlayerName))
            {
                for (int i = 0; i < playerElements.Length; i++)
                {
                    PlayerElement playerElement = playerElements[i];
                    if (playerElement != null
                        && !string.IsNullOrWhiteSpace(playerElement.PlayerName)
                        && string.Equals(playerElement.PlayerName.Trim(), currentPlayerName.Trim(), System.StringComparison.OrdinalIgnoreCase))
                    {
                        return playerElement;
                    }
                }
            }

            int fallbackRank = selectedTab == LeaderboardTab.Global
                ? globalCurrentPlayerFallbackRank
                : leaguesCurrentPlayerFallbackRank;

            for (int i = 0; i < playerElements.Length; i++)
            {
                PlayerElement playerElement = playerElements[i];
                if (playerElement != null && playerElement.Rank == fallbackRank)
                    return playerElement;
            }

            return null;
        }

        private string GetCurrentPlayerName()
        {
            UIProfilePopup profilePopup = FindFirstObjectByType<UIProfilePopup>(FindObjectsInactive.Include);
            if (profilePopup == null)
                return string.Empty;

            TMP_InputField[] inputFields = profilePopup.GetComponentsInChildren<TMP_InputField>(true);
            for (int i = 0; i < inputFields.Length; i++)
            {
                TMP_InputField inputField = inputFields[i];
                if (inputField != null && !string.IsNullOrWhiteSpace(inputField.text))
                    return inputField.text;
            }

            TMP_Text[] texts = profilePopup.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                TMP_Text text = texts[i];
                if (text != null && text.gameObject.name == "Player Name" && !string.IsNullOrWhiteSpace(text.text))
                    return text.text;
            }

            return string.Empty;
        }

        private bool TryGetElementScrollY(ScrollRect scrollRect, RectTransform target, out float targetScrollY, out float maxScrollY)
        {
            targetScrollY = 0f;
            maxScrollY = 0f;

            if (scrollRect == null || target == null || scrollRect.content == null || scrollRect.viewport == null)
                return false;

            RectTransform content = scrollRect.content;
            RectTransform viewport = scrollRect.viewport;
            float contentTop;
            float contentHeight = GetRowsContentHeight(content, out contentTop);
            float viewportHeight = viewport.rect.height;
            maxScrollY = Mathf.Max(0f, contentHeight - viewportHeight);
            if (maxScrollY <= 0f)
            {
                scrollRect.verticalNormalizedPosition = 1f;
                return false;
            }

            Bounds targetBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(content, target);
            float targetCenterFromTop = contentTop - targetBounds.center.y;
            targetScrollY = targetCenterFromTop - viewportHeight * Mathf.Clamp01(currentPlayerViewportPosition);
            targetScrollY = Mathf.Clamp(targetScrollY, 0f, maxScrollY);

            return true;
        }

        private static void EnsureRowsContentHeight(RectTransform content, VerticalLayoutGroup layoutGroup)
        {
            if (content == null)
                return;

            float contentTop;
            float contentHeight = GetRowsContentHeight(content, out contentTop);
            if (contentHeight <= 0f)
                return;

            Vector2 size = content.sizeDelta;
            if (!Mathf.Approximately(size.y, contentHeight))
            {
                size.y = contentHeight;
                content.sizeDelta = size;
            }
        }

        private static float GetRowsContentHeight(RectTransform content, out float contentTop)
        {
            contentTop = 0f;
            if (content == null)
                return 0f;

            float preferredHeight = LayoutUtility.GetPreferredHeight(content);
            float rectHeight = content.rect.height;
            float height = Mathf.Max(preferredHeight, rectHeight);
            if (height > 0f)
                return height;

            bool hasBounds = false;
            float maxY = float.MinValue;
            float minY = float.MaxValue;
            for (int i = 0; i < content.childCount; i++)
            {
                RectTransform child = content.GetChild(i) as RectTransform;
                if (child == null || !child.gameObject.activeSelf || !child.name.StartsWith("Row"))
                    continue;

                Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(content, child);
                maxY = Mathf.Max(maxY, bounds.max.y);
                minY = Mathf.Min(minY, bounds.min.y);
                hasBounds = true;
            }

            if (!hasBounds)
                return 0f;

            contentTop = maxY;
            return Mathf.Max(0f, maxY - minY);
        }

        private static void SetScrollY(ScrollRect scrollRect, float scrollY, float maxScrollY)
        {
            if (scrollRect == null || scrollRect.content == null)
                return;

            scrollRect.StopMovement();
            RectTransform content = scrollRect.content;
            content.anchoredPosition = new Vector2(content.anchoredPosition.x, scrollY);
            scrollRect.verticalNormalizedPosition = maxScrollY > 0f ? 1f - scrollY / maxScrollY : 1f;
        }
    }
}
