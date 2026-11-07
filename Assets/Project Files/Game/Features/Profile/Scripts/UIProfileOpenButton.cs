using UnityEngine;
using UnityEngine.UI;

namespace NebulaSoft
{
    [RequireComponent(typeof(Button))]
    public class UIProfileOpenButton : MonoBehaviour
    {
        [SerializeField] Button button;
        [SerializeField] Image avatarImage;
        [SerializeField] Image frameImage;

        private void Awake()
        {
            EnsureReferences();

            button.onClick.RemoveListener(OpenProfile);
            button.onClick.AddListener(OpenProfile);
        }

        private void OnEnable()
        {
            UIProfilePopup.ProfileSaved -= OnProfileSaved;
            UIProfilePopup.ProfileSaved += OnProfileSaved;

            SaveController.OnSaveLoaded -= RefreshFromLocalSave;
            SaveController.OnSaveLoaded += RefreshFromLocalSave;

            RefreshFromLocalSave();
        }

        private void OnDisable()
        {
            UIProfilePopup.ProfileSaved -= OnProfileSaved;
            SaveController.OnSaveLoaded -= RefreshFromLocalSave;
        }

        private void OpenProfile()
        {
            UIController.ShowPage<UIProfilePopup>();
        }

        private void OnProfileSaved(int avatarIndex, int frameIndex, Sprite avatarSprite, Sprite frameSprite, string playerName)
        {
            ApplyProfileSprites(avatarSprite, frameSprite);
        }

        private void RefreshFromLocalSave()
        {
            if (!SaveController.IsSaveLoaded)
                return;

            UIProfilePopup profilePopup = FindProfilePopup();
            if (profilePopup == null)
                return;

            PlayerProfileSave profileSave = FirebaseProfileHandler.GetLocalProfile();
            ApplyProfileSprites(
                profilePopup.GetAvatarSprite(profileSave.AvatarIndex),
                profilePopup.GetFrameSprite(profileSave.FrameIndex));
        }

        private void ApplyProfileSprites(Sprite avatarSprite, Sprite frameSprite)
        {
            EnsureReferences();

            if (avatarImage != null && avatarSprite != null)
                avatarImage.sprite = avatarSprite;

            if (frameImage != null && frameSprite != null)
                frameImage.sprite = frameSprite;
        }

        private void EnsureReferences()
        {
            if (button == null)
                button = GetComponent<Button>();

            if (avatarImage == null)
            {
                Transform avatar = transform.Find("Avatar");
                if (avatar != null)
                    avatarImage = avatar.GetComponent<Image>();
            }

            if (frameImage == null)
            {
                Transform frame = transform.Find("Frame");
                if (frame != null)
                    frameImage = frame.GetComponent<Image>();
            }
        }

        private static UIProfilePopup FindProfilePopup()
        {
            UIProfilePopup[] popups = Resources.FindObjectsOfTypeAll<UIProfilePopup>();
            for (int i = 0; i < popups.Length; i++)
            {
                UIProfilePopup popup = popups[i];
                if (popup != null && popup.gameObject.scene.IsValid())
                    return popup;
            }

            return null;
        }
    }
}
