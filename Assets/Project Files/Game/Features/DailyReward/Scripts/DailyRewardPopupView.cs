using TMPro;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NebulaSoft
{
    [RequireComponent(typeof(Canvas), typeof(GraphicRaycaster), typeof(CanvasGroup))]
    public sealed class DailyRewardPopupView : MonoBehaviour
    {
        public static event System.Action<bool> PopupVisibilityChanged;
        public static bool IsPopupVisible { get; private set; }

        private const float ShowDuration = 0.22f;
        private const float HideDuration = 0.16f;
        private const float ShowStartScale = 0.92f;
        private const float HideEndScale = 0.96f;

        [SerializeField] Image dimBackground;
        [SerializeField] RectTransform safeAreaTransform;
        [SerializeField] DailyRewardResponsiveLayout responsiveLayout;
        [SerializeField] Button closeButton;
        [SerializeField] DailyRewardElementView[] dayElements;
        [SerializeField] DailyRewardClaimButtonView daySevenClaimButton;
        [SerializeField] TextMeshProUGUI daySevenLabel;
        [SerializeField] RectTransform primaryRewardIconAnchor;
        [SerializeField] RectTransform bonusRewardIconAnchor;
        [SerializeField] TextMeshProUGUI plusLabel;
        [SerializeField] int sortingOrder = 1100;
        [SerializeField] DailyRewardClaimPopup rewardClaimPopupPrefab;

        private Canvas popupCanvas;
        private Image primaryRewardIcon;
        private Image bonusRewardIcon;
        private Button dimButton;
        private CanvasGroup canvasGroup;
        private DailyRewardClaimPopup rewardClaimPopup;
        private TweenCase fadeTweenCase;
        private readonly List<Transform> animatedContent = new List<Transform>();
        private readonly List<Vector3> contentTargetScales = new List<Vector3>();
        private readonly List<TweenCase> scaleTweenCases = new List<TweenCase>();
        private bool isClosing;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            IsPopupVisible = false;
            PopupVisibilityChanged = null;
        }

        private void Awake()
        {
            popupCanvas = GetComponent<Canvas>();
            canvasGroup = GetComponent<CanvasGroup>();
            popupCanvas.overrideSorting = true;
            popupCanvas.sortingOrder = sortingOrder;
            if (safeAreaTransform != null)
                NotchSaveArea.RegisterRectTransform(safeAreaTransform);
            if (responsiveLayout != null)
                responsiveLayout.Refresh();
            CacheAnimationContent();

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Hide);
                closeButton.onClick.AddListener(Hide);
                UIAudioFeedback.RegisterButton(closeButton);
            }

            if (dimBackground != null)
            {
                dimBackground.raycastTarget = true;
                dimButton = dimBackground.GetComponent<Button>();
                if (dimButton == null)
                    dimButton = dimBackground.gameObject.AddComponent<Button>();

                dimButton.targetGraphic = dimBackground;
                dimButton.onClick.RemoveListener(Hide);
                dimButton.onClick.AddListener(Hide);
                UIAudioFeedback.RegisterButton(dimButton);
            }
        }

        private void OnEnable()
        {
            if (responsiveLayout != null)
                responsiveLayout.Refresh();
            SetPopupVisible(true);
            BottomNavigationVisibilityEvents.RequestHide();
            DailyRewardService.StateChanged += Refresh;
            Refresh();
            PlayShowAnimation();
        }

        private void OnDisable()
        {
            KillAnimationTweens();
            DailyRewardService.StateChanged -= Refresh;
            SetPopupVisible(false);
            BottomNavigationVisibilityEvents.RequestShow();
        }

        private void OnDestroy()
        {
            if (closeButton != null)
                closeButton.onClick.RemoveListener(Hide);
            if (dimButton != null)
                dimButton.onClick.RemoveListener(Hide);
        }

        public bool TryShowIfClaimable()
        {
            if (!DailyRewardService.IsInitialized || !DailyRewardService.GetState().CanClaimToday)
                return false;

            Show();
            return true;
        }

        public void Show()
        {
            bool wasInactive = !gameObject.activeSelf;
            if (wasInactive)
                gameObject.SetActive(true);

            transform.SetAsLastSibling();
            Refresh();

            if (!wasInactive)
                PlayShowAnimation();
        }

        public void Hide()
        {
            if (!gameObject.activeSelf || isClosing)
                return;

            isClosing = true;
            KillAnimationTweens();
            SetInteraction(false);

            if (!IsTweenSystemReady)
            {
                CompleteHide();
                return;
            }

            fadeTweenCase = canvasGroup
                .DOFade(0f, HideDuration, unscaledTime: true)
                .SetEasing(Ease.Type.CubicIn)
                .OnComplete(CompleteHide);

            for (int i = 0; i < animatedContent.Count; i++)
            {
                scaleTweenCases.Add(animatedContent[i]
                    .DOScale(contentTargetScales[i] * HideEndScale, HideDuration, unscaledTime: true)
                    .SetEasing(Ease.Type.CubicIn));
            }
        }

        private void Claim(int dayIndex)
        {
            DailyRewardState state = DailyRewardService.GetState();
            if (!state.CanClaimToday || state.CurrentDayIndex != dayIndex)
                return;

            DailyRewardDayDefinition day = DailyRewardService.GetDay(dayIndex);
            if (day == null || day.Rewards == null || day.Rewards.Count == 0)
                return;

            if (rewardClaimPopup == null)
                rewardClaimPopup = FindFirstObjectByType<DailyRewardClaimPopup>(FindObjectsInactive.Include);
            if (rewardClaimPopup == null && rewardClaimPopupPrefab != null)
                rewardClaimPopup = Instantiate(rewardClaimPopupPrefab);
            if (rewardClaimPopup == null || rewardClaimPopup.IsShowing)
                return;

            rewardClaimPopup.Show(day.Rewards, ClaimDailyReward);
        }

        private bool ClaimDailyReward()
        {
            bool claimed = DailyRewardService.TryClaimToday();
            if (claimed)
                Refresh();

            return claimed;
        }

        private static void SetPopupVisible(bool visible)
        {
            if (IsPopupVisible == visible)
                return;

            IsPopupVisible = visible;
            PopupVisibilityChanged?.Invoke(visible);
        }

        private static bool IsTweenSystemReady => Tween.Tweens != null;

        private void CacheAnimationContent()
        {
            if (animatedContent.Count > 0)
                return;

            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (dimBackground != null && child == dimBackground.transform)
                    continue;

                animatedContent.Add(child);
                contentTargetScales.Add(child.localScale);
            }
        }

        private void PlayShowAnimation()
        {
            isClosing = false;
            CacheAnimationContent();
            KillAnimationTweens();

            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            if (canvasGroup == null)
                return;

            canvasGroup.alpha = 0f;
            SetInteraction(false);
            RestoreContentScales(ShowStartScale);

            if (!IsTweenSystemReady)
            {
                canvasGroup.alpha = 1f;
                RestoreContentScales(1f);
                SetInteraction(true);
                return;
            }

            fadeTweenCase = canvasGroup
                .DOFade(1f, ShowDuration, unscaledTime: true)
                .SetEasing(Ease.Type.SineOut)
                .OnComplete(() => SetInteraction(true));

            for (int i = 0; i < animatedContent.Count; i++)
            {
                scaleTweenCases.Add(animatedContent[i]
                    .DOScale(contentTargetScales[i], ShowDuration, unscaledTime: true)
                    .SetEasing(Ease.Type.BackOut));
            }
        }

        private void CompleteHide()
        {
            if (!isClosing)
                return;

            fadeTweenCase = null;
            isClosing = false;
            RestoreContentScales(1f);
            canvasGroup.alpha = 1f;
            SetInteraction(true);
            gameObject.SetActive(false);
        }

        private void RestoreContentScales(float multiplier)
        {
            for (int i = 0; i < animatedContent.Count; i++)
                animatedContent[i].localScale = contentTargetScales[i] * multiplier;
        }

        private void SetInteraction(bool enabled)
        {
            if (canvasGroup == null)
                return;

            canvasGroup.interactable = enabled;
            canvasGroup.blocksRaycasts = enabled;
        }

        private void KillAnimationTweens()
        {
            fadeTweenCase.KillActive();
            fadeTweenCase = null;

            for (int i = 0; i < scaleTweenCases.Count; i++)
                scaleTweenCases[i].KillActive();

            scaleTweenCases.Clear();
        }

        private void Refresh()
        {
            if (!DailyRewardService.IsInitialized)
                return;

            DailyRewardState state = DailyRewardService.GetState();
            if (dayElements == null)
                return;

            for (int i = 0; i < dayElements.Length; i++)
            {
                DailyRewardElementView element = dayElements[i];
                if (element == null)
                    continue;

                int dayIndex = i;
                DailyRewardCellState cellState = GetCellState(dayIndex, state);
                element.Bind(dayIndex + 1, DailyRewardService.GetDay(dayIndex), cellState, () => Claim(dayIndex));
            }

            int daySevenIndex = DailyRewardDatabase.CycleLength - 1;
            BindDaySeven(DailyRewardService.GetDay(daySevenIndex), GetCellState(daySevenIndex, state));
        }

        private DailyRewardCellState GetCellState(int index, DailyRewardState state)
        {
            if (state.IsClaimedToday && index == state.ClaimedDayIndex)
                return DailyRewardCellState.Claimed;
            return !state.IsClaimedToday && index == state.CurrentDayIndex
                ? DailyRewardCellState.Current
                : DailyRewardCellState.Locked;
        }

        private void BindDaySeven(DailyRewardDayDefinition day, DailyRewardCellState state)
        {
            if (daySevenLabel != null)
                daySevenLabel.text = "Day 7";

            DailyRewardGrant primary = day != null && day.Rewards != null && day.Rewards.Count > 0
                ? day.Rewards[0]
                : null;
            DailyRewardGrant bonus = day != null && day.Rewards != null && day.Rewards.Count > 1
                ? day.Rewards[1]
                : null;

            ApplyIcon(primaryRewardIconAnchor, ref primaryRewardIcon, primary);
            ApplyIcon(bonusRewardIconAnchor, ref bonusRewardIcon, bonus);
            if (plusLabel != null)
                plusLabel.gameObject.SetActive(bonus != null);
            if (daySevenClaimButton != null)
                daySevenClaimButton.Bind(state, () => Claim(DailyRewardDatabase.CycleLength - 1));
        }

        private static void ApplyIcon(RectTransform anchor, ref Image icon, DailyRewardGrant reward)
        {
            if (reward == null)
            {
                if (icon != null)
                    icon.gameObject.SetActive(false);
                return;
            }

            if (icon == null && anchor != null)
            {
                GameObject iconObject = new GameObject("Reward Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                iconObject.transform.SetParent(anchor, false);
                RectTransform iconRect = iconObject.GetComponent<RectTransform>();
                iconRect.anchorMin = Vector2.zero;
                iconRect.anchorMax = Vector2.one;
                iconRect.offsetMin = Vector2.zero;
                iconRect.offsetMax = Vector2.zero;

                icon = iconObject.GetComponent<Image>();
                icon.preserveAspect = true;
                icon.raycastTarget = false;
            }

            if (icon == null)
                return;

            icon.sprite = reward.GetIcon();
            icon.gameObject.SetActive(icon.sprite != null);
        }
    }
}
