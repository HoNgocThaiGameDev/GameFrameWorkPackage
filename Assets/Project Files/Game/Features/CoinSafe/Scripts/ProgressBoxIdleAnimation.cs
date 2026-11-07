using UnityEngine;

public sealed class ProgressBoxIdleAnimation : MonoBehaviour
{
    [SerializeField] private float bounceHeight = 9f;
    [SerializeField] private float scaleMultiplier = 1.08f;
    [SerializeField] private float riseDuration = 0.16f;
    [SerializeField] private float fallDuration = 0.11f;
    [SerializeField] private float impactShakeDuration = 0.16f;
    [SerializeField] private float shakeDistance = 2.5f;
    [SerializeField] private float shakeAngle = 9f;
    [SerializeField] private float repeatInterval = 4f;

    private RectTransform rectTransform;
    private Vector2 targetPosition;
    private Vector3 targetScale;
    private Quaternion targetRotation;
    private float elapsed;

    private void Awake()
    {
        rectTransform = transform as RectTransform;
        CacheTargetState();
    }

    private void OnEnable()
    {
        if (rectTransform == null)
            rectTransform = transform as RectTransform;

        CacheTargetState();
        elapsed = 0f;
    }

    private void Update()
    {
        elapsed += Time.unscaledDeltaTime;

        var cycleTime = elapsed % repeatInterval;
        var fallStartTime = riseDuration;
        var shakeStartTime = fallStartTime + fallDuration;
        var shakeEndTime = shakeStartTime + impactShakeDuration;

        if (cycleTime < fallStartTime)
        {
            var progress = cycleTime / riseDuration;
            var lift = 1f - (1f - progress) * (1f - progress);
            rectTransform.anchoredPosition = targetPosition + Vector2.up * bounceHeight * lift;
            rectTransform.localScale = targetScale * Mathf.Lerp(1f, scaleMultiplier, lift);
            return;
        }

        if (cycleTime < shakeStartTime)
        {
            var progress = (cycleTime - fallStartTime) / fallDuration;
            var fall = progress * progress;
            rectTransform.anchoredPosition = targetPosition + Vector2.up * bounceHeight * (1f - fall);
            rectTransform.localScale = targetScale * Mathf.Lerp(scaleMultiplier, 1f, fall);
            return;
        }

        if (cycleTime < shakeEndTime)
        {
            var progress = (cycleTime - shakeStartTime) / impactShakeDuration;
            var damping = 1f - progress;
            var shakeFactor = Mathf.Sin(progress * Mathf.PI * 4f) * damping;
            var shake = shakeFactor * shakeDistance;
            rectTransform.anchoredPosition = targetPosition + Vector2.right * shake;
            rectTransform.localScale = targetScale * Mathf.Lerp(1f, 1.02f, damping);
            rectTransform.localRotation = targetRotation * Quaternion.Euler(0f, 0f, -shakeFactor * shakeAngle);
            return;
        }

        RestoreTargetState();
    }

    private void OnDisable()
    {
        RestoreTargetState();
    }

    private void CacheTargetState()
    {
        targetPosition = rectTransform.anchoredPosition;
        targetScale = rectTransform.localScale;
        targetRotation = rectTransform.localRotation;
    }

    private void RestoreTargetState()
    {
        if (rectTransform == null)
            return;

        rectTransform.anchoredPosition = targetPosition;
        rectTransform.localScale = targetScale;
        rectTransform.localRotation = targetRotation;
    }
}
