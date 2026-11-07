using System.Collections.Generic;
using NebulaSoft;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public sealed class ProgressPopupEntranceAnimation : MonoBehaviour
{
    [SerializeField] private float safeDropHeight = 1100f;
    [SerializeField] private float safeDropDuration = 0.35f;
    [SerializeField] private float elementBounceScale = 1.12f;
    [SerializeField] private float elementBounceOutDuration = 0.1f;
    [SerializeField] private float elementBounceInDuration = 0.14f;
    [SerializeField] private float elementBounceStagger = 0.06f;

    private readonly List<RectTransform> bounceElements = new List<RectTransform>();
    private readonly List<Vector3> bounceTargetScales = new List<Vector3>();

    private CanvasGroup canvasGroup;
    private CanvasGroup bottomNavigationGroup;
    private RectTransform content;
    private RectTransform coinSafe;
    private RectTransform coinAmountDialog;
    private Vector2 coinSafeTargetPosition;
    private Vector3 coinSafeTargetScale;
    private Vector3 coinAmountDialogTargetScale;
    private TweenCase safeDropTween;
    private TweenCase coinAmountDialogTween;
    private TweenCase[] elementBounceTweens;
    private bool layoutCached;
    private bool bottomNavigationHidden;
    private float bottomNavigationAlpha;
    private bool bottomNavigationInteractable;
    private bool bottomNavigationBlocksRaycasts;

    private void OnEnable()
    {
        if (!Application.isPlaying)
            return;

        CacheLayout();
    }

    public void Show()
    {
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        AudioClips audioClips = AudioController.AudioClips;
        if (audioClips != null && audioClips.brightest_star != null)
            AudioController.PlaySound(audioClips.brightest_star);

        if (!CacheLayout())
        {
            SetInteraction(true);
            return;
        }

        KillTweens();
        ResetVisuals();
        SetInteraction(true);
        HideBottomNavigation();

        coinSafe.anchoredPosition = coinSafeTargetPosition + Vector2.up * safeDropHeight;
        safeDropTween = coinSafe
            .DOAnchoredPosition(coinSafeTargetPosition, safeDropDuration, unscaledTime: true)
            .SetEasing(Ease.Type.BounceOut)
            .OnComplete(ShowCoinAmountDialog);

        PlayElementBounceSequence();
    }

    public void Hide()
    {
        KillTweens();
        ResetVisuals();
        RestoreBottomNavigation();
        SetInteraction(true);
        gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        KillTweens();
        RestoreBottomNavigation();
    }

    private bool CacheLayout()
    {
        if (layoutCached)
            return true;

        canvasGroup = GetComponent<CanvasGroup>();
        content = transform.Find("Content") as RectTransform;
        if (content == null)
            return false;

        coinSafe = content.Find("Coin Safe Icon") as RectTransform;
        coinAmountDialog = content.Find("Coin Amount Dialog") as RectTransform;
        if (coinSafe == null || coinAmountDialog == null)
            return false;

        coinSafeTargetPosition = coinSafe.anchoredPosition;
        coinSafeTargetScale = coinSafe.localScale;
        coinAmountDialogTargetScale = coinAmountDialog.localScale;

        var bottomNavigationCanvas = GameObject.Find("Bottom Navigation Canvas");
        if (bottomNavigationCanvas != null)
            bottomNavigationGroup = bottomNavigationCanvas.GetComponent<CanvasGroup>();

        for (var i = 0; i < content.childCount; i++)
        {
            var element = content.GetChild(i) as RectTransform;
            if (element == null || element == coinSafe || element == coinAmountDialog)
                continue;

            bounceElements.Add(element);
            bounceTargetScales.Add(element.localScale);
        }

        elementBounceTweens = new TweenCase[bounceElements.Count];
        layoutCached = true;
        return true;
    }

    private void ShowCoinAmountDialog()
    {
        if (!gameObject.activeInHierarchy || coinAmountDialog == null)
            return;

        coinAmountDialog.gameObject.SetActive(true);
        coinAmountDialog.localScale = coinAmountDialogTargetScale;
        coinAmountDialogTween = coinAmountDialog.DOPushScale(
            coinAmountDialogTargetScale * elementBounceScale,
            coinAmountDialogTargetScale,
            elementBounceOutDuration,
            elementBounceInDuration,
            Ease.Type.SineOut,
            Ease.Type.SineIn,
            0f,
            unscaledTime: true);
    }

    private void PlayElementBounceSequence()
    {
        if (!gameObject.activeInHierarchy)
            return;

        if (bounceElements.Count == 0)
        {
            SetInteraction(true);
            return;
        }

        for (var i = 0; i < bounceElements.Count; i++)
        {
            elementBounceTweens[i] = bounceElements[i]
                .DOPushScale(
                    bounceTargetScales[i] * elementBounceScale,
                    bounceTargetScales[i],
                    elementBounceOutDuration,
                    elementBounceInDuration,
                    Ease.Type.SineOut,
                    Ease.Type.SineIn,
                    i * elementBounceStagger,
                    unscaledTime: true);
        }

        elementBounceTweens[elementBounceTweens.Length - 1].OnComplete(() => SetInteraction(true));
    }

    private void ResetVisuals()
    {
        if (!layoutCached)
            return;

        coinSafe.anchoredPosition = coinSafeTargetPosition;
        coinSafe.localScale = coinSafeTargetScale;
        coinAmountDialog.localScale = coinAmountDialogTargetScale;
        coinAmountDialog.gameObject.SetActive(false);

        for (var i = 0; i < bounceElements.Count; i++)
            bounceElements[i].localScale = bounceTargetScales[i];
    }

    private void KillTweens()
    {
        safeDropTween?.Kill();
        safeDropTween = null;

        coinAmountDialogTween?.Kill();
        coinAmountDialogTween = null;

        if (elementBounceTweens == null)
            return;

        for (var i = 0; i < elementBounceTweens.Length; i++)
        {
            elementBounceTweens[i]?.Kill();
            elementBounceTweens[i] = null;
        }
    }

    private void SetInteraction(bool state)
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            return;

        canvasGroup.interactable = state;
        canvasGroup.blocksRaycasts = state;
    }

    private void HideBottomNavigation()
    {
        if (bottomNavigationHidden)
            return;

        if (bottomNavigationGroup == null)
        {
            var bottomNavigationCanvas = GameObject.Find("Bottom Navigation Canvas");
            if (bottomNavigationCanvas != null)
                bottomNavigationGroup = bottomNavigationCanvas.GetComponent<CanvasGroup>();
        }

        if (bottomNavigationGroup == null)
            return;

        bottomNavigationAlpha = bottomNavigationGroup.alpha;
        bottomNavigationInteractable = bottomNavigationGroup.interactable;
        bottomNavigationBlocksRaycasts = bottomNavigationGroup.blocksRaycasts;

        bottomNavigationGroup.alpha = 0f;
        bottomNavigationGroup.interactable = false;
        bottomNavigationGroup.blocksRaycasts = false;
        bottomNavigationHidden = true;
    }

    private void RestoreBottomNavigation()
    {
        if (!bottomNavigationHidden || bottomNavigationGroup == null)
            return;

        bottomNavigationGroup.alpha = bottomNavigationAlpha;
        bottomNavigationGroup.interactable = bottomNavigationInteractable;
        bottomNavigationGroup.blocksRaycasts = bottomNavigationBlocksRaycasts;
        bottomNavigationHidden = false;
    }
}
