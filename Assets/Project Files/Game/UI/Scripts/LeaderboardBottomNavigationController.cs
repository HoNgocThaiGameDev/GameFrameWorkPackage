using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using NebulaSoft.IAPStore;
using UnityEngine.SceneManagement;
using TMPro;

namespace NebulaSoft
{
    [RequireComponent(typeof(RectTransform))]
    public class LeaderboardBottomNavigationController : MonoBehaviour
    {
        private enum NavigationDestination
        {
            Store,
            Home,
            Quest,
            Leaderboard
        }

        private const string DedicatedCanvasName = "Bottom Navigation Canvas";
        private const float StoreAnchorX = 0.1f;
        private const float EventsAnchorX = 0.3f;
        private const float HomeAnchorX = 0.5f;
        private const float CardsAnchorX = 0.7f;
        private const float LeaderboardAnchorX = 0.9f;
        private const float TabHalfAnchorWidth = 0.1f;
        private const int NoConnectionPopupSortingOrder = 5000;

        [SerializeField] Vector2 inactiveIconSize = new Vector2(52f, 52f);
        [SerializeField] Vector2 activeIconSize = new Vector2(72f, 68f);
        [SerializeField] float inactiveIconCenterY = 70.8f;
        [SerializeField] float activeIconCenterY = 126f;
        [SerializeField] float activeLabelCenterY = 48f;
        [SerializeField] Vector2 normalActiveCardSize = new Vector2(210f, 184f);
        [SerializeField] Vector2 leaderboardActiveCardSize = new Vector2(230f, 184f);
        [SerializeField] Vector2 selectedTabBackgroundSize = new Vector2(210f, 180f);
        [SerializeField] float tabSwitchDuration = 0.18f;
        [SerializeField] AnimationCurve tabSwitchCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] bool showStorePageOnSelect = true;
        [SerializeField] bool keepAboveStorePage = true;
        [SerializeField] int navigationSortingOrder = 900;
        [SerializeField] Sprite shopLabelSprite;
        [SerializeField] Sprite leaderboardLabelSprite;
        [SerializeField] Vector2 shopLabelSize = new Vector2(118f, 58f);
        [SerializeField] Vector2 leaderboardLabelSize = new Vector2(118f, 58f);
        [SerializeField] Vector2 activeTextLabelSize = new Vector2(166f, 52f);
        [SerializeField] float activeTextFontSize = 34f;
        [SerializeField] Color activeTextColor = new Color(0.45f, 0.31f, 0.86f, 1f);
        [SerializeField] Color activeTextOutlineColor = Color.white;
        [SerializeField] GameObject noConnectionPopup;
#if UNITY_EDITOR
        [Header("Editor Testing")]
        [SerializeField] bool simulateNoConnectionInEditor;
#endif

        private CanvasGroup dedicatedCanvasGroup;
        private static LeaderboardBottomNavigationController activeInstance;
        private RectTransform activeCard;
        private RectTransform selectedTabBackground;
        private RectTransform navigationBackground;
        private Image activeIcon;
        private Image homeLabelImage;
        private Image activeSpriteLabel;
        private TextMeshProUGUI activeTextLabel;

        private Transform storeTab;
        private Transform eventsTab;
        private Transform homeTab;
        private Transform cardsTab;
        private Transform leaderboardTab;

        private Sprite storeSprite;
        private Sprite homeSprite;
        private Sprite cardsSprite;
        private Sprite leaderboardSprite;
        private Coroutine tabSwitchRoutine;
        private NavigationDestination requestedDestination = NavigationDestination.Home;
        private bool pageTransitionInProgress;
        private bool requestedLeaderboardOnlinePlayers = true;
        private Button noConnectionOkButton;
        private Button noConnectionCloseButton;
        private QuestPanelView questPanel;
        private Coroutine homeControlsRefreshRoutine;

        private void Awake()
        {
            activeInstance = this;
            EnsureDedicatedCanvasRoot();
            CacheReferences();
            ApplyCanvasSorting();
            NormalizeLayout();
            RegisterButtons();
            RegisterNoConnectionPopupButtons();
            HideNoConnectionPopup();
            SelectHome(false);
            RefreshHomeTabControls();
        }

        private void OnEnable()
        {
            BottomNavigationVisibilityEvents.ShowRequested += ShowBottomNavigation;
            BottomNavigationVisibilityEvents.HideRequested += HideBottomNavigation;
            UIController.PopupOpened += OnPopupWindowStateChanged;
            UIController.PopupClosed += OnPopupWindowStateChanged;
            UIRewardsConfirmation.RewardPopupVisibilityChanged += OnStandalonePopupVisibilityChanged;
            UIProfilePopup.ProfilePopupVisibilityChanged += OnStandalonePopupVisibilityChanged;
            DailyRewardPopupView.PopupVisibilityChanged += OnStandalonePopupVisibilityChanged;
            IAPManager.ProductPurchaseCompleted += OnPurchaseCompleted;
            StartCoroutine(ShowWhenNavigationAllowed());
        }

