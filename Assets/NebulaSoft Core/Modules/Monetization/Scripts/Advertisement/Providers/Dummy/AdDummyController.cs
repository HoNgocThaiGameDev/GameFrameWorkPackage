#pragma warning disable 0649

using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using NebulaSoft.Ads;

namespace NebulaSoft
{
    public class AdDummyController : MonoBehaviour
    {
        public static event System.Action NoAdsOfferRequested;
        public event System.Action InterstitialClosed;
        public event System.Action<bool> RewardedClosed;
        public event System.Action<BannerInfo> BannerGeometryChanged;
        private bool usesProviderEvents;
        private GameObject bannerObject;
        private Button bannerCloseButton;

        private GameObject interstitialObject;
        private Button interstitialCloseButton;

        private GameObject rewardedVideoObject;
        private Button rewardedVideoCloseButton;
        private Button rewardedVideoRewardButton;

        private RectTransform bannerRectTransform;
        private Vector2Int lastScreenSize;
        private ScreenOrientation lastOrientation;
        private float lastBannerScreenHeight = -1f;

        public void Init(AdsSettings settings)
        {
            Init(settings.DummyContainer.BannerPosition, false);
        }

        public void Init(BannerPosition position)
        {
            Init(position, true);
        }

        private void Init(BannerPosition position, bool providerEvents)
        {
            usesProviderEvents = providerEvents;
            bannerRectTransform = (RectTransform)bannerObject.transform;

            bannerCloseButton.AddEvent(EventTriggerType.PointerDown, (data) => OnBannerCloseButtonClicked());
            interstitialCloseButton.AddEvent(EventTriggerType.PointerDown, (data) => OnInterstitialCloseButtonClicked());
            rewardedVideoCloseButton.AddEvent(EventTriggerType.PointerDown, (data) => OnRewardedVideoCloseButtonClicked());
            rewardedVideoRewardButton.AddEvent(EventTriggerType.PointerDown, (data) => OnRewardedVideoRewardButtonClicked());
            UIAudioFeedback.RegisterButtons(transform);

            // Toggle editor visibility
            gameObject.ToggleVisibility(true);
            gameObject.TogglePicking(true);

            DontDestroyOnLoad(gameObject);

            switch (position)
            {
                case BannerPosition.Bottom:
                    bannerRectTransform.pivot = new Vector2(0.5f, 0.0f);

                    bannerRectTransform.anchorMin = new Vector2(0.0f, 0.0f);
                    bannerRectTransform.anchorMax = new Vector2(1.0f, 0.0f);

                    bannerRectTransform.anchoredPosition = Vector2.zero;
                    break;
                case BannerPosition.Top:
                    bannerRectTransform.pivot = new Vector2(0.5f, 1.0f);

                    bannerRectTransform.anchorMin = new Vector2(0.0f, 1.0f);
                    bannerRectTransform.anchorMax = new Vector2(1.0f, 1.0f);

                    bannerRectTransform.anchoredPosition = Vector2.zero;
                    break;
            }
        }

        public void ShowBanner()
        {
            if (bannerObject == null)
                return;

            bannerObject.SetActive(true);

            Canvas.ForceUpdateCanvases();
            RefreshBannerHeight(forceRefresh: true);
        }

        public float GetBannerScreenHeight()
        {
            if (bannerRectTransform == null)
                return 0f;

            Vector3[] corners = new Vector3[4];
            bannerRectTransform.GetWorldCorners(corners);

            Camera camera = null;
            Canvas canvas = bannerRectTransform.GetComponentInParent<Canvas>();
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                camera = canvas.worldCamera;

            float bottom = RectTransformUtility.WorldToScreenPoint(camera, corners[0]).y;
            float top = RectTransformUtility.WorldToScreenPoint(camera, corners[1]).y;

            return Mathf.Abs(top - bottom);
        }

