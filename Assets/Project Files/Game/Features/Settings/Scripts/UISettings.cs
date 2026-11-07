using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NebulaSoft
{
    public class UISettings : UIPage, IPopupWindow, IPausePopup
    {
        [BoxGroup("References", "References")]
        [SerializeField] Image backgroundImage;
        [BoxGroup("References", "References")]
        [SerializeField] RectTransform panelRectTransform;
        [BoxGroup("References", "References")]
        [SerializeField] RectTransform contentRectTransform;
        public RectTransform ContentRectTransform => contentRectTransform;

        [BoxGroup("Buttons", "Buttons")]
        [SerializeField] Button closeButton;

        private float fadeIntensity;

        public bool IsOpened => canvas.enabled;

        public override void Init()
        {
            closeButton.onClick.AddListener(OnCloseButtonClicked);
            backgroundImage.AddEvent(EventTriggerType.PointerDown, OnBackgroundClicked);

            fadeIntensity = backgroundImage.color.a;
        }

        public override void PlayShowAnimation()
        {
            RecalculatePanelSize();

            panelRectTransform.anchoredPosition = Vector2.down * 2000;
            panelRectTransform.DOAnchoredPosition(Vector2.zero, 0.3f, unscaledTime: true).SetEasing(Ease.Type.SineOut);

            backgroundImage.SetAlpha(0f);
            backgroundImage.DOFade(fadeIntensity, 0.3f, unscaledTime: true).OnComplete(() => 
            {
                UIController.OnPageOpened(this);
            });
        }

        public override void PlayHideAnimation()
        {
            panelRectTransform.DOAnchoredPosition(Vector2.down * 2000, 0.3f, unscaledTime: true).SetEasing(Ease.Type.SineIn);

            backgroundImage.DOFade(0f, 0.3f, unscaledTime: true).OnComplete(() =>
            {
                UIController.OnPageClosed(this);
            });
        }

        private void RecalculatePanelSize()
        {
            float height = Mathf.Abs(contentRectTransform.sizeDelta.y);

            int childCount = contentRectTransform.childCount;
            for(int i = 0; i < childCount; i++)
            {
                Transform childTransform = contentRectTransform.GetChild(i);
                if (childTransform != null)
                {
                    SettingsElementsGroup settingsElementsGroup = childTransform.GetComponent<SettingsElementsGroup>();
                    if(settingsElementsGroup != null)
                    {
                        if (settingsElementsGroup.IsGroupActive())
                        {
                            height += ((RectTransform)childTransform).sizeDelta.y;

                            settingsElementsGroup.gameObject.SetActive(true);
                        }
                        else
                        {
                            settingsElementsGroup.gameObject.SetActive(false);
                        }
                    }
                    else
                    {
                        if(childTransform.gameObject.activeSelf)
                        {
                            height += ((RectTransform)childTransform).sizeDelta.y;
                        }
                    }
                }
            }

            panelRectTransform.sizeDelta = new Vector2(panelRectTransform.sizeDelta.x, height);
        }

        public void OnCloseButtonClicked()
        {
            UIController.HidePage<UISettings>();
        }

        private void OnBackgroundClicked(PointerEventData data)
        {
            UIAudioFeedback.PlayButtonSound();
            UIController.HidePage<UISettings>();
        }
    }
}
