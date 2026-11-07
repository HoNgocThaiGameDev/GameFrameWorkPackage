using UnityEngine;

namespace NebulaSoft
{
    [System.Serializable]
    public sealed class CachedRectTransform : ICachedComponent<RectTransform>
    {
        [SerializeField] Vector2 anchoredPosition;
        public Vector2 AnchoredPosition => anchoredPosition;

        [SerializeField] Vector2 sizeDelta;
        public Vector2 SizeDelta => sizeDelta;

        [SerializeField] Vector2 offsetMin;
        public Vector2 OffsetMin => offsetMin;

        [SerializeField] Vector2 offsetMax;
        public Vector2 OffsetMax => offsetMax;

        [SerializeField] Vector2 pivot;
        public Vector2 Pivot => pivot;

        public void Apply(RectTransform rectTransform)
        {
            rectTransform.pivot = pivot;
            rectTransform.offsetMin = offsetMin;
            rectTransform.offsetMax = offsetMax;
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = sizeDelta;
        }

        public void Cache(RectTransform rectTransform)
        {
            anchoredPosition = rectTransform.anchoredPosition;
            sizeDelta = rectTransform.sizeDelta;
            offsetMin = rectTransform.offsetMin;
            offsetMax = rectTransform.offsetMax;
            pivot = rectTransform.pivot;
        }
    }
}