        public void HideBanner()
        {
            if (bannerObject == null)
                return;

            bannerObject.SetActive(false);
            if (usesProviderEvents) BannerGeometryChanged?.Invoke(BannerInfo.Hidden);
        }

        private void LateUpdate()
        {
            if (bannerObject == null || !bannerObject.activeInHierarchy)
                return;

            RefreshBannerHeight(forceRefresh: false);
        }

        private void RefreshBannerHeight(bool forceRefresh)
        {
            Vector2Int screenSize = new Vector2Int(Screen.width, Screen.height);
            float bannerScreenHeight = GetBannerScreenHeight();
            bool layoutChanged = forceRefresh ||
                                 screenSize != lastScreenSize ||
                                 Screen.orientation != lastOrientation ||
                                 !Mathf.Approximately(bannerScreenHeight, lastBannerScreenHeight);

            if (!layoutChanged)
                return;

            lastScreenSize = screenSize;
            lastOrientation = Screen.orientation;
            lastBannerScreenHeight = bannerScreenHeight;

            if (usesProviderEvents)
            {
                Vector3[] corners = new Vector3[4];
                bannerRectTransform.GetWorldCorners(corners);
                Canvas canvas = bannerRectTransform.GetComponentInParent<Canvas>();
                Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
                Vector2 bottom = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
                Vector2 top = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
                BannerGeometryChanged?.Invoke(new BannerInfo("Dummy", Mathf.RoundToInt(Mathf.Abs(top.x - bottom.x)),
                    Mathf.RoundToInt(bannerScreenHeight), Mathf.Min(bottom.y, top.y)));
            }
            else AdsManager.SetBannerHeight(bannerScreenHeight);
        }

        public void ShowInterstitial()
        {
            interstitialObject.SetActive(true);
        }

        public void CloseInterstitial()
        {
            interstitialObject.SetActive(false);

            if (usesProviderEvents) InterstitialClosed?.Invoke();
            else AdsManager.OnProviderAdClosed(AdProvider.Dummy, AdType.Interstitial);
        }

        public void ShowRewardedVideo()
        {
            rewardedVideoObject.SetActive(true);
        }

        public void CloseRewardedVideo()
        {
            rewardedVideoObject.SetActive(false);

            if (!usesProviderEvents) AdsManager.OnProviderAdClosed(AdProvider.Dummy, AdType.RewardedVideo);
        }

        #region Buttons
        public void OpenNoAdsOfferButton()
        {
            HideBanner();
            NoAdsOfferRequested?.Invoke();
        }

        private void OnBannerCloseButtonClicked()
        {
            UIAudioFeedback.PlayButtonSound();
            OpenNoAdsOfferButton();
        }

        public void CloseInterstitialButton()
        {
            if (!usesProviderEvents) AdsManager.ExecuteInterstitialCallback(true);

            CloseInterstitial();
        }

        public void CloseRewardedVideoButton()
        {
            CloseRewardedVideo();
            if (usesProviderEvents) RewardedClosed?.Invoke(false);
            else AdsManager.ExecuteRewardVideoCallback(false);
        }

        public void GetRewardButton()
        {
            CloseRewardedVideo();
            if (usesProviderEvents) RewardedClosed?.Invoke(true);
            else AdsManager.ExecuteRewardVideoCallback(true);
        }

        private void OnInterstitialCloseButtonClicked()
        {
            UIAudioFeedback.PlayButtonSound();
            CloseInterstitialButton();
        }

        private void OnRewardedVideoCloseButtonClicked()
        {
            UIAudioFeedback.PlayButtonSound();
            CloseRewardedVideoButton();
        }

        private void OnRewardedVideoRewardButtonClicked()
        {
            UIAudioFeedback.PlayButtonSound();
            GetRewardButton();
        }
        #endregion

