#pragma warning disable 0649 

using UnityEngine;
using UnityEngine.UI;

namespace NebulaSoft
{
    public class SettingsHapticToggleButton : SettingsButtonBase
    {
        [SerializeField] Image imageRef;
        [SerializeField] Image selectionImage;

        [Space]
        [SerializeField] Sprite activeSprite;
        [SerializeField] Sprite disableSprite;

        private bool isActive = true;

        private TweenCase selectionFadeCase;

        public override void Init()
        {

        }

        private void OnEnable()
        {
            isActive = Haptic.IsActive;

            Redraw();

            Haptic.StateChanged += OnStateChanged;
        }

        private void OnDisable()
        {
            Haptic.StateChanged -= OnStateChanged;
        }

        private void OnStateChanged(bool value)
        {
            isActive = value;

            Redraw();
        }

        private void Redraw()
        {
            imageRef.sprite = isActive ? activeSprite : disableSprite;
        }

        public override void OnClick()
        {
            Haptic.IsActive = !isActive;
        }

        public override void Select()
        {
            IsSelected = true;

            selectionFadeCase.KillActive();

            selectionImage.gameObject.SetActive(true);
            selectionImage.color = selectionImage.color.SetAlpha(0.0f);
            selectionFadeCase = selectionImage.DOFade(0.2f, 0.2f);
        }

        public override void Deselect()
        {
            IsSelected = false;

            selectionFadeCase.KillActive();

            selectionImage.gameObject.SetActive(false);
            selectionImage.color = selectionImage.color.SetAlpha(0.0f);
        }
    }
}
