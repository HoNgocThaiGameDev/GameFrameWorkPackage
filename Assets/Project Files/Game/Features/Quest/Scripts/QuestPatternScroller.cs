using UnityEngine;
using UnityEngine.UI;

namespace NebulaSoft
{
    [RequireComponent(typeof(RawImage))]
    public sealed class QuestPatternScroller : MonoBehaviour
    {
        [SerializeField] float scrollSpeedX = -0.05f;
        [SerializeField] float scrollSpeedY = -0.04f;
        [SerializeField] float uvResetThreshold = 100f;

        private RawImage rawImage;
        private Rect uvRect;

        private void Awake()
        {
            rawImage = GetComponent<RawImage>();
            uvRect = rawImage.uvRect;
        }

        private void Update()
        {
            uvRect.x += scrollSpeedX * Time.deltaTime;
            uvRect.y += scrollSpeedY * Time.deltaTime;

            if (Mathf.Abs(uvRect.x) > uvResetThreshold)
                uvRect.x = Mathf.Repeat(uvRect.x, 1f);
            if (Mathf.Abs(uvRect.y) > uvResetThreshold)
                uvRect.y = Mathf.Repeat(uvRect.y, 1f);

            rawImage.uvRect = uvRect;
        }
    }
}
