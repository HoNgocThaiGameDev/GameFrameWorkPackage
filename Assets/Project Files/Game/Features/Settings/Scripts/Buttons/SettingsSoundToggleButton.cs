#pragma warning disable 649

using System;
using UnityEngine;
using UnityEngine.UI;

namespace NebulaSoft
{
    public class SettingsSoundToggleButton : SettingsButtonBase
    {
        [SerializeField] bool universal;

        [HideIf("universal")]
        [SerializeField] AudioType type;

        [Space]
        [SerializeField] Image imageRef;
        [SerializeField] Image selectionImage;

        [Space]
        [SerializeField] Sprite activeSprite;
        [SerializeField] Sprite disableSprite;

        private bool isActive = true;

        private AudioType[] availableAudioTypes;
        private TweenCase selectionFadeCase;

        public override void Init()
        {
            if(universal)
            {
                availableAudioTypes = EnumUtils.GetEnumArray<AudioType>();
            }
        }

        private void OnEnable()
        {
            isActive = GetState();

            Redraw();

            AudioController.VolumeChanged += OnVolumeChanged;
        }

        private void OnDisable()
        {
            AudioController.VolumeChanged -= OnVolumeChanged;
        }

        private void Redraw()
        {
            imageRef.sprite = isActive ? activeSprite : disableSprite;
        }

        public override void OnClick()
        {
            isActive = !isActive;

            SetState(isActive);
        }

        private void OnVolumeChanged(AudioType audioType, float volume)
        {
            if (universal || audioType == type)
            {
                isActive = GetState();

                Redraw();
            }
        }

        private bool GetState()
        {
            if(universal)
            {
                foreach(AudioType audioType in availableAudioTypes)
                {
                    if (!AudioController.IsAudioTypeActive(audioType))
                        return false;
                }

                return true;
            }

            return AudioController.IsAudioTypeActive(type);
        }

        private void SetState(bool state)
        {
            float volume = state ? 1.0f : 0.0f;

            if (universal)
            {
                foreach (AudioType audioType in availableAudioTypes)
                {
                    AudioController.SetVolume(audioType, volume);
                }

                return;
            }

            AudioController.SetVolume(type, volume);
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
