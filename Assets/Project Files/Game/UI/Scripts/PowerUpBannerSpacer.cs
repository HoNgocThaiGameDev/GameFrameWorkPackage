using UnityEngine;

namespace NebulaSoft
{
    [RequireComponent(typeof(RectTransform))]
    public class PowerUpBannerSpacer : MonoBehaviour
    {
        [SerializeField] float bottomSpacing = 6f;
        [SerializeField] bool refreshSafeAreaOnScreenChange = true;

        private RectTransform rectTransform;
        private RectTransform parentRectTransform;
        private Canvas rootCanvas;
        private float baseAnchoredY;
        private bool basePositionCaptured;
        private Vector2Int lastScreenSize;
        private ScreenOrientation lastOrientation;
        private float lastBannerTop = -1f;

        private void Awake()
        {
            CacheReferences();
            CaptureBasePosition();
            Apply(forceRefresh: true);
        }

        private void OnEnable()
        {
            CacheReferences();
            CaptureBasePosition();

#if MODULE_MONETIZATION
            AdsManager.BannerLayoutChanged -= OnBannerLayoutChanged;
            AdsManager.BannerLayoutChanged += OnBannerLayoutChanged;
#endif

            Apply(forceRefresh: true);
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
            Apply(forceRefresh: true);
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

        private void Apply(bool forceRefresh)
        {
            float bannerTop = BottomNavigationBannerSpacer.GetBannerTopScreenY();
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

            ApplyPosition(bannerTop);
        }

        private void ApplyPosition(float bannerTopScreenY)
        {
            if (rectTransform == null)
                return;

            float targetY = baseAnchoredY + GetBannerOffsetInParentSpace(bannerTopScreenY);
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

        private void CaptureBasePosition()
        {
            if (basePositionCaptured || rectTransform == null)
                return;

            baseAnchoredY = rectTransform.anchoredPosition.y;
            basePositionCaptured = true;
        }

        private float GetBannerOffsetInParentSpace(float bannerTopScreenY)
        {
            if (parentRectTransform == null || bannerTopScreenY <= 0f)
                return 0f;

            Camera uiCamera = GetUICamera();
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentRectTransform,
                    new Vector2(Screen.width * 0.5f, bannerTopScreenY),
                    uiCamera,
                    out Vector2 bannerTop))
            {
                return 0f;
            }

            float parentBottom = parentRectTransform.rect.yMin;
            if (bannerTop.y <= parentBottom)
                return 0f;

            return bannerTop.y - parentBottom + GetBottomSpacingInParentSpace(bannerTopScreenY);
        }

        private float GetBottomSpacingInParentSpace(float bannerTopScreenY)
        {
            float density = Screen.dpi > 0f ? Screen.dpi / 160f : 1f;
            float spacingInPixels = Mathf.Max(0f, bottomSpacing) * density;
            if (spacingInPixels <= 0f || parentRectTransform == null)
                return 0f;

            Camera uiCamera = GetUICamera();
            Vector2 screenPoint = new Vector2(Screen.width * 0.5f, bannerTopScreenY);
            Vector2 paddedScreenPoint = new Vector2(Screen.width * 0.5f, bannerTopScreenY + spacingInPixels);
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRectTransform, screenPoint, uiCamera, out Vector2 localPoint) &&
                RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRectTransform, paddedScreenPoint, uiCamera, out Vector2 paddedLocalPoint))
            {
                return Mathf.Abs(paddedLocalPoint.y - localPoint.y);
            }

            return bottomSpacing;
        }

        private Camera GetUICamera()
        {
            if (rootCanvas == null || rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
                return null;

            return rootCanvas.worldCamera;
        }
    }
}
