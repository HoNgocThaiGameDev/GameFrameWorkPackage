using UnityEngine;
using UnityEngine.UI;

namespace NebulaSoft
{
    public sealed class UIComplete : UIPage
    {
        [SerializeField] Button nextLevelButton;
        [SerializeField] Button extraRewardButton;
        public override void Init()
        {
            nextLevelButton?.onClick.AddListener(GameController.OnCompleteRewardRecieved);
            extraRewardButton?.onClick.AddListener(GameController.OnCompleteRewardRecieved);
        }
        public override void PlayShowAnimation() => UIController.OnPageOpened(this);
        public override void PlayHideAnimation() => UIController.OnPageClosed(this);
    }
}
