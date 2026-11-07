using UnityEngine;
using UnityEngine.UI;

namespace NebulaSoft
{
    public static class UIAudioFeedback
    {
        public static void RegisterButtons(Transform root)
        {
            if (root == null)
                return;

            Button[] buttons = root.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
                RegisterButton(buttons[i]);
        }

        public static void RegisterButton(Button button)
        {
            if (button == null)
                return;

            UIAudioButtonListener listener = button.GetComponent<UIAudioButtonListener>();
            if (listener == null)
                listener = button.gameObject.AddComponent<UIAudioButtonListener>();

            listener.Bind(button);
        }

        public static void PlayButtonSound()
        {
            AudioClips audioClips = AudioController.AudioClips;
            if (audioClips == null || audioClips.buttonSound == null)
                return;

            AudioController.PlaySound(audioClips.buttonSound);
        }
    }
}
