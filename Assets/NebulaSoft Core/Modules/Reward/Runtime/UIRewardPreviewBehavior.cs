using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NebulaSoft
{
    [RequireComponent(typeof(CanvasGroup))]
    public class UIRewardPreviewBehavior : MonoBehaviour
    {
        [SerializeField] Image image;
        public Image Image => image;

        [SerializeField] TextMeshProUGUI text;
        public TextMeshProUGUI Text => text;

        private CanvasGroup canvasGroup;
        public CanvasGroup CanvasGroup => canvasGroup;

        private IRewardPreview rewardPreview;

        public void Init(IRewardPreview rewardPreview, Sprite defaultSprite)
        {
            this.rewardPreview = rewardPreview;

            canvasGroup = GetComponent<CanvasGroup>();

            if (image != null)
            {
                Sprite sprite = rewardPreview.Icon;
                if (sprite == null)
                    sprite = defaultSprite;

                image.sprite = sprite;
            }

            if (text != null)
                text.text = rewardPreview.Text;

            OnInitialized();
        }

        protected virtual void OnInitialized() { }
    }
}
