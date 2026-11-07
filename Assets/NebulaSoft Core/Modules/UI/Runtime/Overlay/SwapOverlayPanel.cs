using UnityEngine;
using UnityEngine.UI;

namespace NebulaSoft
{
    public class SwapOverlayPanel : BaseOverlayPanel
    {
        [SerializeField] RawImage image;
        [SerializeField] Gradient gradient;

        private Vector2 size;
        private Vector2 center;

        private CanvasScaler scaler;

        private TweenCase tweenCase;

        public override void Init()
        {
            canvas = gameObject.GetComponent<Canvas>();

            scaler = UIController.CanvasScaler;

            float screenWidth;
            float screenHeight;
            if (UIController.IsTablet)
            {
                var height = canvas.pixelRect.height;
                screenHeight = scaler.referenceResolution.y;

                screenWidth = canvas.pixelRect.width / height * screenHeight;
            }
            else
            {
                var width = canvas.pixelRect.width;
                screenWidth = scaler.referenceResolution.x;

                screenHeight = canvas.pixelRect.height / width * screenWidth;
            }

            var start = 0f;
            var end = 1f;

            for (int i = 0; i < gradient.alphaKeys.Length; i++)
            {
                var key = gradient.alphaKeys[i];

                if (key.alpha == 1f)
                {
                    start = key.time;
                    break;
                }
            }

            for (int i = gradient.alphaKeys.Length - 1; i >= 0; i--)
            {
                var key = gradient.alphaKeys[i];

                if (key.alpha == 1f)
                {
                    end = key.time;
                    break;
                }
            }

            float sum = end - start;

            float imageHeight = screenHeight / sum;

            size = new Vector2(screenWidth + 10, imageHeight);
            image.rectTransform.anchoredPosition = new Vector2(0, imageHeight);
            image.rectTransform.sizeDelta = size;

            Texture2D texture = new Texture2D(1, 100);
            texture.wrapMode = TextureWrapMode.Clamp;

            Color32[] colors = new Color32[100];
            for (int i = 0; i < 100; i++)
            {
                colors[i] = gradient.Evaluate(i / 99f);
            }

            texture.SetPixels32(colors);
            texture.Apply();

            image.texture = texture;

            center = new Vector3(0, ((end + start) / 2 - 0.5f) * size.y);
        }

        public override void Show(float duration, SimpleCallback onCompleted)
        {
            tweenCase.KillActive();

            image.rectTransform.anchoredPosition = new Vector2(0, size.y);
            tweenCase = image.DOAnchoredPosition(center, duration, unscaledTime: true).SetEasing(Ease.Type.Linear).OnComplete(onCompleted);
        }

        public override void Hide(float duration, SimpleCallback onCompleted)
        {
            tweenCase.KillActive();

            image.rectTransform.anchoredPosition = center;
            tweenCase = image.DOAnchoredPosition(new Vector2(0, -size.y), duration, unscaledTime: true).SetEasing(Ease.Type.Linear).OnComplete(onCompleted);
        }

        public override void Clear()
        {
            tweenCase.KillActive();
        }
    }
}
