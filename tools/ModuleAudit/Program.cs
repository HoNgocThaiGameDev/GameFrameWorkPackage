using System.Reflection;
using System.Text.Json;
using NebulaSoft;
using NebulaSoft.Ads;
using NebulaSoft.Analytics;

// Exit 1 means one or more safety assertions failed.
internal static class Program
{
    private static readonly List<object> results = new();
    private static int failures;
    private static int Main(string[] args)
    {
        if (args.Contains("--hash")) { Console.WriteLine("currency:0".GetHashCode()); return 0; }
        Run("BASE-ADS", "Repeated reward and close grant exactly once", AdsDuplicateReward);
        Run("BASE-IAP", "Same successful transaction cannot grant twice", IapDedup);
        Run("BASE-DAILY", "Two ordinary claims on the same UTC day grant once", DailyOrdinaryClaim);
        Run("AUD-01", "IAP retry after observer exception does not duplicate coins", IapObserverFailure);
        Run("AUD-02", "Distinct save identities remain isolated despite a hash collision", SaveHashCollision);
        Run("AUD-04", "Volume change preserves the clip's gain", AudioGain);
        Run("AUD-06", "Analytics can retry initialization after transient failure", AnalyticsRetry);
        Run("AUD-09", "Remote parser preserves braces inside JSON string values", RemoteJsonBraces);
        Run("AUD-10", "Daily reward cannot be claimed twice through a synchronous observer", DailyReentry);
        Run("AUD-11", "Reading a missed daily reward does not write storage repeatedly", DailyReadWrites);
        string output = args.SkipWhile(x => x != "--output").Skip(1).FirstOrDefault();
        string json = JsonSerializer.Serialize(new { runtime = Environment.Version.ToString(), boundary = "Standalone .NET; Unity, storage and SDKs are mocked", total = results.Count, failures, results }, new JsonSerializerOptions { WriteIndented = true });
        if (output != null) { Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))); File.WriteAllText(output, json); }
        Console.WriteLine($"TOTAL: {results.Count - failures} passed, {failures} failed safety assertions.");
        return failures == 0 ? 0 : 1;
    }
    private static void Run(string id, string name, Action test)
    {
        try { test(); results.Add(new { id, name, passed = true, observed = "Safety assertion satisfied" }); Console.WriteLine($"PASS {id}: {name}"); }
        catch (Exception error) { failures++; results.Add(new { id, name, passed = false, observed = error.Message }); Console.WriteLine($"FAIL {id}: {error.Message}"); }
    }
    private static void Check(bool condition, string observed) { if (!condition) throw new Exception(observed); }
    private static void Set(object target, string field, object value) =>
        (target as Type ?? target.GetType()).GetField(field, BindingFlags.NonPublic | (target is Type ? BindingFlags.Static : BindingFlags.Instance)).SetValue(target is Type ? null : target, value);
    private static void ResetIap() => typeof(IAPManager).GetMethod("UnloadStatic", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
    private static Currency SetupCurrency()
    {
        SaveController.Reset();
        var currency = new Currency();
        currency.SetSave(new Currency.Save { Amount = 0 });
        Set(typeof(CurrencyController), "currencies", new[] { currency });
        Set(typeof(CurrencyController), "currenciesLink", new Dictionary<CurrencyType, int> { [CurrencyType.Coins] = 0 });
        return currency;
    }
    private static IAPItem SetupIap()
    {
        ResetIap();
        Set(typeof(IAPManager), "save", new IAPManager.Save());
        var set = new RewardsSet();
        set.Rewards.Add(new CoinReward());
        var item = new IAPItem();
        Set(item, "rewardsSet", set);
        item.Init();
        return item;
    }
    private static void IapDedup()
    {
        SetupCurrency(); var item = SetupIap();
        IAPManager.TryFulfillPurchase(item, "order-1"); IAPManager.TryFulfillPurchase(item, "order-1");
        Check(CurrencyController.Get(CurrencyType.Coins) == 100 && item.TimesPurchased == 1, "Successful transaction duplicated");
    }
    private static void IapObserverFailure()
    {
        var currency = SetupCurrency(); var item = SetupIap();
        CurrencyCallback observer = (_, _) => throw new InvalidOperationException("UI observer failed");
        currency.OnCurrencyChanged += observer;
        try { IAPManager.TryFulfillPurchase(item, "order-observer"); } catch (InvalidOperationException) { }
        int firstBalance = currency.Amount;
        currency.OnCurrencyChanged -= observer;
        IAPManager.TryFulfillPurchase(item, "order-observer");
        Check(currency.Amount == 100, $"First attempt coins={firstBalance}; same transaction retried coins={currency.Amount}; TimesPurchased={item.TimesPurchased}");
    }
    private static void SaveHashCollision()
    {
        var seen = new Dictionary<int, string>(); string first = null, second = null; int collision = 0;
        // A collision is overwhelmingly likely within this bound; fail explicitly if absent.
        for (int i = 0; i < 1_000_000; i++)
        {
            string key = "audit_save_" + i; int hash = key.GetHashCode();
            if (seen.TryGetValue(hash, out first)) { second = key; collision = hash; break; }
            seen.Add(hash, key);
        }
        Check(second != null, "No collision found in bound; rerun the diagnostic");
        var save = new GlobalSave(); save.Init(0);
        var a = save.GetSaveObject<ProbeSave>(first); a.Value = 71;
        var b = save.GetSaveObject<ProbeSave>(second);
        Check(!ReferenceEquals(a, b), $"{first} and {second} hash to {collision}; same save object returned, second.Value={b.Value}");
    }
    private static void AudioGain()
    {
        var sound = new AudioSourceCase(); sound.Play(new UnityEngine.AudioClip(), 0.8f);
        sound.OverrideVolume(AudioType.Sound, 0.5f);
        Check(Math.Abs(sound.AudioSource.volume - 0.4f) < 0.001f, $"Expected active volume=0.4; actual={sound.AudioSource.volume}");
    }
    private static void AnalyticsRetry()
    {
        using var service = new AnalyticsService(); var provider = new AnalyticsProvider { FailFirst = true };
        service.Track(new AnalyticsEvent("queued_event"));
        bool first = service.InitializeAsync(provider).GetAwaiter().GetResult();
        bool second = service.InitializeAsync(provider).GetAwaiter().GetResult();
        Check(!first && second && service.IsReady && service.PendingCount == 0, $"First={first}; retry={second}; provider initialization calls={provider.Calls}; pending={service.PendingCount}");
    }
    private static void RemoteJsonBraces()
    {
        const string input = "{\"message\":{\"text\":\"a}b\",\"value\":1}}";
        var method = typeof(RemoteConfigController).GetMethod("ParseJsonLikeString", BindingFlags.Static | BindingFlags.NonPublic);
        var result = (Dictionary<string, string>)method.Invoke(null, new object[] { input });
        Check(result.ContainsKey("message"), "Parser lost the message key");
        try { using var doc = JsonDocument.Parse(result["message"]); Check(doc.RootElement.GetProperty("text").GetString() == "a}b", "Message changed"); }
        catch (JsonException) { throw new Exception($"Valid input JSON became invalid extracted JSON: {result["message"]}"); }
    }
    private static MemoryDailyStore SetupDaily(DateTime? lastClaim = null)
    {
        typeof(DailyRewardService).GetMethod("UnloadStatic", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
        var db = new DailyRewardDatabase();
        db.ReplaceSchedule(1, Enumerable.Range(0, 7).Select(i => new DailyRewardDayDefinition("day-" + i, new DailyRewardGrant(CurrencyType.Coins, 10))).ToArray());
        var store = new MemoryDailyStore();
        if (lastClaim.HasValue) { store.State.LastClaimedUtcDateKey = lastClaim.Value.ToString("yyyy-MM-dd"); store.State.NextDayIndex = 3; }
        DailyRewardService.Init(db, new FixedClock(), store); return store;
    }
    private static void DailyOrdinaryClaim()
    {
        SetupCurrency(); SetupDaily();
        bool first = DailyRewardService.TryClaimToday(), second = DailyRewardService.TryClaimToday();
        Check(first && !second && CurrencyController.Get(CurrencyType.Coins) == 10, "Ordinary claim duplicated");
    }
    private static void DailyReentry()
    {
        var currency = SetupCurrency(); SetupDaily(); bool reentered = false, nestedClaim = false;
        currency.OnCurrencyChanged += (_, _) => { if (!reentered) { reentered = true; nestedClaim = DailyRewardService.TryClaimToday(); } };
        bool outer = DailyRewardService.TryClaimToday();
        Check(outer && !nestedClaim && currency.Amount == 10, $"outer={outer}; nested={nestedClaim}; same-day coins={currency.Amount} (expected 10)");
    }
    private static void DailyReadWrites()
    {
        SetupCurrency(); var store = SetupDaily(new DateTime(2026, 11, 1)); store.Writes = 0;
        for (int i = 0; i < 5; i++) DailyRewardService.GetState();
        Check(store.Writes == 0, $"Five GetState calls after initial normalization caused {store.Writes} additional storage writes");
    }
    private static void AdsDuplicateReward()
    {
        var provider = new AdsProvider(); var placement = new AdPlacementDefinition("revive", AdFormat.Rewarded, preload: false);
        using var service = new AdsService(provider, new[] { placement }, () => 0);
        service.InitializeAsync().GetAwaiter().GetResult(); int rewards = 0;
        service.AdEvent += value => { if (value.Kind == AdEventKind.RewardGranted) rewards++; };
        var task = service.ShowAsync("revive");
        provider.Emit(AdEventKind.Started); provider.Emit(AdEventKind.RewardGranted); provider.Emit(AdEventKind.RewardGranted); provider.Emit(AdEventKind.Closed); provider.Emit(AdEventKind.Closed);
        Check(task.Result.RewardGranted && rewards == 1 && !service.IsFullscreenBusy, "Reward deduplication or fullscreen lock failed");
    }
    private sealed class CoinReward : Reward { public override void ApplyReward() => CurrencyController.Add(CurrencyType.Coins, 100); }
    public sealed class ProbeSave : ISaveObject { public int Value; public void Flush() { } }
    private sealed class FixedClock : IDailyRewardClock { public DateTime UtcNow => new(2026, 11, 7, 12, 0, 0, DateTimeKind.Utc); }
    private sealed class MemoryDailyStore : IDailyRewardStateStore
    {
        public DailyRewardSave State = new(); public int Writes;
        public DailyRewardSave Load() => State;
        public void Save(DailyRewardSave state) { State = state; Writes++; }
    }
    private sealed class AnalyticsProvider : IAnalyticsProvider
    {
        public int Calls; public bool FailFirst;
        public Task InitializeAsync(CancellationToken cancellation) { Calls++; return Calls == 1 && FailFirst ? Task.FromException(new IOException("Transient initialization failure")) : Task.CompletedTask; }
        public void SetCollectionEnabled(bool value) { }
        public void SetUserId(string value) { }
        public void SetUserProperty(string name, string value) { }
        public void LogEvent(AnalyticsEvent value) { }
        public void Dispose() { }
    }
    private sealed class AdsProvider : IAdsProvider
    {
        public string Id => "audit"; private AdRequest request;
        public event Action<AdEvent> AdEvent;
        public event Action<BannerInfo> BannerChanged;
        public Task<bool> InitializeAsync(CancellationToken cancellation) => Task.FromResult(true);
        public AdState GetState(AdPlacementDefinition placement) => AdState.Ready;
        public void Load(AdPlacementDefinition placement) { }
        public void Show(AdRequest value) => request = value;
        public void ShowBanner(AdRequest value) { }
        public void HideBanner(bool destroy) { }
        public void Tick() { }
        public void Dispose() { }
        public void Emit(AdEventKind kind) => AdEvent?.Invoke(new AdEvent(request.Id, request.Placement.Id, request.Placement.Format, kind, Id));
    }
}