        public static AdDummyController CreateObject()
        {
            Color backgroundColor = new Color(0.211f, 0.211f, 0.321f, 1.0f);
            Color mainColor = new Color(0.207f, 0.305f, 0.717f, 1.0f);

#if UNITY_EDITOR
            backgroundColor = CoreEditor.AdsDummyBackgroundColor;
            mainColor = CoreEditor.AdsDummyMainColor;
#endif

            GameObject go = new GameObject("[ADS DUMMY CANVAS]");

            Canvas canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999;

            CanvasScaler canvasScaler = go.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1080, 1920);
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            canvasScaler.MatchSize();

            GraphicRaycaster graphicRaycaster = go.AddComponent<GraphicRaycaster>();
            graphicRaycaster.ignoreReversedGraphics = true;
            graphicRaycaster.blockingObjects = GraphicRaycaster.BlockingObjects.None;

            // Banner
            GameObject bannerObject = new GameObject("Banner");
            bannerObject.SetActive(false);

            RectTransform bannerRectTransform = bannerObject.AddComponent<RectTransform>();
            bannerRectTransform.SetParent(go.transform);
            bannerRectTransform.anchorMin = new Vector2(0, 1);
            bannerRectTransform.anchorMax = new Vector2(1, 1);
            bannerRectTransform.pivot = new Vector2(0.5f, 1);
            bannerRectTransform.sizeDelta = new Vector2(0, 84);
            bannerRectTransform.anchoredPosition = new Vector2(0, 0);

            Image bannerImage = bannerObject.AddComponent<Image>();
            bannerImage.color = mainColor;

            // Banner Text
            GameObject bannerTextObject = new GameObject("Text");

            RectTransform bannerTextRectTransform = bannerTextObject.AddComponent<RectTransform>();
            bannerTextRectTransform.SetParent(bannerRectTransform);
            bannerTextRectTransform.anchorMin = new Vector2(0, 0);
            bannerTextRectTransform.anchorMax = new Vector2(1, 1);
            bannerTextRectTransform.pivot = new Vector2(0.5f, 0.5f);
            bannerTextRectTransform.sizeDelta = new Vector2(0, 0);
            bannerTextRectTransform.anchoredPosition = new Vector2(0, 0);

            TextMeshProUGUI bannerText = bannerTextObject.AddComponent<TextMeshProUGUI>();
            bannerText.text = "TEST BANNER";
            bannerText.alignment = TextAlignmentOptions.Converted;
            bannerText.horizontalAlignment = HorizontalAlignmentOptions.Center;
            bannerText.verticalAlignment = VerticalAlignmentOptions.Geometry;
            bannerText.fontSize = 30;
            bannerText.fontWeight = FontWeight.Bold;

            // Banner Close Button
            GameObject bannerCloseButtonObject = new GameObject("Close Button");

            RectTransform bannerCloseButtonRectTransform = bannerCloseButtonObject.AddComponent<RectTransform>();
            bannerCloseButtonRectTransform.SetParent(bannerRectTransform);
            bannerCloseButtonRectTransform.anchorMin = new Vector2(0, 1);
            bannerCloseButtonRectTransform.anchorMax = new Vector2(0, 1);
            bannerCloseButtonRectTransform.pivot = new Vector2(0, 1);
            bannerCloseButtonRectTransform.sizeDelta = new Vector2(46, 46);
            bannerCloseButtonRectTransform.anchoredPosition = new Vector2(10, -4);

            Image bannerCloseButtonImage = bannerCloseButtonObject.AddComponent<Image>();
            bannerCloseButtonImage.sprite = Resources.Load<Sprite>("UI/btn_close");
            bannerCloseButtonImage.preserveAspect = true;
            bannerCloseButtonImage.color = Color.white;
            bannerCloseButtonImage.raycastTarget = true;

            Button bannerCloseButton = bannerCloseButtonObject.AddComponent<Button>();
            bannerCloseButton.targetGraphic = bannerCloseButtonImage;

