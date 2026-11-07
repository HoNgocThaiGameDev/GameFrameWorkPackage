#pragma warning disable 0649 

using UnityEngine;

namespace NebulaSoft
{
    public class SettingsLinkButton : SettingsButtonBase
    {
        [SerializeField] string url;

        public override void Init()
        {

        }

        public override void OnClick()
        {
            Application.OpenURL(url);
        }
    }
}
