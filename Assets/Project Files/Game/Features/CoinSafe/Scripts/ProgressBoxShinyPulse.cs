using Coffee.UIEffects;
using UnityEngine;

public sealed class ProgressBoxShinyPulse : MonoBehaviour
{
    [SerializeField] private float boxEffectScaleMultiplier = 1.75f;
    [SerializeField] private float shinyPassRate = 0.5f;
    [SerializeField] private float shinyPassWindow = 0.25f;

    private RectTransform boxEffect;
    private UIEffectTweener tweener;
    private Vector3 boxEffectTargetScale;
    private bool boxEffectScaleCached;

    private void Awake()
    {
        CacheReferences();
    }

    private void OnEnable()
    {
        CacheReferences();

        if (tweener != null)
            tweener.onChangedRate.AddListener(OnShinyRateChanged);
    }

    private void OnDisable()
    {
        if (tweener != null)
            tweener.onChangedRate.RemoveListener(OnShinyRateChanged);

        RestoreBoxEffectScale();
    }

    private void CacheReferences()
    {
        if (boxEffect == null)
            boxEffect = transform.Find("Box_effect") as RectTransform;

        if (tweener == null)
            tweener = GetComponent<UIEffectTweener>();

        if (boxEffect != null)
        {
            boxEffectTargetScale = boxEffect.localScale;
            boxEffectScaleCached = true;
        }
    }

    private void OnShinyRateChanged(float rate)
    {
        if (!boxEffectScaleCached)
            CacheReferences();

        if (boxEffect == null)
            return;

        var normalizedDistance = Mathf.Abs(rate - shinyPassRate) / shinyPassWindow;
        var pulse = Mathf.Clamp01(1f - normalizedDistance);
        boxEffect.localScale = boxEffectTargetScale * Mathf.Lerp(1f, boxEffectScaleMultiplier, pulse);
    }

    private void RestoreBoxEffectScale()
    {
        if (boxEffect != null)
            boxEffect.localScale = boxEffectTargetScale;
    }
}
