using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NebulaSoft.Tests
{
    public sealed class CoreModuleUpgradeTests
    {
        private readonly Dictionary<FieldInfo, object> state = new Dictionary<FieldInfo, object>();
        private readonly List<Object> owned = new List<Object>();
        private float timeScale;

        [SetUp]
        public void SetUp()
        {
            timeScale = Time.timeScale;
            foreach (var type in new[] { typeof(SaveController), typeof(CurrencyController), typeof(PoolManager), typeof(UIController), typeof(AudioController), typeof(SkinController) })
                foreach (var field in type.GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public))
                    if (!field.IsInitOnly && !field.IsLiteral) state.Add(field, field.GetValue(null));
            var save = new GlobalSave(); save.Init(0);
            Set(typeof(SaveController), "globalSave", save);
            Set(typeof(SaveController), "isSaveLoaded", true);
            Set(typeof(SaveController), "isSaveRequired", false);
            CurrencyController.Shutdown();
            Set(typeof(PoolManager), "poolsList", new List<IPool>());
            Set(typeof(PoolManager), "poolsDictionary", new Dictionary<string, IPool>(StringComparer.Ordinal));
            Set(typeof(PoolManager), "<DefaultContainer>k__BackingField", null);
            Set(typeof(UIController), "uiController", null);
            Set(typeof(AudioController), "audioSourcesPool", null);
            Set(typeof(AudioController), "audioListener", null);
            Set(typeof(AudioController), "save", null);
        }

        [TearDown]
        public void TearDown()
        {
            AudioController.Shutdown();
            for (int i = owned.Count - 1; i >= 0; i--) if (owned[i] != null) Object.DestroyImmediate(owned[i]);
            owned.Clear();
            PoolManager.Shutdown();
            foreach (var pair in state) pair.Key.SetValue(null, pair.Value);
            state.Clear();
            Time.timeScale = timeScale;
        }

        [Test]
        public void DataConfiguredCurrency_PreservesLegacyBalance_AndSpendsSafely()
        {
            var save = (GlobalSave)Get(typeof(SaveController), "globalSave");
            save.GetSaveObject<Currency.Save>("currency:0").Amount = 73;
            InitCurrencies();
            Assert.That(CurrencyController.Get("Coins"), Is.EqualTo(73));
            Assert.That(CurrencyController.Get("Gem"), Is.EqualTo(10));
            Assert.That(CurrencyController.TrySpend("Gem", 6), Is.True);
            Assert.That(CurrencyController.Get("Gem"), Is.EqualTo(4));
            Assert.That(CurrencyController.TrySpend("Gem", 5), Is.False);
            Assert.That(CurrencyController.TrySpend("Gem", -1), Is.False);
            Assert.That(CurrencyController.TrySpend("unknown", 1), Is.False);
            Assert.That(CurrencyController.Get("Gem"), Is.EqualTo(4));
        }

        [Test]
        public void CurrencyBatch_ValidatesUnknownIdsAndOverflowBeforeChangingAnyBalance()
        {
            InitCurrencies();
            var bad = new CurrencyReward(new[] { new CurrencyAmount("Coins", 5), new CurrencyAmount("missing", 1) });
            Assert.Throws<KeyNotFoundException>(() => bad.ApplyReward());
            Assert.That(CurrencyController.Get("Coins"), Is.EqualTo(10));
            var overflow = new CurrencyReward(new[] { new CurrencyAmount("Coins", 5), new CurrencyAmount("Gem", int.MaxValue) });
            Assert.Throws<OverflowException>(() => overflow.ApplyReward());
            Assert.That(CurrencyController.Get("Coins"), Is.EqualTo(10));
            Assert.That(CurrencyController.Get("Gem"), Is.EqualTo(10));
        }

        [Test]
        public void CurrencyBatch_AggregatesDuplicateIds_AndNotifiesAfterTheWholeBatch()
        {
            InitCurrencies();
            int callbacks = 0;
            CurrencyController.GetCurrency("Coins").OnCurrencyChanged += (_, delta) =>
            {
                Assert.That(CurrencyController.Get("Gem"), Is.EqualTo(13));
                Assert.That(delta, Is.EqualTo(7));
                callbacks++;
            };
            new CurrencyReward(new[] { new CurrencyAmount("Coins", 2), new CurrencyAmount("Gem", 3), new CurrencyAmount("Coins", 5) }).ApplyReward();
            Assert.That(callbacks, Is.EqualTo(1));
            Assert.That(CurrencyController.Get("Coins"), Is.EqualTo(17));
        }

        [Test]
        public void UiCloseCallbacks_BelongToTheirOwnPage_AndRepeatedCloseDoesNotRestartAnimation()
        {
            CreateUi();
            var a = CreatePage(); var b = CreatePage();
            int first = 0, second = 0;
            UIController.HidePage(a, () => first++);
            UIController.HidePage(a, () => first++);
            UIController.HidePage(b, () => second++);
            Assert.That(a.HideCount, Is.EqualTo(1));
            UIController.OnPageClosed(b);
            Assert.That(first, Is.Zero); Assert.That(second, Is.EqualTo(1));
            UIController.OnPageClosed(a);
            UIController.OnPageClosed(a);
            Assert.That(first, Is.EqualTo(2));
        }

        [Test]
        public void UiCloseCallback_CanStartAnotherCloseWithoutLosingItsCallback()
        {
            CreateUi(); var page = CreatePage(); int completed = 0;
            UIController.HidePage(page, () =>
            {
                UIController.ShowPage(page);
                UIController.HidePage(page, () => completed++);
            });
            UIController.OnPageClosed(page);
            Assert.That(completed, Is.Zero);
            UIController.OnPageClosed(page);
            Assert.That(completed, Is.EqualTo(1));
        }

        [Test]
        public void UiOwnerDestroyed_CancelsPendingCloseCallbacks()
        {
            var ui = CreateUi(); var page = CreatePage(); int completed = 0;
            UIController.HidePage(page, () => completed++);
            Object.DestroyImmediate(ui.gameObject);
            UIController.OnPageClosed(page);
            Assert.That(completed, Is.Zero);
        }

        [Test]
        public void SkinSelectionAndUnlock_MarkSaveDirty_AndKeepStableIds()
        {
            var save = new SkinControllerSave();
            save.Update(new[] { 123 });
            Assert.That(save.HasStableSelections, Is.False);
            Set(typeof(SaveController), "isSaveRequired", false);
            save.UpdateIds(new[] { "skin_blue" });
            Assert.That((bool)Get(typeof(SaveController), "isSaveRequired"), Is.True);
            Assert.That(save.GetSelectedSkinId(0), Is.EqualTo("skin_blue"));
            Set(typeof(SaveController), "isSaveRequired", false);
            var skin = new UpgradeTestSkin();
            SetField(skin, typeof(AbstractSkinData), "id", "skin_test");
            skin.Init(null); skin.Unlock();
            Assert.That(skin.IsUnlocked, Is.True);
            Assert.That((bool)Get(typeof(SaveController), "isSaveRequired"), Is.True);
        }

        [Test]
        public void CappedPool_ReplacesDestroyedObjects_AndPrewarmRespectsTheCap()
        {
            var prefab = Own(new GameObject("test prefab"));
            var pool = new Pool(prefab, "limited");
            SetField(pool, typeof(Pool), "capSize", true);
            SetField(pool, typeof(Pool), "maxSize", 1);
            pool.CreatePoolObjects(10);
            var first = pool.GetPooledObject();
            Assert.That(first, Is.Not.Null);
            Assert.That(pool.GetPooledObject(), Is.Null);
            Object.DestroyImmediate(first);
            var replacement = pool.GetPooledObject();
            Assert.That(replacement, Is.Not.Null);
            Assert.That(replacement.transform.parent, Is.EqualTo(PoolManager.DefaultContainer));
            pool.ReturnToPoolEverything();
        }

        [Test]
        public void SkinController_MigratesLegacyHash_ThenUsesTheStableId()
        {
            var blue = new UpgradeTestSkin(); var red = new UpgradeTestSkin();
            SetField(blue, typeof(AbstractSkinData), "id", "blue");
            SetField(red, typeof(AbstractSkinData), "id", "red");
            var database = Own(ScriptableObject.CreateInstance<UpgradeSkinDatabase>());
            SetField(database, typeof(GenericSkinDatabase<UpgradeTestSkin>), "skins", new[] { blue, red });
            var handler = new SkinsHandler();
            SetField(handler, typeof(SkinsHandler), "skinProviders", new AbstractSkinDatabase[] { database });
            var model = SaveController.GetSaveObject<SkinControllerSave>("Skin Controller Save");
            model.Update(new[] { "red".GetHashCode() });
            var controller = Own(new GameObject("test skins")).AddComponent<SkinController>();
            SetField(controller, typeof(SkinController), "handler", handler);
            controller.Init();
            Assert.That(model.HasStableSelections, Is.True);
            Assert.That(model.GetSelectedSkinId(0), Is.EqualTo("red"));
            Assert.That(controller.GetSelectedSkin<UpgradeSkinDatabase>().ID, Is.EqualTo("red"));
            SetField(model, typeof(SkinControllerSave), "selectedSkins", new[] { int.MinValue });
            controller.Init();
            Assert.That(controller.GetSelectedSkin<UpgradeSkinDatabase>().ID, Is.EqualTo("red"));
        }

        [Test]
        public void Pool_DefaultAndExplicitContainersHaveDistinctOwnership()
        {
            var parent = Own(new GameObject("explicit parent"));
            Assert.That(PoolManager.GetContainer(parent.transform), Is.EqualTo(parent.transform));
            Assert.That(PoolManager.GetContainer(null), Is.Not.Null);
            PoolManager.Shutdown();
            Assert.That(parent != null, Is.True);
        }

        [Test]
        public void Audio_ThrottlesTheSameClipAcrossBothOverloads()
        {
            var clips = Own(ScriptableObject.CreateInstance<AudioClips>());
            AudioController.Init(clips, 1, 2);
            var clip = Own(AudioClip.Create("long clip", 441000, 1, 44100, false));
            AudioController.PlaySound(clip, minDelay: 100);
            var times = (Dictionary<AudioClip, float>)Get(typeof(AudioController), "lastClipPlayTimes");
            times[clip] = Time.unscaledTime - 1f;
            float previous = times[clip];
            AudioController.PlaySound(clip, Vector3.zero, minDelay: 100);
            Assert.That(times[clip], Is.EqualTo(previous));
            Assert.That(((ICollection)Get(typeof(AudioController), "audioSourcesPool")).Count, Is.EqualTo(1));
            AudioController.Shutdown();
            Assert.That(Get(typeof(AudioController), "audioListener"), Is.Null);
        }

        [Test]
        public void ModulePreparation_RetryOnlyRunsTheFailedStep_AndInitializesOnce()
        {
            var settings = Own(ScriptableObject.CreateInstance<ProjectInitSettings>());
            var first = Own(ScriptableObject.CreateInstance<UpgradeTestModule>());
            var second = Own(ScriptableObject.CreateInstance<UpgradeFailingModule>());
            second.Fail = true;
            SetField(settings, typeof(ProjectInitSettings), "modules", new InitModule[] { first, second });
            Drain((IEnumerator)typeof(ProjectInitSettings).GetMethod("Prepare", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(settings, null));
            Assert.That(settings.FailedModule, Is.EqualTo("failing"));
            second.Fail = false;
            Drain((IEnumerator)typeof(ProjectInitSettings).GetMethod("Prepare", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(settings, null));
            settings.Init(null); settings.Init(null);
            Assert.That(first.Prepared, Is.EqualTo(1));
            Assert.That(second.Prepared, Is.EqualTo(2));
            Assert.That(first.Created, Is.EqualTo(1));
            Assert.That(settings.LastError, Is.Null);
            settings.Shutdown();
        }

        [Test]
        public void AudioShutdown_PreservesSettingsDuringTheLaterSaveFlush()
        {
            var clips = Own(ScriptableObject.CreateInstance<AudioClips>());
            AudioController.Init(clips, 1);
            AudioController.SetVolume(AudioType.Sound, 0.25f);
            var settings = SaveController.GetSaveObject<AudioSave>("audio");
            AudioController.Shutdown();
            settings.Flush();
            Assert.That(Array.Find(settings.VolumeDatas, value => value.AudioType == AudioType.Sound).Volume, Is.EqualTo(0.25f));
        }

        [Test]
        public void ModuleValidation_ReportsMissingDependenciesAndDuplicates()
        {
            var settings = Own(ScriptableObject.CreateInstance<ProjectInitSettings>());
            var module = Own(ScriptableObject.CreateInstance<CurrencyInitModule>());
            SetField(settings, typeof(ProjectInitSettings), "modules", new InitModule[] { module, module });
            Assert.That(settings.ValidateModules(), Has.Some.Contains("Duplicate"));
            Assert.That(settings.ValidateModules(), Has.Some.Contains("SaveInitModule"));
        }

        [Test]
        public void LoadingTask_TimesOutAndIgnoresLateCompletion()
        {
            var task = new NeverFinishingTask(); task.Activate();
            task.CheckTimeout(Time.realtimeSinceStartupAsDouble + 31);
            task.CompleteTask(LoadingTask.CompleteStatus.Completed);
            Assert.That(task.Status, Is.EqualTo(LoadingTask.CompleteStatus.TimedOut));
        }

        [Test]
        public void LowLevelRuntimeAssemblies_HaveNoGameplayOrEditorReferences()
        {
            foreach (var type in new[] { typeof(SaveController), typeof(PoolManager) })
            {
                Assert.That(type.Assembly, Is.Not.EqualTo(typeof(GameController).Assembly));
                foreach (var reference in type.Assembly.GetReferencedAssemblies())
                {
                    Assert.That(reference.Name, Is.Not.EqualTo("Assembly-CSharp"));
                    Assert.That(reference.Name, Does.Not.StartWith("UnityEditor"));
                }
            }
        }

        private void InitCurrencies()
        {
            var coins = new Currency(); var gems = new Currency();
            SetField(coins, typeof(Currency), "defaultAmount", 10);
            SetField(gems, typeof(Currency), "defaultAmount", 10);
            SetField(gems, typeof(Currency), "id", "Gem");
            var database = Own(ScriptableObject.CreateInstance<CurrencyDatabase>());
            SetField(database, typeof(CurrencyDatabase), "currencies", new[] { coins, gems });
            CurrencyController.Init(database);
        }
        private UIController CreateUi()
        {
            var go = Own(new GameObject("test UI")); var controller = go.AddComponent<UIController>();
            Set(typeof(UIController), "uiController", controller);
            SetField(controller, typeof(UIController), "popupWindows", new List<IPopupWindow>());
            return controller;
        }
        private UpgradeTestPage CreatePage()
        {
            var page = Own(new GameObject("test page")).AddComponent<UpgradeTestPage>();
            page.CacheComponents(); page.EnableCanvas(); return page;
        }
        private T Own<T>(T value) where T : Object { owned.Add(value); return value; }
        private static object Get(Type type, string name) => type.GetField(name, BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
        private static void Set(Type type, string name, object value) => type.GetField(name, BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, value);
        private static void SetField(object target, Type type, string name, object value) => type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        private static void Drain(IEnumerator routine) { while (routine.MoveNext()) if (routine.Current is IEnumerator child) Drain(child); }
        private sealed class NeverFinishingTask : LoadingTask { public override void OnTaskActivated() { } }
    }
    public sealed class UpgradeTestPage : UIPage
    {
        public int HideCount;
        public override void Init() { }
        public override void PlayShowAnimation() { }
        public override void PlayHideAnimation() { HideCount++; }
    }
    [Serializable] public sealed class UpgradeTestSkin : AbstractSkinData { }
    public sealed class UpgradeSkinDatabase : GenericSkinDatabase<UpgradeTestSkin> { }
    public class UpgradeTestModule : InitModule
    {
        public int Prepared, Created;
        public override string ModuleName => "working";
        public override IEnumerator Prepare() { Prepared++; yield return null; }
        public override void CreateComponent() { Created++; }
    }
    public sealed class UpgradeFailingModule : UpgradeTestModule
    {
        public bool Fail;
        public override string ModuleName => "failing";
        public override IEnumerator Prepare() { Prepared++; yield return null; if (Fail) throw new InvalidOperationException("retry me"); }
    }
}
