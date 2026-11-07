using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NebulaSoft
{
    public class UIIAPOffer : UIPage, IPopupWindow
    {
        [SerializeField] Image backgroundImage;
        [SerializeField] UIScaleAnimation panelScalable; 
        [SerializeField] Button smallCloseButton;

        public bool IsOpened => canvas.enabled;

        private UIFadeAnimation backFade;

        public override void Init()
        {
            backFade = new UIFadeAnimation(gameObject);

            backgroundImage.AddEvent(EventTriggerType.PointerClick, OnBackgroundClicked);

            smallCloseButton.onClick.AddListener(OnCloseButtonClicked);

            backFade.Hide(immediately: true);
            panelScalable.Hide(immediately: true);
        }

        public override void PlayShowAnimation()
        {
            backFade.Show(0.2f, onCompleted: () =>
            {
                panelScalable.Show(immediately: false, duration: 0.3f);
            });

            UIController.OnPageOpened(this);

        }

        public override void PlayHideAnimation()
        {
            backFade.Hide(0.2f);
            panelScalable.Hide(immediately: false, duration: 0.4f, onCompleted: () =>
            {
                UIController.OnPageClosed(this);
                OnOfferClosed();
            });
        }

        protected virtual void OnOfferClosed()
        {
        }

        private void OnCloseButtonClicked()
        {
            UIController.HidePage(this);
        }

        private void OnBackgroundClicked(PointerEventData data)
        {
            UIAudioFeedback.PlayButtonSound();
            UIController.HidePage(this);
        }

        [Button]
        private void Show()
        {
            UIController.ShowPage(this);
        }

        [Button]
        private void Hide()
        {
            UIController.HidePage(this);
        }
    }
}