        private void OnDisable()
        {
            BottomNavigationVisibilityEvents.ShowRequested -= ShowBottomNavigation;
            BottomNavigationVisibilityEvents.HideRequested -= HideBottomNavigation;
            UIController.PopupOpened -= OnPopupWindowStateChanged;
            UIController.PopupClosed -= OnPopupWindowStateChanged;
            UIRewardsConfirmation.RewardPopupVisibilityChanged -= OnStandalonePopupVisibilityChanged;
            UIProfilePopup.ProfilePopupVisibilityChanged -= OnStandalonePopupVisibilityChanged;
            DailyRewardPopupView.PopupVisibilityChanged -= OnStandalonePopupVisibilityChanged;
            IAPManager.ProductPurchaseCompleted -= OnPurchaseCompleted;

            if (tabSwitchRoutine != null)
            {
                StopCoroutine(tabSwitchRoutine);
                tabSwitchRoutine = null;
            }

            if (homeControlsRefreshRoutine != null)
            {
                StopCoroutine(homeControlsRefreshRoutine);
                homeControlsRefreshRoutine = null;
            }

        }

        private void OnDestroy()
        {
            if (activeInstance == this)
                activeInstance = null;

            if (noConnectionOkButton != null)
                noConnectionOkButton.onClick.RemoveListener(OnNoConnectionCloseClicked);

            if (noConnectionCloseButton != null)
                noConnectionCloseButton.onClick.RemoveListener(OnNoConnectionCloseClicked);

            if (questPanel != null)
                Destroy(questPanel.gameObject);
        }

        private void Update()
        {
            UpdateLoadingVisibility();
            HideNoConnectionPopupIfReachable();
        }

        private void ShowBottomNavigation()
        {
            RefreshBottomNavigationVisibility();
        }

        private void HideBottomNavigation()
        {
            ApplyBottomNavigationVisibility(false);
        }

        private IEnumerator ShowWhenNavigationAllowed()
        {
            while (isActiveAndEnabled && !IsBottomNavigationVisibleAllowed())
                yield return null;

            if (isActiveAndEnabled)
                RefreshBottomNavigationVisibility();
        }

#if UNITY_EDITOR
        private bool validateQueued;

        private void OnValidate()
        {
            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode || validateQueued)
                return;

            validateQueued = true;
            UnityEditor.EditorApplication.delayCall += DelayedValidate;
        }

        private void DelayedValidate()
        {
            if (this == null)
                return;

            validateQueued = false;
            CacheReferences();
            ApplyCanvasSorting();
            NormalizeLayout();
        }