            // Interstitial
            GameObject interstitialObject = new GameObject("Interstitial");
            interstitialObject.SetActive(false);

            RectTransform interstitialRectTransform = interstitialObject.AddComponent<RectTransform>();
            interstitialRectTransform.SetParent(go.transform);
            interstitialRectTransform.anchorMin = new Vector2(0, 0);
            interstitialRectTransform.anchorMax = new Vector2(1, 1);
            interstitialRectTransform.pivot = new Vector2(0.5f, 0.5f);
            interstitialRectTransform.sizeDelta = new Vector2(0, 0);
            interstitialRectTransform.anchoredPosition = new Vector2(0, 0);

            Image interstitialImage = interstitialObject.AddComponent<Image>();
            interstitialImage.color = backgroundColor;

            // Interstitial Title
            GameObject interstitialTitleObject = new GameObject("Title");

            RectTransform interstitialTitleRectTransform = interstitialTitleObject.AddComponent<RectTransform>();
            interstitialTitleRectTransform.SetParent(interstitialRectTransform);
            interstitialTitleRectTransform.anchorMin = new Vector2(0, 0.5f);
            interstitialTitleRectTransform.anchorMax = new Vector2(1, 0.5f);
            interstitialTitleRectTransform.pivot = new Vector2(0.5f, 0.5f);
            interstitialTitleRectTransform.sizeDelta = new Vector2(0, 100);
            interstitialTitleRectTransform.anchoredPosition = new Vector2(0, 200);

            TextMeshProUGUI interstitialText = interstitialTitleObject.AddComponent<TextMeshProUGUI>();
            interstitialText.text = "TEST INTERSTITIAL";
            interstitialText.alignment = TextAlignmentOptions.Converted;
            interstitialText.horizontalAlignment = HorizontalAlignmentOptions.Center;
            interstitialText.verticalAlignment = VerticalAlignmentOptions.Geometry;
            interstitialText.fontSize = 62;
            interstitialText.fontWeight = FontWeight.Bold;

            // Interstitial Close Button
            GameObject interstitialCloseButtonObject = new GameObject("Close Button");

            RectTransform interstitialCloseButtonRectTransform = interstitialCloseButtonObject.AddComponent<RectTransform>();
            interstitialCloseButtonRectTransform.SetParent(interstitialRectTransform);
            interstitialCloseButtonRectTransform.anchorMin = new Vector2(0.5f, 0);
            interstitialCloseButtonRectTransform.anchorMax = new Vector2(0.5f, 0);
            interstitialCloseButtonRectTransform.pivot = new Vector2(0.5f, 0);
            interstitialCloseButtonRectTransform.sizeDelta = new Vector2(600, 200);
            interstitialCloseButtonRectTransform.anchoredPosition = new Vector2(0, 360);

            Image interstitialCloseButtonImage = interstitialCloseButtonObject.AddComponent<Image>();
            interstitialCloseButtonImage.color = mainColor;

            Button interstitialCloseButton = interstitialCloseButtonObject.AddComponent<Button>();
            interstitialCloseButton.targetGraphic = interstitialCloseButtonImage;

            // Interstitial Close Button Text
            GameObject interstitialCloseButtonTextObject = new GameObject("Text");

            RectTransform interstitialCloseButtonTextRectTransform = interstitialCloseButtonTextObject.AddComponent<RectTransform>();
            interstitialCloseButtonTextRectTransform.SetParent(interstitialCloseButtonRectTransform);
            interstitialCloseButtonTextRectTransform.anchorMin = new Vector2(0, 0);
            interstitialCloseButtonTextRectTransform.anchorMax = new Vector2(1, 1);
            interstitialCloseButtonTextRectTransform.pivot = new Vector2(0.5f, 0.5f);
            interstitialCloseButtonTextRectTransform.sizeDelta = new Vector2(0, 0);
            interstitialCloseButtonTextRectTransform.anchoredPosition = new Vector2(0, 0);

