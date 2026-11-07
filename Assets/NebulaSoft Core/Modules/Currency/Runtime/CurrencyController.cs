using System;
using System.Collections.Generic;
using UnityEngine;

namespace NebulaSoft
{
    [StaticUnload]
    public static class CurrencyController
    {
        private static Currency[] currencies;
        public static Currency[] Currencies => currencies;

        private static Dictionary<CurrencyType, int> currenciesLink;
        private static Dictionary<string, Currency> currenciesById;

        private static bool isInitialized;
        private static int notificationDepth;
        private static readonly Queue<Action> pendingNotifications = new Queue<Action>();

        internal static IDisposable DeferNotifications()
        {
            notificationDepth++;
            return new NotificationScope();
        }

        private sealed class NotificationScope : IDisposable
        {
            private bool disposed;
            public void Dispose()
            {
                if (disposed) return;
                disposed = true;
                notificationDepth--;
                if (notificationDepth != 0) return;
                while (pendingNotifications.Count > 0)
                    try { pendingNotifications.Dequeue()(); }
                    catch (Exception exception) { Debug.LogException(exception); }
            }
        }

        private static void Publish(Action notification)
        {
            if (notificationDepth > 0) pendingNotifications.Enqueue(notification);
            else notification();
        }

        public static void Init(CurrencyDatabase currenciesDatabase)
        {
            if (isInitialized) return;

            if (currenciesDatabase == null || currenciesDatabase.Currencies == null)
                throw new ArgumentException("Currency database is missing.", nameof(currenciesDatabase));
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var currency in currenciesDatabase.Currencies)
                if (currency == null || !ids.Add(currency.ID) || currency.DefaultAmount < 0)
                    throw new ArgumentException("Currencies require unique IDs and nonnegative initial balances.");

            // Store active currencies
            currencies = currenciesDatabase.Currencies;

            // Initialize currencies
            foreach (Currency currency in currencies)
            {
                currency.Init();
            }

            // Load currencies from remote config
            CurrencyRemoteConfigData remoteConfigData = RemoteConfigController.TryGetConfig<CurrencyRemoteConfigData>("currencies");

            // Link currencies by the type
            currenciesLink = new Dictionary<CurrencyType, int>();
            currenciesById = new Dictionary<string, Currency>(StringComparer.Ordinal);
            for (int i = 0; i < currencies.Length; i++)
            {
                currenciesById.Add(currencies[i].ID, currencies[i]);
                if (currencies[i].HasLegacyType)
                {
                    currenciesLink.Add(currencies[i].CurrencyType, i);
                }

                Currency.Save save = SaveController.GetSaveObject<Currency.Save>(currencies[i].SaveKey);
                if (save.Amount == -1)
                {
                    int defaultAmount = currencies[i].DefaultAmount;

                    if (remoteConfigData != null)
                    {
                        CurrencyRemoteConfigData.Currency currencyOverride = remoteConfigData.GetCurrencyOverride(currencies[i].ID);
                        if (currencyOverride != null)
                        {
                            defaultAmount = currencyOverride.defaultCount;
                        }
                    }

                    save.Amount = defaultAmount;
                }

                currencies[i].SetSave(save);
            }

            isInitialized = true;
        }

        public static bool HasAmount(CurrencyType currencyType, int amount)
        {
            return amount >= 0 && currencies[currenciesLink[currencyType]].Amount >= amount;
        }

        public static int Get(CurrencyType currencyType)
        {
            return currencies[currenciesLink[currencyType]].Amount;
        }

        public static Currency GetCurrency(CurrencyType currencyType)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                ProjectInitSettings projectInitSettings = RuntimeEditorUtils.GetAsset<ProjectInitSettings>();
                if (projectInitSettings != null)
                {
                    CurrencyInitModule currencyInitModule = projectInitSettings.GetModule<CurrencyInitModule>();
                    if (currencyInitModule != null)
                    {
                        CurrencyDatabase currencyDatabase = currencyInitModule.Database;
                        if (currencyDatabase != null)
                        {
                            return currencyDatabase.Currencies.Find(x => x.CurrencyType.Equals(currencyType));
                        }
                    }
                }

                return null;
            }
