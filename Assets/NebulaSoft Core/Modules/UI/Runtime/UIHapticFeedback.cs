using UnityEngine;
using UnityEngine.UI;

namespace NebulaSoft
{
    public static class UIHapticFeedback
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

            UIHapticButtonListener listener = button.GetComponent<UIHapticButtonListener>();
            if (listener == null)
                listener = button.gameObject.AddComponent<UIHapticButtonListener>();

            listener.Bind(button);
        }
    }

}
