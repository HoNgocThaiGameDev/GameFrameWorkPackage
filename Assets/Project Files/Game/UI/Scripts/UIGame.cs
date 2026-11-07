using UnityEngine;

namespace NebulaSoft
{
    public sealed class UIGame : UIPage
    {
        [SerializeField] RectTransform safeAreaRectTransform;
        [SerializeField] TimerVisualiser gameplayTimer;
        [SerializeField] MessageBox messageBox;
        [SerializeField] PUUIController powerUpsUIController;

        public TimerVisualiser GameplayTimer => gameplayTimer;
        public MessageBox MessageBox => messageBox;
        public PUUIController PowerUpsUIController => powerUpsUIController;

        public override void Init()
        {
            messageBox?.Init();
            powerUpsUIController?.Init();
            NotchSaveArea.RegisterRectTransform(safeAreaRectTransform);
        }

        public override void PlayHideAnimation() { gameplayTimer?.Hide(); UIController.OnPageClosed(this); }
        public override void PlayShowAnimation() => UIController.OnPageOpened(this);
    }
}

