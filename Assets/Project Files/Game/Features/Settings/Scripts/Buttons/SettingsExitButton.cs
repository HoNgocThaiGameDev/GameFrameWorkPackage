using UnityEngine;
using UnityEngine.EventSystems;

namespace NebulaSoft
{
    public class SettingsExitButton : SettingsButtonBase
    {
        public override void Init()
        {
#if UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX
            gameObject.SetActive(true);
#else
            gameObject.SetActive(false);
#endif
        }

        public override void OnClick()
        {
            Application.Quit();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
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
