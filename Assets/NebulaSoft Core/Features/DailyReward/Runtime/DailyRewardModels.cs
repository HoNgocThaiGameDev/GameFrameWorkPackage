using System;
using System.Collections.Generic;
using UnityEngine;

namespace NebulaSoft
{
    public enum DailyRewardGrantType
    {
        Currency,
        PowerUp
    }

    public enum DailyRewardCellState
    {
        Locked,
        Current,
        Claimed
    }

    [Serializable]
    public sealed class DailyRewardGrant
    {
        [SerializeField] DailyRewardGrantType type = DailyRewardGrantType.Currency;
        [SerializeField] CurrencyType currencyType = CurrencyType.Coins;
        [SerializeField] PUType powerUpType = PUType.FreezeTimer;
        [SerializeField, Min(1)] int amount = 1;

        public DailyRewardGrantType Type => type;
        public CurrencyType CurrencyType => currencyType;
        public PUType PowerUpType => powerUpType;
        public int Amount => Mathf.Max(0, amount);

        public DailyRewardGrant() { }

        public DailyRewardGrant(CurrencyType rewardCurrencyType, int rewardAmount)
        {
            type = DailyRewardGrantType.Currency;
            currencyType = rewardCurrencyType;
            amount = rewardAmount;
        }

        public DailyRewardGrant(PUType rewardPowerUpType, int rewardAmount)
        {
            type = DailyRewardGrantType.PowerUp;
            powerUpType = rewardPowerUpType;
            amount = rewardAmount;
        }

        public bool IsAvailable()
        {
            if (Amount <= 0)
                return false;

            switch (type)
            {
                case DailyRewardGrantType.Currency:
                    return CurrencyController.Currencies != null
                        && CurrencyController.GetCurrency(currencyType) != null;

                case DailyRewardGrantType.PowerUp:
                    return false;

                default:
                    return false;
            }
        }

        public void Apply(string source)
        {
            switch (type)
            {
                case DailyRewardGrantType.Currency:
                    CurrencyController.Add(currencyType, Amount, source);
                    break;

                case DailyRewardGrantType.PowerUp:
                    break;
            }
        }

        public Sprite GetIcon()
        {
            switch (type)
            {
                case DailyRewardGrantType.Currency:
                    return CurrencyController.Currencies != null
                        ? CurrencyController.GetCurrency(currencyType)?.Icon
                        : null;

                case DailyRewardGrantType.PowerUp:
                    return null;

                default:
                    return null;
            }
        }

        public string GetAmountLabel()
        {
            return type == DailyRewardGrantType.Currency
                ? Amount + " coins"
                : "x" + Amount;
        }
    }

    [Serializable]
    public sealed class DailyRewardDayDefinition
    {
        [SerializeField] string id;
        [SerializeField] List<DailyRewardGrant> rewards = new List<DailyRewardGrant>();

        public string Id => id;
        public IReadOnlyList<DailyRewardGrant> Rewards => rewards;

        public DailyRewardDayDefinition() { }

        public DailyRewardDayDefinition(string rewardId, params DailyRewardGrant[] rewardGrants)
        {
            id = rewardId;
            rewards = rewardGrants == null
                ? new List<DailyRewardGrant>()
                : new List<DailyRewardGrant>(rewardGrants);
        }
    }

    public interface IDailyRewardClock : IClock { }

    public interface IDailyRewardStateStore
    {
        DailyRewardSave Load();
        void Save(DailyRewardSave save);
    }

    public interface IDailyRewardScheduleProvider
    {
        int ScheduleVersion { get; }
        IReadOnlyList<DailyRewardDayDefinition> Days { get; }
    }

    public sealed class LocalDailyRewardClock : IDailyRewardClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }

    public sealed class LocalDailyRewardStateStore : IDailyRewardStateStore
    {
        public DailyRewardSave Load()
        {
            return SaveController.GetSaveObject<DailyRewardSave>(DailyRewardService.SaveKey);
        }

        public void Save(DailyRewardSave save)
        {
            SaveController.MarkAsSaveIsRequired();
            SaveController.SaveChecked();
        }
    }

    public struct DailyRewardState
    {
        public readonly int CurrentDayIndex;
        public readonly int ClaimedDayIndex;
        public readonly bool CanClaimToday;
        public readonly bool IsClaimedToday;

        public DailyRewardState(int currentDayIndex, int claimedDayIndex, bool canClaimToday, bool isClaimedToday)
        {
            CurrentDayIndex = currentDayIndex;
            ClaimedDayIndex = claimedDayIndex;
            CanClaimToday = canClaimToday;
            IsClaimedToday = isClaimedToday;
        }
    }
}
