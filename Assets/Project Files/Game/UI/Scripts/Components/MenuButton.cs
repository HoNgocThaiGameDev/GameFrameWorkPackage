using UnityEngine;
using UnityEngine.UI;

namespace NebulaSoft
{
    [RequireComponent(typeof(Button))]
    public class MenuButton : MonoBehaviour
    {
        private void Awake()
        {
            if (!SceneUtils.DoesSceneExist(GameConsts.SCENE_MENU))
            {
                Destroy(gameObject);

                return;
            }

            Button button = GetComponent<Button>();
            button.onClick.AddListener(OnButtonClicked);
        }

        private void OnButtonClicked()
        {
            // Show fullscreen black overlay
            Overlay.Show(0.3f, () =>
            {
                // Save the current state of the game
                SaveController.Save(true);

                // Unload the current level and all the dependencies
                GameController.LoadMenu();
            });
        }
    }
}
