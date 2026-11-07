using UnityEngine;

namespace NebulaSoft
{
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public class BottomNavigationBannerSpacer : MonoBehaviour
    {
        [SerializeField] float bottomSpacing = 6f;
        [SerializeField] bool refreshSafeAreaOnScreenChange = true;

        private RectTransform rectTransform;
        private RectTransform parentRectTransform;
        private Canvas rootCanvas;
        private Vector2Int lastScreenSize;
        private ScreenOrientation lastOrientation;
        private float lastBannerTop = -1f;
        private float baseAnchoredY;
        private bool basePositionCaptured;

        private void Awake()
        {
            CacheReferences();
        }

        private void OnEnable()
        {
            CacheReferences();

#if MODULE_MONETIZATION
            AdsManager.BannerLayoutChanged -= OnBannerLayoutChanged;
            AdsManager.BannerLayoutChanged += OnBannerLayoutChanged;
#endif
        }

        private void Start()
        {
            CaptureBasePosition();
        }

        private void OnDisable()
        {
#if MODULE_MONETIZATION
            AdsManager.BannerLayoutChanged -= OnBannerLayoutChanged;
#endif
        }

        private void OnTransformParentChanged()
        {
            CacheReferences();
            basePositionCaptured = false;
        }

        private void LateUpdate()
        {
            Apply(forceRefresh: false);
        }

#if MODULE_MONETIZATION
        private void OnBannerLayoutChanged(BannerLayoutInfo layout)
        {
            Apply(forceRefresh: true);
        }
#endif

#if UNITY_EDITOR
        private void OnValidate()
        {
            CacheReferences();

            ApplyPosition();
        }
#endif

        private void Apply(bool forceRefresh)
        {
            float bannerTop = GetBannerTopScreenY();
            Vector2Int screenSize = new Vector2Int(Screen.width, Screen.height);
            bool layoutChanged = forceRefresh ||
                                 screenSize != lastScreenSize ||
                                 Screen.orientation != lastOrientation ||
                                 !Mathf.Approximately(bannerTop, lastBannerTop);

            if (layoutChanged)
            {
                lastScreenSize = screenSize;
                lastOrientation = Screen.orientation;
                lastBannerTop = bannerTop;

                if (refreshSafeAreaOnScreenChange)
                    NotchSaveArea.Refresh(true);
            }

            ApplyPosition();
        }

        private void ApplyPosition()
        {
            if (rectTransform == null || !basePositionCaptured)
                return;

            float targetY = GetTargetAnchoredY();
            Vector2 anchoredPosition = rectTransform.anchoredPosition;
            if (Mathf.Approximately(anchoredPosition.y, targetY))
                return;

            anchoredPosition.y = targetY;
            rectTransform.anchoredPosition = anchoredPosition;
        }

        private void CacheReferences()
        {
            rectTransform = (RectTransform)transform;
            parentRectTransform = rectTransform.parent as RectTransform;
            rootCanvas = GetComponentInParent<Canvas>()?.rootCanvas;
        }

        private float GetTargetAnchoredY()
        {
            if (parentRectTransform == null)
                return baseAnchoredY;

            float parentBottomY = parentRectTransform.rect.yMin;
            float bannerTopScreenY = GetBannerTopScreenY();
            float bannerTopY = GetBannerTopInParentSpace(bannerTopScreenY);
            float spacing = GetBottomSpacingInParentSpace(bannerTopScreenY);

            if (bannerTopY > parentBottomY)
            {
                float directBannerTargetY = bannerTopY + spacing - parentBottomY;
                return baseAnchoredY + Mathf.Max(0f, directBannerTargetY);
            }

            // No banner (including No Ads): occupy the released banner slot at the
            // bottom of the safe-area canvas rather than keeping a phantom gap.
            return baseAnchoredY;
        }

        internal void CaptureBasePosition()
        {
            CacheReferences();
            if (rectTransform == null)
                return;

            baseAnchoredY = rectTransform.anchoredPosition.y;
            basePositionCaptured = true;
            Apply(forceRefresh: true);
        }

        private float GetBannerTopInParentSpace(float bannerTopScreenY)
        {
            if (bannerTopScreenY <= 0f)
                return parentRectTransform.rect.yMin;

            Camera uiCamera = GetUICamera();
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentRectTransform,
                    new Vector2(Screen.width * 0.5f, bannerTopScreenY),
                    uiCamera,
                    out Vector2 localPoint))
            {
                return localPoint.y;
            }

            return parentRectTransform.rect.yMin;
        }

        private float GetBottomSpacingInParentSpace(float bannerTopScreenY)
        {
            if (parentRectTransform == null)
                return bottomSpacing;

            float density = Screen.dpi > 0f ? Screen.dpi / 160f : 1f;
            float spacingInPixels = Mathf.Max(0f, bottomSpacing) * density;
            if (spacingInPixels <= 0f)
                return 0f;

            Camera uiCamera = GetUICamera();
            float anchorY = Mathf.Max(0f, bannerTopScreenY);
            Vector2 screenPoint = new Vector2(Screen.width * 0.5f, anchorY);
            Vector2 paddedScreenPoint = new Vector2(Screen.width * 0.5f, anchorY + spacingInPixels);
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRectTransform, screenPoint, uiCamera, out Vector2 localPoint) &&
                RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRectTransform, paddedScreenPoint, uiCamera, out Vector2 paddedLocalPoint))
            {
                return Mathf.Abs(paddedLocalPoint.y - localPoint.y);
            }

            return bottomSpacing;
        }

        internal static float GetBannerTopScreenY()
        {
            RectTransform dummyBanner = FindDummyBanner();
            if (dummyBanner != null && dummyBanner.gameObject.activeInHierarchy)
            {
                Vector3[] corners = new Vector3[4];
                dummyBanner.GetWorldCorners(corners);

                Camera camera = GetCanvasCamera(dummyBanner.GetComponentInParent<Canvas>());
                return RectTransformUtility.WorldToScreenPoint(camera, corners[1]).y;
            }

            return GetBannerTopEdge();
        }

        private static RectTransform FindDummyBanner()
        {
            Transform banner = GameObject.Find("[ADS DUMMY CANVAS]")?.transform.Find("Banner");
            if (banner != null)
                return banner as RectTransform;

            RectTransform[] rectTransforms = Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < rectTransforms.Length; i++)
            {
                RectTransform rectTransform = rectTransforms[i];
                if (rectTransform.name != "Banner")
                    continue;

                Transform parent = rectTransform.parent;
                while (parent != null)
                {
                    if (parent.name == "[ADS DUMMY CANVAS]")
                        return rectTransform;

                    parent = parent.parent;
                }
            }

            return null;
        }

        private Camera GetUICamera()
        {
            return GetCanvasCamera(rootCanvas);
        }

        private static Camera GetCanvasCamera(Canvas canvas)
        {
            if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                return null;

            return canvas.worldCamera;
        }

        private static float GetBannerTopEdge()
        {
#if MODULE_MONETIZATION
            return AdsManager.GetBannerTopEdge();
#else
            return 0f;
#endif
        }
    }
}
