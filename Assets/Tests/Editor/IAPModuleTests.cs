using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace NebulaSoft.Tests
{
    public sealed class IAPModuleTests
    {
        private IAPSettings settings;
        private RewardsSet rewards;
        private CountingReward reward;
        private IAPItem product;
        private TestStore store;
        private string directory;
        private object previousSave, previousLoaded, previousPath, previousSaveRequired;
        private const BindingFlags Flags = BindingFlags.Static | BindingFlags.NonPublic;

        [SetUp]
        public void SetUp()
        {
            ResetManager();
            previousSave = typeof(SaveController).GetField("globalSave", Flags).GetValue(null);
            previousLoaded = typeof(SaveController).GetField("isSaveLoaded", Flags).GetValue(null);
            previousSaveRequired = typeof(SaveController).GetField("isSaveRequired", Flags).GetValue(null);
            previousPath = typeof(Serializer).GetField("persistentDataPath", Flags).GetValue(null);
            var global = new GlobalSave();
            global.Init(0);
            Set(typeof(SaveController), "globalSave", global);
            Set(typeof(SaveController), "isSaveLoaded", true);
            directory = Path.Combine(Path.GetTempPath(), "NebulaIAPTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            Set(typeof(Serializer), "persistentDataPath", directory);
            rewards = ScriptableObject.CreateInstance<RewardsSet>();
            reward = new CountingReward();
            rewards.Rewards.Add(reward);
            product = JsonUtility.FromJson<IAPItem>("{\"key\":\"new_pack\",\"useLegacyKey\":false,\"androidID\":\"com.test.newpack\",\"iOSID\":\"com.test.newpack.ios\",\"productType\":0}");
            typeof(IAPItem).GetField("rewardsSet", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(product, rewards);
            settings = ScriptableObject.CreateInstance<IAPSettings>();
            typeof(IAPSettings).GetField("storeItems", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(settings, new[] { product });
            IAPManager.ConfigureModule(settings);
            product.Init();
            Set(typeof(IAPManager), "save", global.GetSaveObject<IAPManager.Save>("iapGlobalSave"));
            store = new TestStore();
            Set(typeof(IAPManager), "wrapper", store);
            IAPManager.OnModuleInitialized();
        }

        [TearDown]
        public void TearDown()
        {
            ResetManager();
            UnityEngine.Object.DestroyImmediate(settings);
            UnityEngine.Object.DestroyImmediate(rewards);
            Set(typeof(SaveController), "globalSave", previousSave);
            Set(typeof(SaveController), "isSaveLoaded", previousLoaded);
            Set(typeof(Serializer), "persistentDataPath", previousPath);
            Set(typeof(SaveController), "isSaveRequired", previousSaveRequired);
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }

        [Test]
        public void NewProduct_UsesKeyWithoutEnum_StoreIdLookupRemainsSeparate()
        {
            Assert.That(settings.ValidateCatalog(), Is.Empty);
            Assert.That(IAPManager.GetIAPItemByKey("new_pack"), Is.SameAs(product));
            Assert.That(IAPManager.GetIAPItem(product.ID), Is.SameAs(product));
            Assert.That(IAPManager.GetIAPItem("new_pack"), Is.Null);
            Assert.That(IAPManager.GetIAPItem(ProductKeyType.NoAds), Is.Null);
            Assert.That(product.SaveKey, Is.EqualTo("iap_new_pack"));
        }

        [TestCase(ProductKeyType.NoAds, "no_ads")]
        [TestCase(ProductKeyType.StarterPack, "starter_pack")]
        [TestCase(ProductKeyType.GoldSmall, "coins_small")]
        [TestCase(ProductKeyType.GoldMedium, "coins_medium")]
        [TestCase(ProductKeyType.GoldBig, "coins_big")]
        [TestCase(ProductKeyType.PUPack, "powerup_pack")]
        public void LegacyProducts_PreserveEnumSaveKeys(ProductKeyType key, string expected)
        {
            var item = JsonUtility.FromJson<IAPItem>("{\"productKeyType\":" + (int)key + "}");
            Assert.That(item.Key, Is.EqualTo(expected));
            Assert.That(item.SaveKey, Is.EqualTo("iap_" + key));
        }

        [Test]
        public void Catalog_RejectsDuplicateKeysAndStoreIds()
        {
            typeof(IAPSettings).GetField("storeItems", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(settings, new[] { product, product });
            Assert.That(settings.ValidateCatalog(), Has.Some.Contains("Duplicate key"));
            Assert.That(settings.ValidateCatalog(), Has.Some.Contains("Duplicate Android"));
            Assert.That(settings.ValidateCatalog(), Has.Some.Contains("Duplicate iOS"));
        }

        [Test]
        public void Catalog_RejectsBlankKeyAndMissingPlatformId()
        {
            var invalid = JsonUtility.FromJson<IAPItem>("{\"useLegacyKey\":false,\"iOSID\":\"ios.only\"}");
            typeof(IAPSettings).GetField("storeItems", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(settings, new[] { invalid });
            Assert.That(settings.ValidateCatalog("Android"), Has.Some.Contains("key"));
            Assert.That(settings.ValidateCatalog("Android"), Has.Some.Contains("Android ID is missing"));
        }

        [Test]
        public void Catalog_RejectsCustomKeyThatCollidesWithLegacySaveIdentity()
        {
            var legacy = JsonUtility.FromJson<IAPItem>("{\"productKeyType\":0,\"androidID\":\"legacy.id\"}");
            var custom = JsonUtility.FromJson<IAPItem>("{\"key\":\"NoAds\",\"useLegacyKey\":false,\"androidID\":\"custom.id\"}");
            typeof(IAPSettings).GetField("storeItems", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(settings, new[] { legacy, custom });
            Assert.That(settings.ValidateCatalog(), Has.Some.Contains("Duplicate save identity"));
        }

        [Test]
        public void RepeatedTransaction_GrantsOnce_AcrossServiceReload()
        {
            Assert.That(IAPManager.TryFulfillPurchase(product, "order-1"), Is.True);
            Assert.That(IAPManager.TryFulfillPurchase(product, "order-1"), Is.True);
            Assert.That(reward.Count, Is.EqualTo(1));
            Assert.That(product.TimesPurchased, Is.EqualTo(1));
            var restored = Serializer.Deserialize<GlobalSave>("save");
            restored.Init(0);
            Set(typeof(IAPManager), "save", restored.GetSaveObject<IAPManager.Save>("iapGlobalSave"));
            Assert.That(IAPManager.TryFulfillPurchase(product, "order-1"), Is.True);
            Assert.That(reward.Count, Is.EqualTo(1));
        }

        [Test]
        public void UnknownProductOrMissingTransaction_IsNotFulfilled()
        {
            Assert.That(IAPManager.TryFulfillPurchase(null, "order-1"), Is.False);
            Assert.That(IAPManager.TryFulfillPurchase(product, ""), Is.False);
            Assert.That(reward.Count, Is.Zero);
            Assert.That(IAPManager.IsIAPTransactionFulfilled("order-1"), Is.False);
        }

        [Test]
        public void PresentationException_DoesNotInterruptFulfillmentOrOtherObservers()
        {
            int completed = 0;
            rewards.RewardRecieved += () => throw new InvalidOperationException("presentation failed");
            IAPManager.ProductPurchaseCompleted += _ => throw new InvalidOperationException("observer failed");
            IAPManager.ProductPurchaseCompleted += _ => completed++;
            bool ignore = LogAssert.ignoreFailingMessages;
            LogAssert.ignoreFailingMessages = true;
            try { Assert.That(IAPManager.TryFulfillPurchase(product, "order-ui"), Is.True); }
            finally { LogAssert.ignoreFailingMessages = ignore; }
            Assert.That(completed, Is.EqualTo(1));
            Assert.That(IAPManager.TryFulfillPurchase(product, "order-ui"), Is.True);
            Assert.That(reward.Count, Is.EqualTo(1));
        }

        [Test]
        public void RewardFailure_DoesNotMarkFulfilled_AndCanRetry()
        {
            reward.Fail = true;
            Assert.Throws<InvalidOperationException>(() => IAPManager.TryFulfillPurchase(product, "order-error"));
            Assert.That(IAPManager.IsIAPTransactionFulfilled("order-error"), Is.False);
            Assert.That(product.TimesPurchased, Is.Zero);
            reward.Fail = false;
            Assert.That(IAPManager.TryFulfillPurchase(product, "order-error"), Is.True);
            Assert.That(reward.Count, Is.EqualTo(1));
        }

        [Test]
        public void SaveFailure_RetryPersistsWithoutGrantingAgain()
        {
            string invalidPath = Path.Combine(directory, "file");
            File.WriteAllText(invalidPath, "not a directory");
            Set(typeof(Serializer), "persistentDataPath", invalidPath);
            Assert.Throws<IOException>(() => IAPManager.TryFulfillPurchase(product, "order-save"));
            Set(typeof(Serializer), "persistentDataPath", directory);
            Assert.That(IAPManager.TryFulfillPurchase(product, "order-save"), Is.True);
            Assert.That(reward.Count, Is.EqualTo(1));
        }

        [Test]
        public void MultiRewardPurchase_ResumesAtFailedRewardAfterReload()
        {
            var nextReward = new CountingReward { Fail = true };
            rewards.Rewards.Add(nextReward);
            Assert.Throws<InvalidOperationException>(() => IAPManager.TryFulfillPurchase(product, "order-partial"));
            Assert.That(reward.Count, Is.EqualTo(1));
            Assert.That(IAPManager.IsIAPTransactionFulfilled("order-partial"), Is.False);

            var restored = Serializer.Deserialize<GlobalSave>("save");
            restored.Init(0);
            Set(typeof(SaveController), "globalSave", restored);
            Set(typeof(IAPManager), "save", restored.GetSaveObject<IAPManager.Save>("iapGlobalSave"));
            product.Init();
            nextReward.Fail = false;

            Assert.That(IAPManager.TryFulfillPurchase(product, "order-partial"), Is.True);
            Assert.That(reward.Count, Is.EqualTo(1));
            Assert.That(nextReward.Count, Is.EqualTo(1));
            Assert.That(product.TimesPurchased, Is.EqualTo(1));
        }

        [Test]
        public void TwoButtons_SynchronizeAndPreventRepeatedPurchase()
        {
            var first = CreateButton();
            var second = CreateButton();
            try
            {
                Assert.That(first.GetComponent<Button>().interactable, Is.True);
                IAPManager.BuyProduct("new_pack");
                IAPManager.BuyProduct("new_pack");
                Assert.That(store.BuyCount, Is.EqualTo(1));
                Assert.That(first.GetComponent<Button>().interactable, Is.False);
                Assert.That(second.GetComponent<Button>().interactable, Is.False);
                UnityEngine.Object.DestroyImmediate(first);
                first = null;
                IAPManager.TryFulfillPurchase(product, "order-closed");
                IAPManager.FinishPurchase();
                Assert.That(second.GetComponent<Button>().interactable, Is.True);
                second.SetActive(false);
                second.SetActive(true);
                Assert.That(second.GetComponent<Button>().interactable, Is.True);
            }
            finally
            {
                if (first != null) UnityEngine.Object.DestroyImmediate(first);
                UnityEngine.Object.DestroyImmediate(second);
            }
        }

        [Test]
        public void DummyRestore_AlwaysClearsInProgress()
        {
            Set(typeof(IAPManager), "wrapper", new DummyIAPWrapper());
            IAPManager.RestorePurchases();
            Assert.That(IAPManager.IsRestoreInProgress, Is.False);
        }

        [Test]
        public void RestoreException_ClearsBusyStateAndAllowsRetry()
        {
            store.FailRestore = true;
            LogAssert.Expect(LogType.Exception, new System.Text.RegularExpressions.Regex("InvalidOperationException: restore failed"));
            IAPManager.RestorePurchases();
            Assert.That(IAPManager.IsRestoreInProgress, Is.False);
            store.FailRestore = false;
            IAPManager.RestorePurchases();
            Assert.That(IAPManager.IsRestoreInProgress, Is.False);
        }

        [Test]
        public void ButtonOpenedBeforeInitialization_BecomesBuyableWhenStoreIsReady()
        {
            Set(typeof(IAPManager), "<IsInitialized>k__BackingField", false);
            var go = CreateButton();
            try
            {
                Assert.That(go.GetComponent<Button>().interactable, Is.False);
                IAPManager.OnModuleInitialized();
                Assert.That(go.GetComponent<Button>().interactable, Is.True);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        [Test]
        public void ModuleAndMigratedPrefabs_UseSharedStandaloneSettings()
        {
            var registry = AssetDatabase.LoadAssetAtPath<ProjectInitSettings>("Assets/Project Files/Data/Project Init Settings.asset");
            var catalog = AssetDatabase.LoadAssetAtPath<IAPSettings>("Assets/Project Files/Data/IAP Settings.asset");
            var monetization = AssetDatabase.LoadAssetAtPath<MonetizationSettings>("Assets/Project Files/Data/Monetization Settings.asset");
            Assert.That(registry.GetModule<IAPInitModule>().Settings, Is.SameAs(catalog));
            Assert.That(monetization.IAPSettings, Is.SameAs(catalog));
            Assert.That(catalog.ValidateCatalog(), Is.Empty);
            Assert.That(catalog.StoreItems, Has.Length.EqualTo(6));
            int holders = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Project Files/Game/Prefabs" }))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                foreach (var holder in prefab.GetComponentsInChildren<IAPRewardsHolder>(true))
                {
                    var item = catalog.FindByKey(holder.ProductKeyId);
                    Assert.That(item, Is.Not.Null, prefab.name);
                    Assert.That(new SerializedObject(holder).FindProperty("rewardSet").objectReferenceValue, Is.SameAs(item.RewardsSet));
                    holders++;
                }
            }
            Assert.That(holders, Is.GreaterThanOrEqualTo(7));
        }

        [Test]
        public void CatalogEditor_AddDuplicateReorderAndReload_PreserveExistingProducts()
        {
            string path = "Assets/__IAPCatalogTest_" + Guid.NewGuid().ToString("N") + ".asset";
            var original = AssetDatabase.LoadAssetAtPath<IAPSettings>("Assets/Project Files/Data/IAP Settings.asset");
            var draft = UnityEngine.Object.Instantiate(original);
            Editor editor = null;
            try
            {
                AssetDatabase.CreateAsset(draft, path);
                editor = Editor.CreateEditor(draft);
                var type = editor.GetType();
                type.GetMethod("AddProduct", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(editor, new object[] { false });
                Assert.That(draft.StoreItems, Has.Length.EqualTo(7));
                var serialized = new SerializedObject(draft);
                var seventh = serialized.FindProperty("storeItems").GetArrayElementAtIndex(6);
                seventh.FindPropertyRelative("key").stringValue = "coins_2500";
                seventh.FindPropertyRelative("androidID").stringValue = "com.test.coins2500";
                seventh.FindPropertyRelative("iOSID").stringValue = "com.test.coins2500.ios";
                serialized.ApplyModifiedProperties();
                Assert.That(draft.FindByKey("coins_2500").HasLegacyKey, Is.False);
                var list = (UnityEditorInternal.ReorderableList)type.GetField("products", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(editor);
                list.index = 0;
                type.GetMethod("AddProduct", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(editor, new object[] { true });
                Assert.That(draft.StoreItems[0].Key, Is.EqualTo("no_ads"));
                Assert.That(draft.StoreItems[1].Key, Is.Empty);
                Assert.That(draft.StoreItems[1].AndroidID, Is.Empty);
                Assert.That(draft.StoreItems[1].HasLegacyKey, Is.False);
                Assert.That(draft.StoreItems[1].RewardsSet, Is.SameAs(draft.StoreItems[0].RewardsSet));
                serialized.Update();
                serialized.FindProperty("storeItems").DeleteArrayElementAtIndex(1);
                serialized.FindProperty("storeItems").MoveArrayElement(6, 0);
                serialized.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                var loaded = AssetDatabase.LoadAssetAtPath<IAPSettings>(path);
                Assert.That(loaded.ValidateCatalog(), Is.Empty);
                Assert.That(loaded.StoreItems[0].Key, Is.EqualTo("coins_2500"));
                foreach (var item in original.StoreItems)
                    Assert.That(loaded.FindByKey(item.Key).SaveKey, Is.EqualTo(item.SaveKey));
            }
            finally
            {
                if (editor != null) UnityEngine.Object.DestroyImmediate(editor);
                AssetDatabase.DeleteAsset(path);
            }
        }

        [Test]
        public void DisabledModuleAndUnreadyStore_RejectPurchases()
        {
            var serialized = new SerializedObject(settings);
            serialized.FindProperty("isEnabled").boolValue = false;
            serialized.ApplyModifiedProperties();
            IAPManager.BuyProduct("new_pack");
            Assert.That(store.BuyCount, Is.Zero);
            Assert.That(IAPManager.GetProductData("new_pack"), Is.Null);
            serialized.FindProperty("isEnabled").boolValue = true;
            serialized.ApplyModifiedProperties();
            Set(typeof(IAPManager), "<IsInitialized>k__BackingField", false);
            IAPManager.BuyProduct("new_pack");
            Assert.That(store.BuyCount, Is.Zero);
        }

        [Test]
        public void LegacyAndStringApis_ResolveSameProductAndEmitBothEvents()
        {
            typeof(IAPItem).GetField("useLegacyKey", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(product, true);
            int legacy = 0, keyed = 0;
            IAPManager.PurchaseCompleted += _ => legacy++;
            IAPManager.ProductPurchaseCompleted += _ => keyed++;
            Assert.That(IAPManager.GetIAPItem(ProductKeyType.NoAds), Is.SameAs(IAPManager.GetIAPItemByKey("new_pack")));
            IAPManager.BuyProduct(ProductKeyType.NoAds);
            Assert.That(store.BuyCount, Is.EqualTo(1));
            IAPManager.TryFulfillPurchase(product, "order-legacy");
            IAPManager.FinishPurchase();
            Assert.That(legacy, Is.EqualTo(1));
            Assert.That(keyed, Is.EqualTo(1));
        }

        [Test]
        public void NonConsumable_IsOwnedAfterFulfillment_AndCannotBeBoughtAgain()
        {
            product.ProductType = ProductType.NonConsumable;
            IAPManager.TryFulfillPurchase(product, "unlock-order");
            Assert.That(IAPManager.IsPurchased("new_pack"), Is.True);
            IAPManager.BuyProduct("new_pack");
            Assert.That(store.BuyCount, Is.Zero);
        }

        [UnityTest]
        public System.Collections.IEnumerator Initialization_IsGuardedWhileAsyncStoreStarts()
        {
            ResetManager();
            IAPManager.ConfigureModule(settings);
            IAPManager.Init(settings);
            var first = typeof(IAPManager).GetField("wrapper", Flags).GetValue(null);
            IAPManager.Init(settings);
            Assert.That(typeof(IAPManager).GetField("wrapper", Flags).GetValue(null), Is.SameAs(first));
            double deadline = EditorApplication.timeSinceStartup + 5;
            while (IAPManager.IsInitializing && EditorApplication.timeSinceStartup < deadline) yield return null;
            Assert.That(IAPManager.IsInitializing, Is.False);
            Assert.That(IAPManager.IsInitialized, Is.True);
        }

        private GameObject CreateButton()
        {
            var go = new GameObject("IAP test button", typeof(RectTransform), typeof(Button));
            var view = go.AddComponent<IAPButton>();
            // EditMode does not run MonoBehaviour lifecycle automatically.
            typeof(IAPButton).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(view, null);
            typeof(IAPButton).GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(view, null);
            view.Init("new_pack");
            return go;
        }
        private static void Set(Type type, string field, object value) => type.GetField(field, Flags).SetValue(null, value);
        private static void ResetManager() => typeof(IAPManager).GetMethod("UnloadStatic", Flags).Invoke(null, null);

        [Serializable]
        private sealed class CountingReward : Reward
        {
            public int Count;
            public bool Fail;
            public override void ApplyReward()
            {
                if (Fail) throw new InvalidOperationException("reward failed");
                Count++;
            }
        }
        private sealed class TestStore : IAPWrapper
        {
            public int BuyCount;
            public bool FailRestore;
            public override Task Init(IAPSettings settings) => Task.CompletedTask;
            public override void BuyProduct(string key) => BuyCount++;
            public override ProductData GetProductData(string key) => new ProductData(ProductType.Consumable);
            public override bool IsSubscribed(string key) => false;
            public override bool IsPurchased(string id) => false;
            public override void RestorePurchases()
            {
                if (FailRestore) throw new InvalidOperationException("restore failed");
                IAPManager.FinishRestorePurchases();
            }
        }
    }
}
