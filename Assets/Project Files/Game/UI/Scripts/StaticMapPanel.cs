using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NebulaSoft
{
    public sealed class StaticMapPanel : MonoBehaviour
    {
        [SerializeField] Button playButton;
        [SerializeField] TextMeshProUGUI playButtonText;
        [SerializeField] TextMeshProUGUI levelTypeText;
        public void Init()
        {
            playButton?.onClick.AddListener(MenuController.OnPlayButtonClicked);

            if (playButtonText != null)
                playButtonText.text = "PLAY";

            if (levelTypeText != null)
            {
                levelTypeText.text = "LEVEL";
            }
        }
    }
}
