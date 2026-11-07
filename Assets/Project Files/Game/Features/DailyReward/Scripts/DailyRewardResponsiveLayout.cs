using UnityEngine;

namespace NebulaSoft
{
    /// <summary>
    /// Keeps the Daily Reward artwork at one authored size and uniformly fits it inside its Safe Area.
    /// </summary>
    public sealed class DailyRewardResponsiveLayout : MonoBehaviour
    {
        [SerializeField] RectTransform safeAreaTransform;
        [SerializeField] Vector2 referenceSize = new Vector2(1080f, 2000f);
        [SerializeField] float maximumScale = 1f;

        private RectTransform contentTransform;
        private Vector2 lastAvailableSize = new Vector2(-1f, -1f);

        private void Awake()
        {
            contentTransform = transform as RectTransform;
        }

        private void OnEnable()
        {
            Refresh();
        }

        private void LateUpdate()
        {
            if (safeAreaTransform == null)
                return;

            Vector2 availableSize = safeAreaTransform.rect.size;
            if (availableSize != lastAvailableSize)
                Refresh();
        }

        public void Refresh()
        {
            if (contentTransform == null)
                contentTransform = transform as RectTransform;
            if (contentTransform == null || safeAreaTransform == null || referenceSize.x <= 0f || referenceSize.y <= 0f)
                return;

            Vector2 availableSize = safeAreaTransform.rect.size;
            if (availableSize.x <= 0f || availableSize.y <= 0f)
                return;

            float scale = Mathf.Min(availableSize.x / referenceSize.x, availableSize.y / referenceSize.y);
            scale = Mathf.Min(scale, maximumScale);

            contentTransform.anchorMin = new Vector2(0.5f, 0.5f);
            contentTransform.anchorMax = new Vector2(0.5f, 0.5f);
            contentTransform.pivot = new Vector2(0.5f, 0.5f);
            contentTransform.sizeDelta = referenceSize;
            contentTransform.anchoredPosition = Vector2.zero;
            contentTransform.localScale = Vector3.one * scale;
            lastAvailableSize = availableSize;
        }
    }
}
