using System;
using UnityEngine;

namespace NebulaSoft
{
    /// <summary>
    /// A single Quest reward. It intentionally supports only the two reward types
    /// used by Quest: a currency amount or one of the existing Power Ups.
    /// </summary>
    [Serializable]
    public sealed class QuestReward
    {
        [SerializeField] QuestRewardType type = QuestRewardType.Currency;
        [SerializeField] CurrencyType currencyType = CurrencyType.Coins;
        [SerializeField] PUType powerUpType = PUType.FreezeTimer;
        [SerializeField] int amount;

        public QuestRewardType Type => type;
        public CurrencyType CurrencyType => currencyType;
        public PUType PowerUpType => powerUpType;
        public int Amount => Mathf.Max(0, amount);
        public bool IsConfigured => Amount > 0;

        public QuestReward() { }

        public QuestReward(CurrencyType currencyType, int amount)
        {
            type = QuestRewardType.Currency;
            this.currencyType = currencyType;
            this.amount = amount;
        }

        public QuestReward(PUType powerUpType, int amount)
        {
            type = QuestRewardType.PowerUp;
            this.powerUpType = powerUpType;
            this.amount = amount;
        }

        public static QuestReward FromLegacy(CurrencyAmount legacyReward)
        {
            return legacyReward == null ? null : new QuestReward(legacyReward.CurrencyType, legacyReward.Amount);
        }

        public bool TryApply(string source)
        {
            if (Amount <= 0)
                return true;

            switch (type)
            {
                case QuestRewardType.Currency:
                    if (CurrencyController.Currencies == null)
                    {
                        Debug.LogWarning("[Quest] Currency system is not ready. Reward was not claimed.");
                        return false;
                    }

                    Currency currency = CurrencyController.GetCurrency(currencyType);
                    if (currency == null)
                    {
                        Debug.LogWarning($"[Quest] Currency '{currencyType}' is not registered. Reward was not claimed.");
                        return false;
                    }

                    CurrencyController.Add(currencyType, Amount, source);
                    return true;

                case QuestRewardType.PowerUp:
                    return false;

                case QuestRewardType.RandomPowerUp:
                    return false;

                default:
                    Debug.LogWarning($"[Quest] Unsupported reward type '{type}'. Reward was not claimed.");
                    return false;
            }
        }

        public Sprite GetIcon()
        {
            switch (type)
            {
                case QuestRewardType.Currency:
#if UNITY_EDITOR
                    if (!Application.isPlaying)
                        return CurrencyController.GetCurrency(currencyType)?.Icon;
#endif
                    return CurrencyController.Currencies != null
                        ? CurrencyController.GetCurrency(currencyType)?.Icon
                        : null;

                case QuestRewardType.PowerUp:
                    return null;

                case QuestRewardType.RandomPowerUp:
                    return null;

                default:
                    return null;
            }
        }
    }
}
