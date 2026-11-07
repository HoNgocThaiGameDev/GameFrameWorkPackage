using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NebulaSoft
{
    [DisallowMultipleComponent]
    public sealed class UIAudioButtonListener : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        private Button button;
        private bool pointerPressed;
        private bool skipNextClickSound;

        private void Awake()
        {
            Bind(GetComponent<Button>());
        }

        public void Bind(Button targetButton)
        {
            if (button != null)
                button.onClick.RemoveListener(PlayButtonSound);

            button = targetButton;
            if (button == null)
                return;

            button.onClick.RemoveListener(PlayButtonSound);
            button.onClick.AddListener(PlayButtonSound);
        }

        private void OnDestroy()
        {
            if (button != null)
                button.onClick.RemoveListener(PlayButtonSound);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            skipNextClickSound = false;
            pointerPressed = IsButtonReady();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!pointerPressed)
                return;

            pointerPressed = false;
            if (!IsButtonReady() || !IsPointerOverButton(eventData))
                return;

            skipNextClickSound = true;
            UIAudioFeedback.PlayButtonSound();
        }

        private void PlayButtonSound()
        {
            if (skipNextClickSound)
            {
                skipNextClickSound = false;
                return;
            }

            if (!IsButtonReady())
                return;

            UIAudioFeedback.PlayButtonSound();
        }

        private bool IsButtonReady()
        {
            return button != null && button.interactable && button.gameObject.activeInHierarchy;
        }

        private bool IsPointerOverButton(PointerEventData eventData)
        {
            GameObject target = eventData.pointerCurrentRaycast.gameObject;
            return target != null && target.transform.IsChildOf(button.transform);
        }
    }
}
