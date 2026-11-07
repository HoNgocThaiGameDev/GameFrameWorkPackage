using UnityEngine;
using UnityEngine.UI;

namespace NebulaSoft
{
    [RequireComponent(typeof(DevPanel))]
    public sealed class DevPanelGameButtons : MonoBehaviour
    {
        [SerializeField] Button failLevelButton;
        [SerializeField] Button completeLevelButton;
        [SerializeField] Button getMoneyButton;
        private DevPanel devPanel;
        private void Awake()
        {
            devPanel = GetComponent<DevPanel>();
            failLevelButton?.onClick.AddListener(() => { GameController.GameOver(false); devPanel.DisablePanel(); });
            completeLevelButton?.onClick.AddListener(() => { GameController.GameComplete(); GameController.ShowCompleteUI(); devPanel.DisablePanel(); });
            getMoneyButton?.onClick.AddListener(() => CurrencyController.Add(CurrencyType.Coins, 1000));
        }
    }
}
