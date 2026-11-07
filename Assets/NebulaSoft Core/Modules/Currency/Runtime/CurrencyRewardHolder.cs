using UnityEngine;

namespace NebulaSoft
{
    public sealed class CurrencyRewardsHolder : RewardsHolder
    {
        [Group("Settings"), UniqueID]
        [SerializeField] string rewardID;

        [Group("Settings")]
        [SerializeField] UICurrencyButton currencyButton;

        [Group("Settings")]
        [SerializeField] CurrencyAmount price;

        [Group("Settings"), Space]
        [SerializeField] bool disableAfterPurchase;

        private SimpleBoolSave save;

        private void Start()
        {
            InitializeComponents();

            save = SaveController.GetSaveObject<SimpleBoolSave>($"CurrencyProduct_{rewardID}");

            if(disableAfterPurchase && save.Value)
            {
                // Disable offer game object
                gameObject.SetActive(false);

                return;
            }

            currencyButton.Init(price.Amount, price.CurrencyType);
            currencyButton.Purchased += OnPurchased;
        }

        private void OnPurchased()
        {
            rewardSet.ApplyReward();

            save.Value = true;

            if(disableAfterPurchase)
            {
                // Disable holder game object
                gameObject.SetActive(false);
            }

            SaveController.MarkAsSaveIsRequired();
        }
    }
}
