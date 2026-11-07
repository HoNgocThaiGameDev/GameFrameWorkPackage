using UnityEngine;
using UnityEngine.UI;

namespace NebulaSoft
{
    [RequireComponent(typeof(Button))]
    public sealed class PopupCloseButton : MonoBehaviour
    {
        [SerializeField] private Canvas targetCanvas;

        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(Close);
        }

        private void Close()
        {
            if (targetCanvas != null)
            {
                targetCanvas.enabled = false;
            }
        }
    }
}
