using TMPro;
using UnityEngine;

namespace NebulaSoft
{
    [System.Serializable]
    public class MessageBox
    {
        [SerializeField] RectTransform rectTransform;
        public RectTransform RectTransform => rectTransform;

        [SerializeField] TextMeshProUGUI messageText;
        public TextMeshProUGUI MessageText => messageText;

        [SerializeField] SimpleBounce bounceEffect;
        
        public void Init()
        {
            bounceEffect.Init(rectTransform);

            rectTransform.gameObject.SetActive(false);
        }

        public void Activate(string text, int fontSize = 52)
        {
            messageText.text = text;
            messageText.fontSize = fontSize;

            rectTransform.gameObject.SetActive(true);

            bounceEffect.Bounce();
        }

        public void Disable()
        {
            rectTransform.gameObject.SetActive(false);
        }

        public void ActivateTutorial()
        {
            TutorialCanvasController.ActivateTutorialCanvas(rectTransform, false, false);
        }
    }
}