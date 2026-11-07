using UnityEngine;
using UnityEngine.UI;

namespace NebulaSoft
{
    public sealed class AdsRewardsHolder : RewardsHolder
    {
        [Group("Settings"), UniqueID]
        [SerializeField] string rewardID;

        [Group("Settings"), Space]
        [SerializeField] Button adsButton;

        [Group("Settings")]
        [SerializeField] bool disableAfterPurchase;

        [Group("Settings"), Space]
        [SerializeField] string analyticsEvent = "Default";

        private SimpleBoolSave save;

        private void Start()
        {
            InitializeComponents();

            save = SaveController.GetSaveObject<SimpleBoolSave>($"CurrencyProduct_{rewardID}");

            if (disableAfterPurchase && save.Value)
            {
                // Disable holder game object
                gameObject.SetActive(false);

                return;
            }

            adsButton.onClick.AddListener(OnPurchased);
            UIAudioFeedback.RegisterButton(adsButton);
        }

        private void OnPurchased()
        {
            AdsManager.ShowRewardBasedVideo((reward) =>
            {
                if (reward)
                {
                    rewardSet.ApplyReward();

#if MODULE_HAPTIC
                    Haptic.Play(Haptic.HAPTIC_MEDIUM, HapticPriority.Outcome);
#endif

                    save.Value = true;

                    if (disableAfterPurchase)
                    {
                        // Disable holder game object
                        gameObject.SetActive(false);
                    }

                    SaveController.MarkAsSaveIsRequired();
                }
            }, analyticsEvent);
        }
    }
}
