using TMPro;
using UnityEngine;

namespace NebulaSoft
{
    public sealed class LevelPanel : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI levelText;
        public void Init(int levelIndex) { if (levelText != null) levelText.text = string.Format("LEVEL {0}", levelIndex + 1); }
    }
}