            TextMeshProUGUI interstitialCloseButtonText = interstitialCloseButtonTextObject.AddComponent<TextMeshProUGUI>();
            interstitialCloseButtonText.text = "CLOSE";
            interstitialCloseButtonText.alignment = TextAlignmentOptions.Converted;
            interstitialCloseButtonText.horizontalAlignment = HorizontalAlignmentOptions.Center;
            interstitialCloseButtonText.verticalAlignment = VerticalAlignmentOptions.Geometry;
            interstitialCloseButtonText.fontSize = 60;
            interstitialCloseButtonText.fontWeight = FontWeight.Bold;

            // Rewarded Video
            GameObject rvObject = new GameObject("Rewarded Video");
            rvObject.SetActive(false);

            RectTransform rvRectTransform = rvObject.AddComponent<RectTransform>();
            rvRectTransform.SetParent(go.transform);
            rvRectTransform.anchorMin = new Vector2(0, 0);
            rvRectTransform.anchorMax = new Vector2(1, 1);
            rvRectTransform.pivot = new Vector2(0.5f, 0.5f);
            rvRectTransform.sizeDelta = new Vector2(0, 0);
            rvRectTransform.anchoredPosition = new Vector2(0, 0);

            Image rvImage = rvObject.AddComponent<Image>();
            rvImage.color = backgroundColor;

            // RV Title
            GameObject rvTitleObject = new GameObject("Title");

            RectTransform rvTitleRectTransform = rvTitleObject.AddComponent<RectTransform>();
            rvTitleRectTransform.SetParent(rvRectTransform);
            rvTitleRectTransform.anchorMin = new Vector2(0, 0.5f);
            rvTitleRectTransform.anchorMax = new Vector2(1, 0.5f);
            rvTitleRectTransform.pivot = new Vector2(0.5f, 0.5f);
            rvTitleRectTransform.sizeDelta = new Vector2(0, 100);
            rvTitleRectTransform.anchoredPosition = new Vector2(0, 200);

            TextMeshProUGUI rvText = rvTitleObject.AddComponent<TextMeshProUGUI>();
            rvText.text = "TEST REWARDED VIDEO";
            rvText.alignment = TextAlignmentOptions.Converted;
            rvText.horizontalAlignment = HorizontalAlignmentOptions.Center;
            rvText.verticalAlignment = VerticalAlignmentOptions.Geometry;
            rvText.fontSize = 62;
            rvText.fontWeight = FontWeight.Bold;

            // RV Close Button
            GameObject rvCloseButtonObject = new GameObject("Close Button");

            RectTransform rvCloseButtonRectTransform = rvCloseButtonObject.AddComponent<RectTransform>();
            rvCloseButtonRectTransform.SetParent(rvRectTransform);
            rvCloseButtonRectTransform.anchorMin = new Vector2(0.5f, 0);
            rvCloseButtonRectTransform.anchorMax = new Vector2(0.5f, 0);
            rvCloseButtonRectTransform.pivot = new Vector2(0.5f, 0);
            rvCloseButtonRectTransform.sizeDelta = new Vector2(600, 200);
            rvCloseButtonRectTransform.anchoredPosition = new Vector2(0, 360);

            Image rvCloseButtonImage = rvCloseButtonObject.AddComponent<Image>();
            rvCloseButtonImage.color = mainColor;

            Button rvCloseButton = rvCloseButtonObject.AddComponent<Button>();
            rvCloseButton.targetGraphic = rvCloseButtonImage;

            // RV Close Button Text
            GameObject rvCloseButtonTextObject = new GameObject("Text");

