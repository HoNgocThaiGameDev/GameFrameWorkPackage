using System.Collections.Generic;
using UnityEngine;

namespace NebulaSoft
{
    [System.Serializable]
    public class CurrencyAmount
    {
        private const string TEXT_FORMAT = "<sprite name={0}>{1}";

        [SerializeField] CurrencyType currencyType;
        public CurrencyType CurrencyType => currencyType;
        [SerializeField] string currencyId;
        public string CurrencyId => string.IsNullOrEmpty(currencyId) ? currencyType.ToString() : currencyId;

        [SerializeField] int amount;
        public int Amount => amount;

        public Currency Currency => string.IsNullOrEmpty(currencyId) ? CurrencyController.GetCurrency(currencyType) : CurrencyController.GetCurrency(currencyId);
        public string FormattedPrice => CurrencyHelper.Format(amount);

        public CurrencyAmount(CurrencyType currencyType, int amount)
        {
            this.currencyType = currencyType;
            this.amount = amount;
        }

        public CurrencyAmount(string currencyId, int amount)
        {
            this.currencyId = currencyId;
            this.amount = amount;
        }

        public bool EnoughMoneyOnBalance()
        {
            return CurrencyController.HasAmount(CurrencyId, amount);
        }

        public void SubstractFromBalance(string sink = "")
        {
            if (!CurrencyController.TrySpend(CurrencyId, amount, sink))
                throw new System.InvalidOperationException("The currency balance cannot cover this price.");
        }

        public void AddToBalance(string source = "")
        {
            CurrencyController.Add(CurrencyId, amount, source);
        }

        public string GetTextWithIcon()
        {
            return string.Format(TEXT_FORMAT, CurrencyId, amount);
        }
    }
}
