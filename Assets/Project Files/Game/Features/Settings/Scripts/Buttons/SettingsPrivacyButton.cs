using UnityEngine;
using UnityEngine.EventSystems;

namespace NebulaSoft
{
    public class SettingsPrivacyButton : SettingsButtonBase
    {
        private string url;

        public override void Init()
        {
#if MODULE_MONETIZATION
            if(Monetization.IsActive)
            {
                url = Monetization.Settings.PrivacyLink;
                if(string.IsNullOrEmpty(url))
                    gameObject.SetActive(false);
            }
            else
            {
                gameObject.SetActive(false);
            }
#else
            gameObject.SetActive(false);
#endif
        }

        public override void OnClick()
        {
            if (string.IsNullOrEmpty(url)) return;

            Application.OpenURL(url);
        }

        public override void Select()
        {
            IsSelected = true;

            Button.Select();

            EventSystem.current.SetSelectedGameObject(null); //clear any previous selection (best practice)
            EventSystem.current.SetSelectedGameObject(Button.gameObject, new BaseEventData(EventSystem.current));
        }

        public override void Deselect()
        {
            IsSelected = false;

            EventSystem.current.SetSelectedGameObject(null);
        }
    }
}