#endif

        public void SelectStore()
        {
            SelectTab(storeTab, StoreAnchorX, normalActiveCardSize, storeSprite,
                "Shop", false, true);
            RequestPageTransition(NavigationDestination.Store);
        }

        public void SelectHome()
        {
            SelectHome(true);
            RequestPageTransition(NavigationDestination.Home);
        }

        public void SelectQuest()
        {
            SelectTab(cardsTab, CardsAnchorX, normalActiveCardSize, cardsSprite,
                "Quest", false, true);
            RequestPageTransition(NavigationDestination.Quest);
        }

        private void SelectHome(bool animate)
        {
            SelectTab(homeTab, HomeAnchorX, normalActiveCardSize, homeSprite,
                "Home", false, animate);
        }

        public void SelectLeaderboard()
        {
            bool noConnection = ShouldShowNoConnectionImmediately();

            if (!noConnection)
                HideNoConnectionPopup();

            OpenLeaderboard(!noConnection);

            if (noConnection)
                ShowNoConnectionPopup();
        }

        public static bool IsNoConnectionActive()
        {
#if UNITY_EDITOR
            if (activeInstance != null && activeInstance.simulateNoConnectionInEditor)
                return true;
#endif
            return Application.internetReachability == NetworkReachability.NotReachable;
        }

        public static bool TryShowNoConnectionPopup()
        {
            if (activeInstance == null)
            {
                Debug.LogWarning("[LeaderboardBottomNavigationController]: No active navigation controller is available to show no connection popup.");
                return false;
            }

            activeInstance.ShowNoConnectionPopup();
            return true;
        }

        private bool ShouldShowNoConnectionImmediately()
        {
#if UNITY_EDITOR
            if (simulateNoConnectionInEditor)
                return true;
#endif
            return Application.internetReachability == NetworkReachability.NotReachable;
        }

        private void OpenLeaderboard(bool loadOnlinePlayers = true)
        {
            SelectTab(leaderboardTab, LeaderboardAnchorX, leaderboardActiveCardSize, leaderboardSprite,
                "Ranking", false, true);
            RequestPageTransition(NavigationDestination.Leaderboard, loadOnlinePlayers);
        }

        private void ShowNoConnectionPopup()
        {
            if (noConnectionPopup == null)
            {
                Debug.LogWarning("[LeaderboardBottomNavigationController]: No connection popup reference is missing.");
                return;
            }

            RegisterNoConnectionPopupButtons();
            noConnectionPopup.transform.SetParent(null, false);
            noConnectionPopup.SetActive(true);
            noConnectionPopup.transform.localScale = Vector3.one;

            if (noConnectionPopup.transform is RectTransform popupRectTransform)
            {
                popupRectTransform.anchorMin = Vector2.zero;
                popupRectTransform.anchorMax = Vector2.one;
                popupRectTransform.offsetMin = Vector2.zero;
                popupRectTransform.offsetMax = Vector2.zero;
                popupRectTransform.anchoredPosition = Vector2.zero;
                popupRectTransform.localRotation = Quaternion.identity;
            }

            noConnectionPopup.transform.SetAsLastSibling();

            Canvas[] canvases = noConnectionPopup.GetComponentsInChildren<Canvas>(true);
            for (int i = 0; i < canvases.Length; i++)
            {
                canvases[i].enabled = true;
                canvases[i].overrideSorting = true;
                canvases[i].sortingOrder = NoConnectionPopupSortingOrder + i;
            }

            CanvasGroup[] canvasGroups = noConnectionPopup.GetComponentsInChildren<CanvasGroup>(true);
            for (int i = 0; i < canvasGroups.Length; i++)
            {
                canvasGroups[i].alpha = 1f;
                canvasGroups[i].interactable = true;
                canvasGroups[i].blocksRaycasts = true;
            }

            RefreshBottomNavigationVisibility();
        }

        private void HideNoConnectionPopup()
        {
            if (noConnectionPopup != null)
                noConnectionPopup.SetActive(false);

            RefreshBottomNavigationVisibility();
        }

        private void HideNoConnectionPopupIfReachable()
        {
            if (noConnectionPopup == null || !noConnectionPopup.activeSelf || ShouldShowNoConnectionImmediately())
                return;

            HideNoConnectionPopup();
        }

        private void RegisterNoConnectionPopupButtons()
        {
            if (noConnectionPopup == null)
                return;

            if (noConnectionOkButton == null)
            {
                Transform okTransform = noConnectionPopup.transform.Find("Popup Panel/Retry Button");
                noConnectionOkButton = okTransform?.GetComponent<Button>();
                if (noConnectionOkButton != null)
                {
                    noConnectionOkButton.onClick.AddListener(OnNoConnectionCloseClicked);
                    UIAudioFeedback.RegisterButton(noConnectionOkButton);
                    UIHapticFeedback.RegisterButton(noConnectionOkButton);
                }
            }

            if (noConnectionCloseButton == null)
            {
                Transform closeTransform = FindChildRecursive(noConnectionPopup.transform, "Close Button");
                noConnectionCloseButton = closeTransform?.GetComponent<Button>();
                if (noConnectionCloseButton != null)
                {
                    noConnectionCloseButton.onClick.AddListener(OnNoConnectionCloseClicked);
                    UIAudioFeedback.RegisterButton(noConnectionCloseButton);
                    UIHapticFeedback.RegisterButton(noConnectionCloseButton);
                }
            }
        }

        private void OnNoConnectionCloseClicked()
        {
            HideNoConnectionPopup();
        }

        private static Transform FindChildRecursive(Transform parent, string childName)
        {
            if (parent == null)
                return null;

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name == childName)
                    return child;

                Transform nestedChild = FindChildRecursive(child, childName);
                if (nestedChild != null)
                    return nestedChild;
            }

            return null;
        }

        private void CacheReferences()
        {
            storeTab = transform.Find("Store");
            eventsTab = transform.Find("Events Locked");
            homeTab = transform.Find("Home");
            cardsTab = transform.Find("Quest") ?? transform.Find("Cards") ?? transform.Find("Cards Locked");
            leaderboardTab = transform.Find("Leaderboard");
            navigationBackground = transform.Find("Navigation Background") as RectTransform;

            activeCard = transform.Find("Home Active") as RectTransform;
            if (activeCard == null)
                return;

            selectedTabBackground = activeCard.Find("Selected Tab BG") as RectTransform;
            activeIcon = activeCard.Find("Home Icon")?.GetComponent<Image>();
            homeLabelImage = activeCard.Find("Home Label")?.GetComponent<Image>();
            activeSpriteLabel = activeCard.Find("Active Sprite Label")?.GetComponent<Image>();
            activeTextLabel = activeCard.Find("Active TMP Label")?.GetComponent<TextMeshProUGUI>();

            storeSprite = GetTabIcon(storeTab)?.sprite;
            homeSprite = GetTabIcon(homeTab)?.sprite ?? activeIcon?.sprite;
            cardsSprite = GetTabIcon(cardsTab)?.sprite;
            leaderboardSprite = GetTabIcon(leaderboardTab)?.sprite;
        }

        private void NormalizeLayout()
        {
            NormalizeTab(storeTab, StoreAnchorX);
            NormalizeTab(eventsTab, EventsAnchorX);
            NormalizeTab(homeTab, HomeAnchorX);
            NormalizeTab(cardsTab, CardsAnchorX);
            NormalizeTab(leaderboardTab, LeaderboardAnchorX);

            NormalizeActiveCard();
        }

        private void NormalizeTab(Transform tab, float anchorX)
        {
            if (tab == null)
                return;

            RectTransform tabRect = (RectTransform)tab;
            tabRect.anchorMin = new Vector2(anchorX - TabHalfAnchorWidth, 0f);
            tabRect.anchorMax = new Vector2(anchorX + TabHalfAnchorWidth, 0f);
            tabRect.pivot = new Vector2(0.5f, 0f);
            tabRect.anchoredPosition = Vector2.zero;
            float hitAreaHeight = navigationBackground != null
                ? navigationBackground.rect.height
                : 118f;
            tabRect.sizeDelta = new Vector2(0f, hitAreaHeight);

            Image icon = GetTabIcon(tab);
            if (icon == null)
                return;

            RectTransform iconRect = icon.rectTransform;
            iconRect.anchorMin = new Vector2(0.5f, 0f);
            iconRect.anchorMax = new Vector2(0.5f, 0f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            float centerY = navigationBackground != null
                ? navigationBackground.rect.height * 0.5f
                : inactiveIconCenterY;
            iconRect.anchoredPosition = new Vector2(0f, centerY);
            iconRect.sizeDelta = inactiveIconSize;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
        }

        private void NormalizeActiveCard()
        {
            if (activeCard == null)
                return;

            activeCard.anchorMin = new Vector2(HomeAnchorX, 0f);
            activeCard.anchorMax = new Vector2(HomeAnchorX, 0f);
            activeCard.pivot = new Vector2(0.5f, 0f);
            activeCard.sizeDelta = normalActiveCardSize;
            NormalizeSelectedTabBackground(normalActiveCardSize);

            if (activeIcon != null)
            {
                activeIcon.rectTransform.anchorMin = new Vector2(0.5f, 0f);
                activeIcon.rectTransform.anchorMax = new Vector2(0.5f, 0f);
                activeIcon.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                activeIcon.rectTransform.anchoredPosition = new Vector2(0f, activeIconCenterY);
                activeIcon.rectTransform.sizeDelta = activeIconSize;
                activeIcon.preserveAspect = true;
                activeIcon.raycastTarget = false;
            }

            if (homeLabelImage != null)
            {
                homeLabelImage.rectTransform.anchorMin = new Vector2(0.5f, 0f);
                homeLabelImage.rectTransform.anchorMax = new Vector2(0.5f, 0f);
                homeLabelImage.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                homeLabelImage.rectTransform.anchoredPosition = new Vector2(0f, activeLabelCenterY);
                homeLabelImage.rectTransform.sizeDelta = new Vector2(118f, 50f);
                homeLabelImage.preserveAspect = true;
                homeLabelImage.raycastTarget = false;
            }

            if (activeSpriteLabel != null)
            {
                activeSpriteLabel.rectTransform.anchorMin = new Vector2(0.5f, 0f);
                activeSpriteLabel.rectTransform.anchorMax = new Vector2(0.5f, 0f);
                activeSpriteLabel.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                activeSpriteLabel.rectTransform.anchoredPosition = new Vector2(0f, activeLabelCenterY);
                activeSpriteLabel.preserveAspect = true;
                activeSpriteLabel.raycastTarget = false;
            }

            NormalizeActiveTextLabel();
        }

        private void RegisterButtons()
        {
            RegisterSelectableTab(storeTab, SelectStore);
            RegisterSelectableTab(homeTab, SelectHome);
            RegisterSelectableTab(cardsTab, SelectQuest);
            RegisterSelectableTab(leaderboardTab, SelectLeaderboard);

            RegisterLockedTab(eventsTab);
        }

        private void RequestPageTransition(NavigationDestination destination, bool loadOnlinePlayers = true)
        {
            requestedDestination = destination;
            if (destination == NavigationDestination.Leaderboard)
                requestedLeaderboardOnlinePlayers = loadOnlinePlayers;

            RefreshHomeTabControls();
            ProcessRequestedPageTransition();
        }

        private void ProcessRequestedPageTransition()
        {
            if (pageTransitionInProgress || !Application.isPlaying)
                return;

            NavigationDestination processingDestination = requestedDestination;
            pageTransitionInProgress = true;

            switch (processingDestination)
            {
                case NavigationDestination.Store:
                    HideQuestPage(() => HideLeaderboardPage(
                        () => CompletePageTransition(processingDestination, ShowStorePage)));
                    break;

                case NavigationDestination.Quest:
                    HideStorePage(() => HideLeaderboardPage(
                        () => CompletePageTransition(processingDestination, ShowQuestPage)));
                    break;

                case NavigationDestination.Leaderboard:
                    bool loadOnlinePlayers = requestedLeaderboardOnlinePlayers;
                    HideStorePage(() => HideQuestPage(() => CompletePageTransition(
                        processingDestination,
                        () => ShowLeaderboardPage(loadOnlinePlayers))));
                    break;

                default:
                    HideStorePage(() => HideLeaderboardPage(
                        () => HideQuestPage(() => CompletePageTransition(processingDestination))));
                    break;
            }
        }

        private void CompletePageTransition(NavigationDestination processedDestination, SimpleCallback showPage = null)
        {
            bool isLatestRequest = requestedDestination == processedDestination;
            if (isLatestRequest)
                showPage?.Invoke();

            pageTransitionInProgress = false;
            RefreshHomeTabControls();

            if (!isLatestRequest)
                ProcessRequestedPageTransition();
        }

        private void ShowStorePage()
        {
            if (!showStorePageOnSelect || !Application.isPlaying)
                return;

            if (FindFirstObjectByType<UIController>(FindObjectsInactive.Include) == null)
                return;

            if (UIController.HasPage<UIStore>())
                UIController.ShowPage<UIStore>();
        }

        private void HideStorePage(SimpleCallback onClosed = null)
        {
            if (!Application.isPlaying)
            {
                onClosed?.Invoke();
                return;
            }

            UIStore storePage = FindFirstObjectByType<UIStore>(FindObjectsInactive.Include);
            if (storePage != null && storePage.IsPageDisplayed)
                UIController.HidePage(storePage, onClosed);
            else
                onClosed?.Invoke();
        }

        private void ShowLeaderboardPage(bool loadOnlinePlayers = true)
        {
            if (!Application.isPlaying)
                return;

            LeaderboardPanelView leaderboardPage = FindFirstObjectByType<LeaderboardPanelView>(FindObjectsInactive.Include);
            leaderboardPage?.Show(loadOnlinePlayers);
        }

        private void ShowQuestPage()
        {
            if (!Application.isPlaying)
                return;

            QuestPanelView questPage = GetQuestPanel();
            if (questPage == null)
            {
                Debug.LogWarning("[LeaderboardBottomNavigationController]: Quest panel prefab is not assigned.");
                return;
            }

            questPage.Show();
        }

        private void HideLeaderboardPage(SimpleCallback onClosed = null)
        {
            if (!Application.isPlaying)
            {
                onClosed?.Invoke();
                return;
            }

            LeaderboardPanelView leaderboardPage = FindFirstObjectByType<LeaderboardPanelView>(FindObjectsInactive.Include);
            if (leaderboardPage != null && leaderboardPage.IsVisible)
                leaderboardPage.Hide(onClosed);
            else
                onClosed?.Invoke();
        }

        private void HideQuestPage(SimpleCallback onClosed = null)
        {
            if (!Application.isPlaying)
            {
                onClosed?.Invoke();
                return;
            }

            QuestPanelView questPage = questPanel;
            if (questPage == null)
                questPage = FindFirstObjectByType<QuestPanelView>(FindObjectsInactive.Include);

            if (questPage != null && questPage.IsVisible)
                questPage.Hide(onClosed);
            else
                onClosed?.Invoke();
        }

        private QuestPanelView GetQuestPanel()
        {
            if (questPanel != null)
                return questPanel;

            UIController uiMainCanvas = FindFirstObjectByType<UIController>();
            if (uiMainCanvas != null)
                questPanel = uiMainCanvas.GetComponentInChildren<QuestPanelView>(true);

            if (questPanel == null)
                Debug.LogWarning("[LeaderboardBottomNavigationController]: Quest panel is missing from UI Main Canvas.");

            return questPanel;
        }

        private void ApplyCanvasSorting()
        {
            Canvas rootCanvas = GetComponentInParent<Canvas>()?.rootCanvas;
            if (rootCanvas != null && rootCanvas.gameObject.name == DedicatedCanvasName)
            {
                ConfigureDedicatedCanvas(rootCanvas.gameObject);

                Canvas nestedCanvas = GetComponent<Canvas>();
                if (nestedCanvas != null)
                {
                    nestedCanvas.overrideSorting = false;
                    nestedCanvas.sortingOrder = 0;
                }

                GraphicRaycaster nestedRaycaster = GetComponent<GraphicRaycaster>();
                if (nestedRaycaster != null)
                    nestedRaycaster.enabled = true;

                return;
            }

            Canvas canvas = GetComponent<Canvas>();
            if (canvas == null)
                canvas = gameObject.AddComponent<Canvas>();

            canvas.overrideSorting = keepAboveStorePage;
            if (keepAboveStorePage)
                canvas.sortingOrder = navigationSortingOrder;

            GraphicRaycaster raycaster = GetComponent<GraphicRaycaster>();
            if (raycaster == null)
                raycaster = gameObject.AddComponent<GraphicRaycaster>();

            raycaster.enabled = true;
        }

        private void EnsureDedicatedCanvasRoot()
        {
            Canvas currentRootCanvas = GetComponentInParent<Canvas>()?.rootCanvas;
            if (currentRootCanvas != null && currentRootCanvas.gameObject.name == DedicatedCanvasName)
            {
                ConfigureDedicatedCanvas(currentRootCanvas.gameObject);
                return;
            }

            GameObject canvasRoot = null;
            Scene scene = gameObject.scene;
            if (scene.IsValid())
            {
                GameObject[] roots = scene.GetRootGameObjects();
                for (int i = 0; i < roots.Length; i++)
                {
                    if (roots[i].name == DedicatedCanvasName)
                    {
                        canvasRoot = roots[i];
                        break;
                    }
                }
            }

            if (canvasRoot == null)
            {
                canvasRoot = new GameObject(
                    DedicatedCanvasName,
                    typeof(RectTransform),
                    typeof(Canvas),
                    typeof(CanvasScaler),
                    typeof(GraphicRaycaster));
                canvasRoot.layer = gameObject.layer;

                if (scene.IsValid())
                    SceneManager.MoveGameObjectToScene(canvasRoot, scene);
            }

            ConfigureDedicatedCanvas(canvasRoot);

            RectTransform rectTransform = (RectTransform)transform;
            rectTransform.SetParent(canvasRoot.transform, false);
            rectTransform.anchorMin = new Vector2(0f, 0f);
            rectTransform.anchorMax = new Vector2(1f, 0f);
            rectTransform.pivot = new Vector2(0.5f, 0f);
            rectTransform.anchoredPosition = new Vector2(0f, 12f);
            rectTransform.sizeDelta = new Vector2(0f, normalActiveCardSize.y);
            rectTransform.localScale = Vector3.one;
            rectTransform.localRotation = Quaternion.identity;
            GetComponent<BottomNavigationBannerSpacer>()?.CaptureBasePosition();
        }

        private void ConfigureDedicatedCanvas(GameObject canvasRoot)
        {
            Canvas canvas = canvasRoot.GetComponent<Canvas>();
            if (canvas == null)
                canvas = canvasRoot.AddComponent<Canvas>();

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = navigationSortingOrder;

            CanvasScaler scaler = canvasRoot.GetComponent<CanvasScaler>();
            if (scaler == null)
                scaler = canvasRoot.AddComponent<CanvasScaler>();

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(828f, 1792f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;

            GraphicRaycaster raycaster = canvasRoot.GetComponent<GraphicRaycaster>();
            if (raycaster == null)
                raycaster = canvasRoot.AddComponent<GraphicRaycaster>();

            raycaster.enabled = true;

            dedicatedCanvasGroup = canvasRoot.GetComponent<CanvasGroup>();
            if (dedicatedCanvasGroup == null)
                dedicatedCanvasGroup = canvasRoot.AddComponent<CanvasGroup>();
        }

        private void UpdateLoadingVisibility()
        {
            if (!IsBottomNavigationVisibleAllowed())
                ApplyBottomNavigationVisibility(false);
        }

        private void RefreshBottomNavigationVisibility()
        {
            bool visible = IsBottomNavigationVisibleAllowed();
            ApplyBottomNavigationVisibility(visible);

            if (visible)
                RefreshHomeTabControls();
        }

        private void ApplyBottomNavigationVisibility(bool visible)
        {
            if (dedicatedCanvasGroup == null)
                dedicatedCanvasGroup = GetComponentInParent<Canvas>()?.rootCanvas?.GetComponent<CanvasGroup>();

            if (dedicatedCanvasGroup == null)
                return;

            float targetAlpha = visible ? 1f : 0f;
            if (Mathf.Approximately(dedicatedCanvasGroup.alpha, targetAlpha)
                && dedicatedCanvasGroup.interactable == visible
                && dedicatedCanvasGroup.blocksRaycasts == visible)
            {
                return;
            }

            dedicatedCanvasGroup.alpha = targetAlpha;
            dedicatedCanvasGroup.interactable = visible;
            dedicatedCanvasGroup.blocksRaycasts = visible;
        }

        private bool IsNavigationAllowedOnCurrentScreen()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid() || activeScene.name != "Menu")
                return false;

            LoadingGraphics loadingGraphics = FindFirstObjectByType<LoadingGraphics>(FindObjectsInactive.Exclude);
            if (loadingGraphics != null)
                return false;

            return true;
        }

        private bool IsBottomNavigationVisibleAllowed()
        {
            return IsNavigationAllowedOnCurrentScreen() && !HasBlockingPopup();
        }

        private bool HasBlockingPopup()
        {
            if (UIController.HasOpenPopupWindow)
                return true;

            if (UIRewardsConfirmation.IsRewardPopupVisible)
                return true;

            if (UIProfilePopup.IsProfilePopupVisible)
                return true;

            if (DailyRewardPopupView.IsPopupVisible)
                return true;

            return noConnectionPopup != null && noConnectionPopup.activeInHierarchy;
        }

        private void OnPopupWindowStateChanged(IPopupWindow popupWindow, bool state)
        {
            RefreshBottomNavigationVisibility();
        }

        private void OnStandalonePopupVisibilityChanged(bool visible)
        {
            RefreshBottomNavigationVisibility();
        }

        private void OnPurchaseCompleted(string _)
        {
            RefreshHomeTabControls();

            if (!isActiveAndEnabled)
                return;

            if (homeControlsRefreshRoutine != null)
                StopCoroutine(homeControlsRefreshRoutine);

            homeControlsRefreshRoutine = StartCoroutine(RefreshHomeTabControlsNextFrame());
        }

        private IEnumerator RefreshHomeTabControlsNextFrame()
        {
            yield return null;

            if (isActiveAndEnabled)
                RefreshHomeTabControls();

            homeControlsRefreshRoutine = null;
        }

        private void RegisterSelectableTab(Transform tab, UnityEngine.Events.UnityAction callback)
        {
            Button button = EnsureButton(tab);
            if (button == null)
                return;

            button.interactable = true;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(callback);
            UIAudioFeedback.RegisterButton(button);
            UIHapticFeedback.RegisterButton(button);
        }

        private void RegisterLockedTab(Transform tab)
        {
            Button button = EnsureButton(tab);
            if (button == null)
                return;

            button.interactable = false;
            button.onClick.RemoveAllListeners();
        }

        private Button EnsureButton(Transform tab)
        {
            if (tab == null)
                return null;

            Image hitArea = tab.GetComponent<Image>();
            if (hitArea == null)
                hitArea = tab.gameObject.AddComponent<Image>();

            hitArea.color = new Color(1f, 1f, 1f, 0f);
            hitArea.raycastTarget = true;

            Button button = tab.GetComponent<Button>();
            if (button == null)
                button = tab.gameObject.AddComponent<Button>();

            button.targetGraphic = hitArea;
            return button;
        }

        private void SelectTab(Transform selectedTab, float anchorX, Vector2 activeFrameSize, Sprite iconSprite,
            string labelText, bool useHomeLabelSprite, bool animate)
        {
            if (activeCard == null || activeIcon == null || iconSprite == null)
                return;

            activeIcon.sprite = iconSprite;
            activeIcon.SetNativeSize();
            activeIcon.rectTransform.sizeDelta = activeIconSize;
            activeIcon.preserveAspect = true;

            if (homeLabelImage != null)
                homeLabelImage.gameObject.SetActive(useHomeLabelSprite);

            if (activeSpriteLabel != null)
                activeSpriteLabel.gameObject.SetActive(false);

            if (activeTextLabel != null)
            {
                bool showTextLabel = !useHomeLabelSprite && !string.IsNullOrEmpty(labelText);
                activeTextLabel.gameObject.SetActive(showTextLabel);
                if (showTextLabel)
                {
                    NormalizeActiveTextLabel();
                    activeTextLabel.text = labelText;
                }
            }

            SetTabIconVisible(storeTab, selectedTab != storeTab);
            SetTabIconVisible(homeTab, selectedTab != homeTab);
            SetTabIconVisible(cardsTab, selectedTab != cardsTab);
            SetTabIconVisible(leaderboardTab, selectedTab != leaderboardTab);
            SetTabIconVisible(eventsTab, true);

            if (tabSwitchRoutine != null)
            {
                StopCoroutine(tabSwitchRoutine);
                tabSwitchRoutine = null;
            }

            if (animate && isActiveAndEnabled && tabSwitchDuration > 0f)
                tabSwitchRoutine = StartCoroutine(AnimateActiveCardFrame(anchorX, activeFrameSize));
            else
                ApplyActiveCardFrame(anchorX, activeFrameSize);
        }

        private void SetHomeTabControlsVisible(bool visible)
        {
            UIMainMenu mainMenu = FindFirstObjectByType<UIMainMenu>(FindObjectsInactive.Include);
            mainMenu?.SetHomeTabControlsVisible(visible);

            ProgressPopupToggle progressPopupToggle = FindFirstObjectByType<ProgressPopupToggle>(FindObjectsInactive.Include);
            progressPopupToggle?.SetHomeTabVisible(visible);
        }

        private void RefreshHomeTabControls()
        {
            SetHomeTabControlsVisible(requestedDestination == NavigationDestination.Home);
        }

        private void ApplyActiveCardFrame(float anchorX, Vector2 frameSize)
        {
            activeCard.anchorMin = new Vector2(anchorX, 0f);
            activeCard.anchorMax = new Vector2(anchorX, 0f);

            if (frameSize.x > 0f && frameSize.y > 0f)
                activeCard.sizeDelta = frameSize;
            NormalizeSelectedTabBackground(activeCard.sizeDelta);

            float offsetX = 0f;
            RectTransform parentRect = activeCard.parent as RectTransform;
            if (parentRect != null)
            {
                float parentWidth = parentRect.rect.width;
                float halfWidth = activeCard.sizeDelta.x * 0.5f;
                float centerX = anchorX * parentWidth;
                float overflowLeft = halfWidth - centerX;
                float overflowRight = centerX + halfWidth - parentWidth;

                if (overflowLeft > 0f)
                    offsetX = overflowLeft;
                else if (overflowRight > 0f)
                    offsetX = -overflowRight;
            }

            activeCard.anchoredPosition = new Vector2(offsetX, 0f);
        }

        private IEnumerator AnimateActiveCardFrame(float targetAnchorX, Vector2 targetFrameSize)
        {
            float startAnchorX = activeCard.anchorMin.x;
            Vector2 startOffset = activeCard.anchoredPosition;
            Vector2 startSize = activeCard.sizeDelta;
            Vector2 targetOffset = GetActiveCardOffset(targetAnchorX, targetFrameSize);
            float elapsed = 0f;

            while (elapsed < tabSwitchDuration)
            {
                float linearTime = Mathf.Clamp01(elapsed / tabSwitchDuration);
                float curveTime = tabSwitchCurve != null
                    ? tabSwitchCurve.Evaluate(linearTime)
                    : linearTime;

                float anchorX = Mathf.Lerp(startAnchorX, targetAnchorX, curveTime);
                activeCard.anchorMin = new Vector2(anchorX, 0f);
                activeCard.anchorMax = new Vector2(anchorX, 0f);
                activeCard.sizeDelta = Vector2.Lerp(startSize, targetFrameSize, curveTime);
                NormalizeSelectedTabBackground(activeCard.sizeDelta);
                activeCard.anchoredPosition = Vector2.Lerp(startOffset, targetOffset, curveTime);
                yield return null;
                elapsed += Time.unscaledDeltaTime;
            }

            ApplyActiveCardFrame(targetAnchorX, targetFrameSize);
            tabSwitchRoutine = null;
        }

        private Vector2 GetActiveCardOffset(float anchorX, Vector2 frameSize)
        {
            float offsetX = 0f;
            RectTransform parentRect = activeCard.parent as RectTransform;
            if (parentRect != null)
            {
                float parentWidth = parentRect.rect.width;
                float halfWidth = frameSize.x * 0.5f;
                float centerX = anchorX * parentWidth;
                float overflowLeft = halfWidth - centerX;
                float overflowRight = centerX + halfWidth - parentWidth;

                if (overflowLeft > 0f)
                    offsetX = overflowLeft;
                else if (overflowRight > 0f)
                    offsetX = -overflowRight;
            }

            return new Vector2(offsetX, 0f);
        }

        private void NormalizeSelectedTabBackground(Vector2 frameSize)
        {
            if (selectedTabBackground == null)
                return;

            selectedTabBackground.anchorMin = new Vector2(0.5f, 0f);
            selectedTabBackground.anchorMax = new Vector2(0.5f, 0f);
            selectedTabBackground.pivot = new Vector2(0.5f, 0f);
            selectedTabBackground.anchoredPosition = Vector2.zero;
            float width = frameSize.x > 0f ? frameSize.x : selectedTabBackgroundSize.x;
            selectedTabBackground.sizeDelta = new Vector2(width, selectedTabBackgroundSize.y);

            Image backgroundImage = selectedTabBackground.GetComponent<Image>();
            if (backgroundImage != null)
                backgroundImage.raycastTarget = false;
        }

        private static Image GetTabIcon(Transform tab)
        {
            return tab?.Find("Icon")?.GetComponent<Image>();
        }

        private static void SetTabIconVisible(Transform tab, bool visible)
        {
            Image icon = GetTabIcon(tab);
            if (icon != null)
                icon.gameObject.SetActive(visible);
        }

        private void NormalizeActiveTextLabel()
        {
            if (activeTextLabel == null)
                return;

            RectTransform rectTransform = activeTextLabel.rectTransform;
            rectTransform.anchorMin = new Vector2(0.5f, 0f);
            rectTransform.anchorMax = new Vector2(0.5f, 0f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(0f, activeLabelCenterY);
            rectTransform.sizeDelta = activeTextLabelSize;

            activeTextLabel.alignment = TextAlignmentOptions.Center;
            activeTextLabel.enableAutoSizing = false;
            activeTextLabel.fontSize = activeTextFontSize;
            activeTextLabel.fontStyle = FontStyles.Bold;
            activeTextLabel.color = activeTextColor;
            EnsureActiveTextLabelMaterial();
            if (activeTextLabel.fontSharedMaterial != null)
            {
                activeTextLabel.outlineColor = activeTextOutlineColor;
                activeTextLabel.outlineWidth = 0.22f;
            }
            activeTextLabel.raycastTarget = false;
        }

        private void EnsureActiveTextLabelMaterial()
        {
            if (activeTextLabel == null)
                return;

            if (activeTextLabel.font == null)
                activeTextLabel.font = TMP_Settings.defaultFontAsset ?? Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

            if (activeTextLabel.fontSharedMaterial == null && activeTextLabel.font != null)
                activeTextLabel.fontSharedMaterial = activeTextLabel.font.material;
        }
    }
}