#endif

            return currencies[currenciesLink[currencyType]];
        }

        public static void Set(CurrencyType currencyType, int amount)
        {
            Currency currency = currencies[currenciesLink[currencyType]];

            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            currency.Amount = amount;

            // Change save state to required
            SaveController.MarkAsSaveIsRequired();

            // Invoke currency change event
            Publish(() => currency.InvokeChangeEvent(0));
        }

        public static void Add(CurrencyType currencyType, int amount, string analyticsEvent = "")
        {
            Currency currency = currencies[currenciesLink[currencyType]];

            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            currency.Amount = checked(currency.Amount + amount);

            // Change save state to required
            SaveController.MarkAsSaveIsRequired();

            // Invoke currency change event;
            Publish(() => currency.InvokeChangeEvent(amount));

            if(!string.IsNullOrEmpty(analyticsEvent))
                Publish(() => AnalyticsController.OnCurrencySource(analyticsEvent, new Dictionary<CurrencyType, int>() { { currencyType, amount } }));
        }

        public static void Substract(CurrencyType currencyType, int amount, string analyticsEvent = "")
        {
            Currency currency = currencies[currenciesLink[currencyType]];
            if (amount < 0 || currency.Amount < amount) throw new ArgumentOutOfRangeException(nameof(amount));
            currency.Amount -= amount;

            // Change save state to required
            SaveController.MarkAsSaveIsRequired();

            // Invoke currency change event
            Publish(() => currency.InvokeChangeEvent(-amount));

            if (!string.IsNullOrEmpty(analyticsEvent))
                Publish(() => AnalyticsController.OnCurrencySink(analyticsEvent, new Dictionary<CurrencyType, int>() { { currencyType, amount } }));
        }

        public static Currency GetCurrency(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("A currency ID is required.", nameof(id));
            if (currenciesById == null || !currenciesById.TryGetValue(id, out var currency))
                throw new KeyNotFoundException("Currency is not registered: " + id);
            return currency;
        }

        public static int Get(string id) => GetCurrency(id).Amount;
        public static bool HasAmount(string id, int amount) => amount >= 0 && Get(id) >= amount;
        public static bool TrySpend(CurrencyType type, int amount, string sink = "") => TrySpend(currencies[currenciesLink[type]], amount, sink);
        public static bool TrySpend(string id, int amount, string sink = "")
        {
            return id != null && currenciesById != null && currenciesById.TryGetValue(id, out var currency) && TrySpend(currency, amount, sink);
        }

        private static bool TrySpend(Currency currency, int amount, string sink)
        {
            if (amount < 0 || currency.Amount < amount) return false;
            currency.Amount -= amount;
            SaveController.MarkAsSaveIsRequired();
            Publish(() => currency.InvokeChangeEvent(-amount));
            if (!string.IsNullOrEmpty(sink)) ReportCurrency(currency, amount, sink, false);
            return true;
        }

        public static void Add(string id, int amount, string source = "")
        {
            var currency = GetCurrency(id);
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            currency.Amount = checked(currency.Amount + amount);
            SaveController.MarkAsSaveIsRequired();
            Publish(() => currency.InvokeChangeEvent(amount));
            if (!string.IsNullOrEmpty(source)) ReportCurrency(currency, amount, source, true);
        }

        // Validate the complete batch, including duplicate IDs and aggregate overflow, before changing any balance.
        internal static Dictionary<Currency, int> PrepareGrant(CurrencyAmount[] amounts)
        {
            if (amounts == null) throw new ArgumentNullException(nameof(amounts));
            var totals = new Dictionary<Currency, int>();
            foreach (var amount in amounts)
            {
                if (amount == null || amount.Amount < 0) throw new ArgumentException("Reward amounts must be nonnegative.");
                var currency = amount.Currency;
                if (currency == null) throw new ArgumentException("Reward currency is not registered.");
                totals.TryGetValue(currency, out int total);
                totals[currency] = checked(total + amount.Amount);
            }
            foreach (var pair in totals) checked { _ = pair.Key.Amount + pair.Value; }
            return totals;
        }

        internal static void Grant(CurrencyAmount[] amounts, string source)
        {
            var totals = PrepareGrant(amounts);
            foreach (var pair in totals) pair.Key.Amount += pair.Value;
            SaveController.MarkAsSaveIsRequired();
            foreach (var pair in totals)
            {
                Publish(() => pair.Key.InvokeChangeEvent(pair.Value));
                if (!string.IsNullOrEmpty(source)) Publish(() => ReportCurrency(pair.Key, pair.Value, source, true));
            }
        }

        private static void ReportCurrency(Currency currency, int amount, string source, bool earned)
        {
            try
            {
                if (currency.HasLegacyType)
                {
                    var delta = new Dictionary<CurrencyType, int> { { currency.CurrencyType, amount } };
                    if (earned) AnalyticsController.OnCurrencySource(source, delta);
                    else AnalyticsController.OnCurrencySink(source, delta);
                }
                else if (earned) AnalyticsController.Events.EarnCurrency(currency.ID, amount, source);
                else AnalyticsController.Events.SpendCurrency(currency.ID, amount, source);
            }
            catch (Exception exception) { Debug.LogException(exception); }
        }

        public static void Shutdown() => UnloadStatic();

        public static void SubscribeGlobalCallback(CurrencyCallback currencyChange)
        {
            for (int i = 0; i < currencies.Length; i++)
            {
                currencies[i].OnCurrencyChanged += currencyChange;
            }
        }

        public static void UnsubscribeGlobalCallback(CurrencyCallback currencyChange)
        {
            if (!currencies.IsNullOrEmpty())
            {
                for (int i = 0; i < currencies.Length; i++)
                {
                    currencies[i].OnCurrencyChanged -= currencyChange;
                }
            }
        }

        private static void UnloadStatic()
        {
            currencies = null;

            currenciesLink = null;
            currenciesById = null;

            isInitialized = false;
        }
    }

    public delegate void CurrencyCallback(Currency currency, int difference);
}
