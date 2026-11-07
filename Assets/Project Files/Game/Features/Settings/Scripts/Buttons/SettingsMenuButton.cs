using UnityEngine.EventSystems;

namespace NebulaSoft
{
    public class SettingsMenuButton : SettingsButtonBase
    {
        public override void Init()
        {
            gameObject.SetActive(SceneUtils.DoesSceneExist(GameConsts.SCENE_MENU));
        }

        public override void OnClick()
        {
            UILevelQuitPopUp.Show((confirmed) =>
            {
                if (confirmed)
                {
                    LoadMenu();
                }
            });
        }

        private void LoadMenu()
        {
            // Show fullscreen black overlay
            Overlay.Show(0.3f, () =>
            {
                LivesSystem.UnlockLife(true);

                // Save the current state of the game
                SaveController.Save(true);

                // Unload the current level and all the dependencies
                GameController.LoadMenu();
            });
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
