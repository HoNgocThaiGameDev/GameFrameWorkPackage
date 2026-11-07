// DIAGNOSTIC BOUNDARIES ONLY. These stand in for Unity native objects, storage and SDKs.
// A passing test here is not evidence that a Unity scene, serializer or native SDK works.
using NebulaSoft.Analytics;

namespace UnityEngine
{
    public static class Application { public static bool isPlaying => true; }
    public class Object { public static void DontDestroyOnLoad(Object value) { } }
    public class ScriptableObject : Object { }
    public class Component : Object { }
    public class Transform : Component { }
    public class Sprite : Object { }
    public class AudioClip : Object { }
    public class GameObject : Object
    {
        public GameObject(string name) { }
        public T AddComponent<T>() where T : new() => new T();
    }
    public class AudioSource : Component
    {
        public bool playOnAwake, isPlaying;
        public AudioClip clip;
        public float volume;
        public void Play() => isPlaying = true;
    }
    public static class Mathf { public static int Max(int a, int b) => Math.Max(a, b); }
    public static class Debug
    {
        public static void Log(object value) { }
        public static void LogWarning(object value) { }
        public static void LogError(object value, Object context = null) { }
        public static void LogException(Exception value) { }
    }
    public static class JsonUtility
    {
        // The audit never exercises the Unity JSON/native persistence boundary.
        public static string ToJson(object value) => throw new NotSupportedException("Requires Unity");
        public static void FromJsonOverwrite(string json, object value) => throw new NotSupportedException("Requires Unity");
        public static T FromJson<T>(string value) => throw new NotSupportedException("Requires Unity");
    }
    public class SerializeField : Attribute { }
    public class TooltipAttribute : Attribute { public TooltipAttribute(string text) { } }
    public class SerializeReference : Attribute { }
    public class MinAttribute : Attribute { public MinAttribute(float value) { } }
    public class CreateAssetMenuAttribute : Attribute { public string fileName, menuName; }
    public enum RuntimeInitializeLoadType { SubsystemRegistration }
    public class RuntimeInitializeOnLoadMethodAttribute : Attribute { public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType type) { } }
}

namespace NebulaSoft
{
    public class StaticUnloadAttribute : Attribute { }
    public class GroupAttribute : Attribute { public GroupAttribute(string name) { } }
    public class CreateScriptableObjectAttribute : Attribute { }
    public delegate void SimpleCallback();
    public static class AuditExtensions
    {
        public static bool IsNullOrEmpty<T>(this ICollection<T> value) => value == null || value.Count == 0;
    }
    public enum AudioType { Music, Sound }
    public enum PUType { FreezeTimer }
    public static class AudioController
    {
        public static void ApplyDefaultSettings(ref UnityEngine.AudioSource source) { }
        public static float GetVolume(AudioType type) => 1;
    }
    public static class CurrencyHelper { public static string Format(int value) => value.ToString(); }
    public class CurrencyData { public void Init(Currency value) { } }
    public class CurrencyRewardPreviewSettings { public void Init(Currency value) { } }
    public class CurrencyDatabase { public Currency[] Currencies; }
    public class CurrencyRemoteConfigData : RemoteConfigData
    {
        public override string Key => "currencies";
        public Currency GetCurrencyOverride(string id) => null;
        public class Currency { public int defaultCount; }
        public Currency GetCurrencyOverride(CurrencyType type) => null;
    }
    public static class SaveController
    {
        private static readonly Dictionary<string, ISaveObject> objects = new();
        public static bool IsSaveLoaded => true;
        public static int WriteCount;
        public static T GetSaveObject<T>(string key) where T : ISaveObject, new()
        {
            if (!objects.TryGetValue(key, out var value)) objects.Add(key, value = new T());
            return (T)value;
        }
        public static void MarkAsSaveIsRequired() { }
        public static void Save(bool forceSave = false, bool useThreads = true) => WriteCount++;
        public static void SaveChecked() => WriteCount++;
        public static void Reset() { objects.Clear(); WriteCount = 0; }
    }
    public class IAPSettings : UnityEngine.Object
    {
        public bool IsEnabled => true;
        public IAPItem[] StoreItems = Array.Empty<IAPItem>();
        public List<string> ValidateCatalog(string platform) => new();
        public IAPItem FindByKey(string key) => StoreItems.FirstOrDefault(x => x.Key == key);
        public IAPItem FindLegacy(ProductKeyType key) => StoreItems.FirstOrDefault(x => x.ProductKeyType == key);
    }
    public class MonetizationSettings { public IAPSettings IAPSettings; }
    public static class Monetization { public static bool IsActive => true; }
    public class IAPRemoteConfigData { public class IAP { public float price; } }
    public class ProductData { public string GetLocalPrice() => ""; }
    public class IAPWrapper : IDisposable
    {
        public Task Init(IAPSettings settings) { IAPManager.OnModuleInitialized(); return Task.CompletedTask; }
        public void Dispose() { }
        public void RestorePurchases() { }
        public void BuyProduct(string key) { }
        public ProductData GetProductData(string key) => null;
        public bool IsSubscribed(string key) => false;
        public bool IsPurchased(string key) => false;
    }
    public class DummyIAPWrapper : IAPWrapper { }
    public static class FirebaseIAPTransactionLogger { public static void TryFlushPendingTransactions() { } }
    public static class FirebaseNoAdsEntitlementSync { public static void RestoreForCurrentUser() { } }
    public static class SystemMessage { public static void HideLoadingPanel() { } }
    public enum AnalyticsEventType { IAPFirstPurchase }
    public static class AnalyticsController
    {
        public static AnalyticsEvents Events = new(new AnalyticsService());
        public static void TrackEvent(AnalyticsEventType value) { }
        public static void OnCurrencySource(string source, Dictionary<CurrencyType, int> values) { }
        public static void OnCurrencySink(string source, Dictionary<CurrencyType, int> values) { }
    }
}
