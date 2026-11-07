using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NebulaSoft
{
    [System.Serializable]
    public sealed class CurrencyRewardView : RewardView
    {
        [SerializeField] CurrencyData[] currencies;

        [SerializeField] bool spawnCurrencyCloud;

        [ShowIf("spawnCurrencyCloud")]
        [SerializeField] CurrencyType currencyCloudType;
        [ShowIf("spawnCurrencyCloud")]
        [SerializeField] int cloudElementsAmount = 10;
        [ShowIf("spawnCurrencyCloud")]
        [SerializeField] RectTransform currencyCloudSpawnPoint;
        [ShowIf("spawnCurrencyCloud")]
        [SerializeField] RectTransform currencyCloudTargetPoint;

        public CurrencyRewardView() { }
        public CurrencyRewardView(CurrencyData[] currencies, bool spawnCurrencyCloud, CurrencyType currencyCloudType, int cloudElementsAmount, RectTransform currencyCloudSpawnPoint, RectTransform currencyCloudTargetPoint)
        {
            this.currencies = currencies;
            this.spawnCurrencyCloud = spawnCurrencyCloud;
            this.currencyCloudType = currencyCloudType;
            this.cloudElementsAmount = cloudElementsAmount;
            this.currencyCloudSpawnPoint = currencyCloudSpawnPoint;
            this.currencyCloudTargetPoint = currencyCloudTargetPoint;
        }

        protected override void OnInitialized()
        {
            CurrencyReward currencyReward = (CurrencyReward)reward;
            if (currencyReward != null)
            {
                foreach (CurrencyData currencyData in currencies)
                {
                    Currency currency = CurrencyController.GetCurrency(currencyData.CurrencyType);

                    if (currencyData.CurrencyImage != null)
                        currencyData.CurrencyImage.sprite = currency.Icon;

                    if (currencyData.AmountText != null)
                    {
                        int amount = currencyReward.GetAmount(currency.CurrencyType);

                        string numberText = currencyData.FormatTheNumber ? CurrencyHelper.Format(amount) : amount.ToString();
                        currencyData.AmountText.text = string.IsNullOrEmpty(currencyData.TextFormating) ? numberText : string.Format(currencyData.TextFormating == "" ? "{0}" : currencyData.TextFormating, numberText);
                    }
                }
            }
        }

        public override void OnPurchased()
        {
            if (spawnCurrencyCloud)
                FloatingCloud.SpawnCurrency(currencyCloudType.ToString(), currencyCloudSpawnPoint, currencyCloudTargetPoint, cloudElementsAmount, "", null);
        }

        public override void Fill(Reward reward)
        {
            CurrencyReward currencyReward = (CurrencyReward)reward;
            if (currencyReward != null)
            {
                CurrencyAmount[] amounts = currencyReward.Currencies;

                currencies = new CurrencyData[amounts.Length];
                for (int i = 0; i < currencies.Length; i++)
                {
                    currencies[i] = new CurrencyData(amounts[i].CurrencyType);
                }
            }
        }

        [System.Serializable]
        public class CurrencyData
        {
            [SerializeField] CurrencyType currencyType;
            public CurrencyType CurrencyType => currencyType;

            [Space]
            [SerializeField] Image currencyImage;
            public Image CurrencyImage => currencyImage;

            [SerializeField] TextMeshProUGUI amountText;
            public TextMeshProUGUI AmountText => amountText;

            [SerializeField] string textFormating = "x{0}";
            public string TextFormating => textFormating;

            [SerializeField] bool formatTheNumber;
            public bool FormatTheNumber => formatTheNumber;

            public CurrencyData() { }
            public CurrencyData(CurrencyType currencyType)
            {
                this.currencyType = currencyType;
            }
        }
    }
}
