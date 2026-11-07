using UnityEngine;
using UnityEngine.UI;

namespace NebulaSoft
{
    [DisallowMultipleComponent]
    public sealed class UIHapticButtonListener : MonoBehaviour
    {
        private Button button;

        private void Awake()
        {
            Bind(GetComponent<Button>());
        }

        public void Bind(Button targetButton)
        {
            if (button != null)
                button.onClick.RemoveListener(PlayHaptic);

            button = targetButton;
            if (button == null)
                return;

            button.onClick.RemoveListener(PlayHaptic);
            button.onClick.AddListener(PlayHaptic);
        }

        private void OnDestroy()
        {
            if (button != null)
                button.onClick.RemoveListener(PlayHaptic);
        }

        private static void PlayHaptic()
        {
            Haptic.Play(Haptic.HAPTIC_LIGHT, HapticPriority.Tap);
        }
    }
}