            RectTransform rvCloseButtonTextRectTransform = rvCloseButtonTextObject.AddComponent<RectTransform>();
            rvCloseButtonTextRectTransform.SetParent(rvCloseButtonRectTransform);
            rvCloseButtonTextRectTransform.anchorMin = new Vector2(0, 0);
            rvCloseButtonTextRectTransform.anchorMax = new Vector2(1, 1);
            rvCloseButtonTextRectTransform.pivot = new Vector2(0.5f, 0.5f);
            rvCloseButtonTextRectTransform.sizeDelta = new Vector2(0, 0);
            rvCloseButtonTextRectTransform.anchoredPosition = new Vector2(0, 0);

            TextMeshProUGUI rvCloseButtonText = rvCloseButtonTextObject.AddComponent<TextMeshProUGUI>();
            rvCloseButtonText.text = "CLOSE";
            rvCloseButtonText.alignment = TextAlignmentOptions.Converted;
            rvCloseButtonText.horizontalAlignment = HorizontalAlignmentOptions.Center;
            rvCloseButtonText.verticalAlignment = VerticalAlignmentOptions.Geometry;
            rvCloseButtonText.fontSize = 60;
            rvCloseButtonText.fontWeight = FontWeight.Bold;

            // RV Reward Button
            GameObject rvRewardButtonObject = new GameObject("Get Reward Button");

            RectTransform rvRewardButtonRectTransform = rvRewardButtonObject.AddComponent<RectTransform>();
            rvRewardButtonRectTransform.SetParent(rvRectTransform);
            rvRewardButtonRectTransform.anchorMin = new Vector2(0.5f, 0);
            rvRewardButtonRectTransform.anchorMax = new Vector2(0.5f, 0);
            rvRewardButtonRectTransform.pivot = new Vector2(0.5f, 0);
            rvRewardButtonRectTransform.sizeDelta = new Vector2(600, 200);
            rvRewardButtonRectTransform.anchoredPosition = new Vector2(0, 600);

            Image rvRewardButtonImage = rvRewardButtonObject.AddComponent<Image>();
            rvRewardButtonImage.color = mainColor;

            Button rvRewardButton = rvRewardButtonObject.AddComponent<Button>();
            rvRewardButton.targetGraphic = rvRewardButtonImage;

            // RV Reward Button Text
            GameObject rvRewardButtonTextObject = new GameObject("Text");

            RectTransform rvRewardButtonTextRectTransform = rvRewardButtonTextObject.AddComponent<RectTransform>();
            rvRewardButtonTextRectTransform.SetParent(rvRewardButtonRectTransform);
            rvRewardButtonTextRectTransform.anchorMin = new Vector2(0, 0);
            rvRewardButtonTextRectTransform.anchorMax = new Vector2(1, 1);
            rvRewardButtonTextRectTransform.pivot = new Vector2(0.5f, 0.5f);
            rvRewardButtonTextRectTransform.sizeDelta = new Vector2(0, 0);
            rvRewardButtonTextRectTransform.anchoredPosition = new Vector2(0, 0);

            TextMeshProUGUI rvRewardButtonText = rvRewardButtonTextObject.AddComponent<TextMeshProUGUI>();
            rvRewardButtonText.text = "GET REWARD";
            rvRewardButtonText.alignment = TextAlignmentOptions.Converted;
            rvRewardButtonText.horizontalAlignment = HorizontalAlignmentOptions.Center;
            rvRewardButtonText.verticalAlignment = VerticalAlignmentOptions.Geometry;
            rvRewardButtonText.fontSize = 60;
            rvRewardButtonText.fontWeight = FontWeight.Bold;

            AdDummyController controller = go.AddComponent<AdDummyController>();
            controller.bannerObject = bannerObject;
            controller.bannerCloseButton = bannerCloseButton;

            controller.interstitialObject = interstitialObject;
            controller.interstitialCloseButton = interstitialCloseButton;

            controller.rewardedVideoObject = rvObject;
            controller.rewardedVideoCloseButton = rvCloseButton;
            controller.rewardedVideoRewardButton = rvRewardButton;

            return controller;
        }
    }
}
