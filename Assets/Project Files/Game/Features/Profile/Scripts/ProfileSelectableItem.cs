using System;
using UnityEngine;
using UnityEngine.UI;

namespace NebulaSoft
{
    public class ProfileSelectableItem : MonoBehaviour
    {
        private const float CheckMarkSizeRatio = 0.42f;
        private const float CheckMarkCornerOffsetRatio = 0.38f;

        [SerializeField] Button button;
        [SerializeField] Image iconImage;
        [SerializeField] Image frameImage;
        [SerializeField] GameObject checkMark;

        private int index;
        private Action<int> clickCallback;

        public Image IconImage => iconImage;
        public Image FrameImage => frameImage;

        public void Init(int itemIndex, Sprite icon, Sprite frame, Action<int> onClicked)
        {
            index = itemIndex;
            clickCallback = onClicked;

            if (button == null)
                button = GetComponent<Button>();

            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled = icon != null;
            }

            if (frameImage != null)
            {
                frameImage.sprite = frame;
                frameImage.enabled = frame != null;
            }

            ConfigureSelectedIconLayout();

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(OnClick);
            }
        }

        public void SetSelected(bool selected)
        {
            ConfigureSelectedIconLayout();

            if (checkMark != null)
                checkMark.SetActive(selected);
        }

        private void OnClick()
        {
            clickCallback?.Invoke(index);
        }

        private void ConfigureSelectedIconLayout()
        {
            if (checkMark == null || frameImage == null)
                return;

            RectTransform checkRect = checkMark.transform as RectTransform;
            RectTransform frameRect = frameImage.rectTransform;
            if (checkRect == null || frameRect == null)
                return;

            Vector2 frameSize = GetRectSize(frameRect);
            if (frameSize.x <= 0f || frameSize.y <= 0f)
                return;

            float checkSize = Mathf.Min(frameSize.x, frameSize.y) * CheckMarkSizeRatio;
            checkRect.anchorMin = new Vector2(0.5f, 0.5f);
            checkRect.anchorMax = new Vector2(0.5f, 0.5f);
            checkRect.pivot = new Vector2(0.5f, 0.5f);
            checkRect.sizeDelta = new Vector2(checkSize, checkSize);
            checkRect.anchoredPosition = new Vector2(
                frameSize.x * CheckMarkCornerOffsetRatio,
                -frameSize.y * CheckMarkCornerOffsetRatio);
            checkRect.localScale = Vector3.one;
        }

        private static Vector2 GetRectSize(RectTransform rectTransform)
        {
            Vector2 size = rectTransform.rect.size;
            if (size.x > 0f && size.y > 0f)
                return size;

            return rectTransform.sizeDelta;
        }
    }
}
