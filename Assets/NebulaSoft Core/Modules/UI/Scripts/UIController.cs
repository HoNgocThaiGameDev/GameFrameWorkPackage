using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NebulaSoft
{

    [StaticUnload]
    public class UIController : MonoBehaviour
    {
        private static UIController uiController;

        [SerializeField] FloatingCloud currencyCloud;
        [SerializeField] NotchSaveArea notchSaveArea;
        [SerializeField] CachedPages cachedPages;

        [Space]
        [SerializeField] bool usePausePopups = true;
        
        private List<UIPage> pages;
        public List<UIPage> Pages => pages;

        private Dictionary<Type, UIPage> pagesLink = new Dictionary<Type, UIPage>();
        public Dictionary<Type, UIPage> PagesLink => pagesLink;

        private List<IPopupWindow> popupWindows;
        public static bool IsPopupOpened
        {
            get
            {
                if(uiController == null)
                {
                    Debug.LogError("[UIController]: Controller is not initialized. Make sure the component exists in the scene and that the Init and InitPages methods have been called!");

                    return false;
                }

                return !uiController.popupWindows.IsNullOrEmpty();
            }
        }

        public static bool HasOpenPopupWindow
        {
            get
            {
                return uiController != null && !uiController.popupWindows.IsNullOrEmpty();
            }
        }

        private List<IPausePopup> pausePopups = new List<IPausePopup>();
        public List<IPausePopup> PausePopups => pausePopups;

        public bool IsPausePopupOpened => !pausePopups.IsNullOrEmpty();

        private bool isTablet;
        public static bool IsTablet 
        { 
            get
            {
                if(uiController == null)
                    return UIUtils.IsWideScreen(Camera.main);

                return uiController.isTablet;
            }
        }

        private static Canvas mainCanvas;
        public static Canvas MainCanvas => mainCanvas;
        public static CanvasScaler CanvasScaler { get; private set; }
        public static float ScaleFactor { get; private set; } = 1.0f;

        private static Camera mainCamera;

        private SimpleCallback localPageClosedCallback;

        public static event PageCallback PageOpened;
        public static event PageCallback PageClosed;

        public static event PopupWindowCallback PopupOpened;
        public static event PopupWindowCallback PopupClosed;

        public void Init()
        {
            uiController = this;

            mainCanvas = GetComponent<Canvas>();
            CanvasScaler = GetComponent<CanvasScaler>();

            mainCamera = Camera.main;
            isTablet = UIUtils.IsWideScreen(mainCamera);

            CanvasScaler.matchWidthOrHeight = isTablet ? 1 : 0;

            popupWindows = new List<IPopupWindow>();
            pausePopups = new List<IPausePopup>();

            pages = new List<UIPage>();
            pagesLink = new Dictionary<Type, UIPage>();
            for (int i = 0; i < transform.childCount; i++)
            {
                UIPage uiPage = transform.GetChild(i).GetComponent<UIPage>();
                if(uiPage != null)
                {
                    uiPage.CacheComponents();

                    if(pagesLink.ContainsKey(uiPage.GetType()))
                    {
                        Debug.LogError($"[UI Controller] Page {uiPage.GetType()} is already added to the UIController. Please remove the duplicate object to resolve this issue.", uiPage);

                        continue;
                    }

                    pagesLink.Add(uiPage.GetType(), uiPage);

                    pages.Add(uiPage);
                }
            }

            cachedPages.Init(this);
        }

        private void RecalculateScaleFactor(CanvasScaler canvasScaler)
        {
            Vector2 refRes = CanvasScaler.referenceResolution;
            float match = CanvasScaler.matchWidthOrHeight;

            float screenRatio = (float)Screen.width / Screen.height;
            float targetRatio = refRes.x / refRes.y;

            if (screenRatio > targetRatio)
            {
                ScaleFactor = Screen.height / refRes.y;
            }
            else
            {
                ScaleFactor = Screen.width / refRes.x;
            }
        }

        public void InitPages()
        {
            RecalculateScaleFactor(CanvasScaler);

            // Refresh notch save area
            notchSaveArea.Init(new Vector2(Screen.width / ScaleFactor, Screen.height / ScaleFactor));

            // Initialize currency cloud
            currencyCloud.Init();

            for (int i = 0; i < pages.Count; i++)
            {
                pages[i].PreparePage();
                pages[i].Init();
            }
        }

        public static void ResetPages()
        {
            UIController controller = uiController;
            if (controller != null)
            {
                List<UIPage> pages = controller.pages;
                for (int i = 0; i < pages.Count; i++)
                {
                    if (pages[i].IsPageDisplayed)
                    {
                        pages[i].Unload();
                    }
                }
            }
        }

        public static void ShowPage<T>() where T : UIPage
        {
            if(uiController == null)
            {
                Debug.LogError("[UIController]: Controller is not initialized. Make sure the component exists in the scene and that the Init and InitPages methods have been called!");

                return;
            }

            UIPage page = null;
            Type pageType = typeof(T);

            Dictionary<Type, UIPage> pagesLink = uiController.pagesLink;
            List<UIPage> pages = uiController.pages;

            if(!pagesLink.ContainsKey(pageType))
            {
                if(!uiController.cachedPages.HasPage(pageType))
                {
                    Debug.LogError($"[UIController]: Page {pageType} not found in the scene and not linked as a cached page. Ensure it’s instantiated or registered before showing.");

                    return;
                }

                page = uiController.cachedPages.InstantiateCachedPage(pageType);
            }
            else
            {
                page = pagesLink[pageType];
            }

            if (!page.IsPageDisplayed)
            {
                page.GraphicRaycaster.enabled = true;
                page.PlayShowAnimation();
                page.EnableCanvas();
            }
        }

        public static void ShowPage(UIPage page)
        {
            if (uiController == null)
            {
                Debug.LogError("[UIController]: Controller is not initialized. Make sure the component exists in the scene and that the Init and InitPages methods have been called!");

                return;
            }

            if(page == null)
            {
                Debug.LogError("[UIController]: Provided page reference cannot be null!");

                return;
            }

            if (!page.IsPageDisplayed)
            {
                page.GraphicRaycaster.enabled = true;
                page.PlayShowAnimation();
                page.EnableCanvas();
            }
        }

        public static void HidePage<T>(SimpleCallback onPageClosed = null) where T : UIPage
        {
            if (uiController == null)
            {
                Debug.LogError("[UIController]: Controller is not initialized. Make sure the component exists in the scene and that the Init and InitPages methods have been called!");

                onPageClosed?.Invoke();

                return;
            }

            Dictionary<Type, UIPage> pagesLink = uiController.pagesLink;
            Type pageType = typeof(T);

            if (!pagesLink.ContainsKey(pageType))
            {
                Debug.LogError($"[UIController]: Page type {pageType.Name} was not found in the registered pages!");

                onPageClosed?.Invoke();

                return;
            }

            UIPage page = pagesLink[pageType];
            if (!page.IsPageDisplayed)
            {
                onPageClosed?.Invoke();

                return;
            }

            uiController.localPageClosedCallback = onPageClosed;

            page.GraphicRaycaster.enabled = false;
            page.PlayHideAnimation();
        }

        public static void HidePage(UIPage page, SimpleCallback onPageClosed = null)
        {
            if (uiController == null)
            {
                Debug.LogError("[UIController]: Controller is not initialized. Make sure the component exists in the scene and that the Init and InitPages methods have been called!");

                onPageClosed?.Invoke();

                return;
            }

            if (page == null)
            {
                Debug.LogError("[UIController]: Provided page reference cannot be null!");

                onPageClosed?.Invoke();

                return;
            }

            if (!page.IsPageDisplayed)
            {
                onPageClosed?.Invoke();

                return;
            }

            uiController.localPageClosedCallback = onPageClosed;

            page.GraphicRaycaster.enabled = false;
            page.PlayHideAnimation();
        }

        public static void DisablePage<T>()
        {
            if (uiController == null)
            {
                Debug.LogError("[UIController]: Controller is not initialized. Make sure the component exists in the scene and that the Init and InitPages methods have been called!");

                return;
            }

            Dictionary<Type, UIPage> pagesLink = uiController.pagesLink;
            Type pageType = typeof(T);

            if (!pagesLink.ContainsKey(pageType))
            {
                Debug.LogError($"[UIController]: Page type {pageType.Name} was not found in the registered pages!");

                return;
            }

            UIPage page = pagesLink[pageType];
            if (page.IsPageDisplayed)
            {
                page.DisableCanvas();

                OnPageClosed(page);
            }
        }

        public static bool IsDisplayed<T>() where T : UIPage
        {
            if (uiController == null)
            {
                Debug.LogError("[UIController]: Controller is not initialized. Make sure the component exists in the scene and that the Init and InitPages methods have been called!");

                return false;
            }

            Dictionary<Type, UIPage> pagesLink = uiController.pagesLink;
            Type pageType = typeof(T);

            if (!pagesLink.ContainsKey(pageType))
            {
                Debug.LogError($"[UIController]: Page type {pageType.Name} was not found in the registered pages!");

                return false;
            }

            return pagesLink[pageType].IsPageDisplayed;
        }

        public static void OnPageClosed(UIPage page)
        {
            page.DisableCanvas();

            PageClosed?.Invoke(page, page.GetType());

            if (page is IPopupWindow popup)
            {
                if (uiController.popupWindows.Contains(popup))
                {
                    uiController.popupWindows.Remove(popup);

                    if (popup is IPausePopup pausePopup)
                    {
                        uiController.pausePopups.Remove(pausePopup);

                        if (uiController.pausePopups.Count == 0)
                        {
                            if (uiController.usePausePopups)
                                Time.timeScale = 1.0f;
                        }
                    }

                    PopupClosed?.Invoke(popup, false);
                }
            }

            if (uiController.localPageClosedCallback != null)
            {
                uiController.localPageClosedCallback.Invoke();
                uiController.localPageClosedCallback = null;
            }

            if(page.IsCached)
            {
                Type pageType = page.GetType();

                CachedPages.PageData data = uiController.cachedPages.GetPageData(pageType);
                if (data != null)
                {
                    if(data.AutoDestroy)
                    {
                        uiController.pagesLink.Remove(pageType);
                        uiController.pages.Remove(page);

                        Destroy(page.gameObject);
                    }
                }
            }
        }

        public static void OnPageOpened(UIPage page)
        {
            PageOpened?.Invoke(page, page.GetType());

            UIAudioFeedback.RegisterButtons(page.transform);
            UIHapticFeedback.RegisterButtons(page.transform);

            if(page is IPopupWindow popup)
            {
                if (!uiController.popupWindows.Contains(popup))
                {
                    uiController.popupWindows.Add(popup);

                    if (popup is IPausePopup pausePopup)
                    {
                        uiController.pausePopups.Add(pausePopup);

                        if (uiController.usePausePopups)
                            Time.timeScale = 0.0f;
                    }

                    PopupOpened?.Invoke(popup, true);
                }
            }
        }

        [Obsolete("Use only OnPageOpened method")]
        public static void OnPopupWindowOpened(IPopupWindow popupWindow)
        {
            // Will be removed in the next update
            // All logic has been moved to the OnPageOpened method
        }

        [Obsolete("Use only OnPageClosed method")]
        public static void OnPopupWindowClosed(IPopupWindow popupWindow)
        {
            // Will be removed in the next update
            // All logic has been moved to the OnPageClosed method
        }

        public static T GetPage<T>() where T : UIPage
        {
            if (uiController == null)
            {
                Debug.LogError("[UIController]: Controller is not initialized. Make sure the component exists in the scene and that the Init and InitPages methods have been called!");

                return null;
            }

            UIPage page = null;
            Type pageType = typeof(T);

            Dictionary<Type, UIPage> pagesLink = uiController.pagesLink;
            List<UIPage> pages = uiController.pages;

            if (!pagesLink.ContainsKey(pageType))
            {
                if (!uiController.cachedPages.HasPage(pageType))
                {
                    Debug.LogError($"[UIController]: Page {pageType} not found in the scene and not linked as a cached page. Ensure it’s instantiated or registered before showing.");

                    return null;
                }

                page = uiController.cachedPages.InstantiateCachedPage(pageType);
            }
            else
            {
                page = pagesLink[pageType];
            }

            return (T)page;
        }

        public static bool HasPage<T>() where T : UIPage
        {
            if (uiController == null)
            {
                Debug.LogError("[UIController]: Controller is not initialized. Make sure the component exists in the scene and that the Init and InitPages methods have been called!");

                return false;
            }

            Type pageType = typeof(T);

            if (uiController.pagesLink.ContainsKey(pageType))
                return true;

            if (uiController.cachedPages.HasPage(pageType))
                return true;

            return false;
        }

        public static Vector3 FixUIElementToWorld(Transform target, Vector3 offset)
        {
            Vector3 targPos = target.transform.position + offset;
            Vector3 camForward = mainCamera.transform.forward;

            float distInFrontOfCamera = Vector3.Dot(targPos - (mainCamera.transform.position + camForward), camForward);
            if (distInFrontOfCamera < 0f)
            {
                targPos -= camForward * distInFrontOfCamera;
            }

            return RectTransformUtility.WorldToScreenPoint(mainCamera, targPos);
        }

        public static TweenCase WaitForPopupsClose(SimpleCallback callback)
        {
            UIController owner = uiController;
            if (owner == null || owner.popupWindows.IsNullOrEmpty())
            {
                callback?.Invoke();

                return null;
            }

            return Tween.DoWaitForCondition(waitTween =>
            {
                if (owner == null || uiController != owner)
                {
                    waitTween.Kill();
                    return;
                }
                if (owner.popupWindows.IsNullOrEmpty())
                {
                    waitTween.Kill();
                    callback?.Invoke();
                }
            }, unscaledTime: true);
        }

        private void OnDestroy()
        {
            FloatingCloud.Clear();

            if(usePausePopups)
                Time.timeScale = 1.0f;
        }

        private static void UnloadStatic()
        {
            PageOpened = null;
            PageClosed = null;

            PopupOpened = null;
            PopupClosed = null;
        }

        public delegate void PageCallback(UIPage page, Type pageType);
        public delegate void PopupWindowCallback(IPopupWindow popupWindow, bool state);
    }
}
