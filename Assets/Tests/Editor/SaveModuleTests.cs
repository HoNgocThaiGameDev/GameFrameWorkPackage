using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace NebulaSoft.Tests
{
    public sealed class SaveModuleTests
    {
        private const BindingFlags Flags = BindingFlags.Static | BindingFlags.NonPublic;
        private const string LegacyFixture = "Assets/Tests/Editor/SaveFixtures/BeforeRefactor.bytes";
        private readonly Dictionary<string, object> previousState = new Dictionary<string, object>();
        private string directory;
        private object previousPath;
        private TestWrapper wrapper;
        private GlobalSave liveSave;
        private SimpleIntSave value;

        [SetUp]
        public void SetUp()
        {
            CompleteWrites();
            foreach (string name in new[] { "globalSave", "isSaveLoaded", "isSaveRequired", "saveRevision",
                "isInitializing", "callbackReciever", "writeCoordinator", "saveWrapper", "OnSaveLoaded" })
                previousState[name] = Get(name);
            previousPath = typeof(Serializer).GetField("persistentDataPath", Flags).GetValue(null);
            directory = Path.Combine(Path.GetTempPath(), "NebulaSaveTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            SetPath(directory);
            Set("writeCoordinator", Activator.CreateInstance(Get("writeCoordinator").GetType(), true));
            wrapper = new TestWrapper();
            Set("saveWrapper", wrapper);
            Set("callbackReciever", null);
            Set("isInitializing", false);
            Set("OnSaveLoaded", null);
            Set("isSaveRequired", false);
            Set("saveRevision", 0L);
            liveSave = new GlobalSave();
            liveSave.Init(0);
            value = liveSave.GetSaveObject<SimpleIntSave>(314159);
            Set("globalSave", liveSave);
            Set("isSaveLoaded", true);
        }

        [TearDown]
        public void TearDown()
        {
            wrapper.Release.Set();
            CompleteWrites();
            typeof(SaveController).GetMethod("UnloadStatic", Flags).Invoke(null, null);
            foreach (var entry in previousState) Set(entry.Key, entry.Value);
            typeof(Serializer).GetField("persistentDataPath", Flags).SetValue(null, previousPath);
            wrapper.Dispose();
            Directory.Delete(directory, true);
        }

        [Test]
        public void LegacyBinaryFile_LoadsAndRoundTripsWithOriginalReader()
        {
            File.Copy(LegacyFixture, Path.Combine(directory, "save"));
            GlobalSave restored = Serializer.Deserialize<GlobalSave>("save");
            restored.Init(100);
            Assert.That(restored.GetSaveObject<SimpleIntSave>(314159).Value, Is.EqualTo(73));
            Assert.That(restored.GetSaveObject<SimpleIntSave>("currency:0").Value, Is.EqualTo(1200));
            Assert.That(restored.GameTime, Is.EqualTo(15));
            DateTime lastExit = restored.LastExitTime;
            restored.GetSaveObject<SimpleIntSave>(314159).Value = 74;
            Set("globalSave", restored);
            Set("saveWrapper", new DefaultSaveWrapper());
            SaveController.SaveChecked();

            // Same BinaryFormatter reader used before this refactor; no new envelope or key mapping.
            GlobalSave reread;
            using (var stream = File.OpenRead(Path.Combine(directory, "save")))
                reread = (GlobalSave)new BinaryFormatter().Deserialize(stream);
            reread.Init(200);
            Assert.That(reread.GetSaveObject<SimpleIntSave>(314159).Value, Is.EqualTo(74));
            Assert.That(reread.GetSaveObject<SimpleIntSave>("currency:0").Value, Is.EqualTo(1200));
            Assert.That(reread.GameTime, Is.EqualTo(15));
            Assert.That(reread.LastExitTime, Is.GreaterThanOrEqualTo(lastExit));
        }

        [Test]
        public void LegacyNamedSave_MigratesToStableIdentityAndReloadsWithoutTheOldHash()
        {
            File.Copy(LegacyFixture, Path.Combine(directory, "save"));
            var restored = Serializer.Deserialize<GlobalSave>("save");
            restored.Init(0);
            Assert.That(restored.GetSaveObject<SimpleIntSave>("currency:0").Value, Is.EqualTo(1200));
            Set("globalSave", restored);
            Set("saveWrapper", new DefaultSaveWrapper());
            SaveController.SaveChecked();

            var migrated = Serializer.Deserialize<GlobalSave>("save");
            migrated.Init(0);
            Assert.That(migrated.SchemaVersion, Is.EqualTo(2));
            Assert.That(migrated.GetSaveObject<SimpleIntSave>("currency:0", int.MinValue).Value, Is.EqualTo(1200));
        }

        [Test]
        public void BackgroundWrites_KeepIndependentSnapshotsAndRunInOrder()
        {
            BeginBlockedWrite(10);
            value.Value = 20;
            SaveController.MarkAsSaveIsRequired();
            SaveController.Save();
            value.Value = 30;
            SaveController.MarkAsSaveIsRequired();
            SaveController.Save();
            wrapper.Release.Set();
            CompleteWrites();

            Assert.That(ReadValues(), Is.EqualTo(new[] { 10, 20, 30 }));
            Assert.That(wrapper.MaxConcurrent, Is.EqualTo(1));
            Assert.That(Dirty, Is.False);
        }

        [Test]
        public void MutationDuringWrite_RemainsDirtyUntilItsOwnSuccessfulSave()
        {
            BeginBlockedWrite(10);
            Assert.That(Dirty, Is.True, "Scheduling a write is not successful persistence.");
            value.Value = 20;
            SaveController.MarkAsSaveIsRequired();
            wrapper.Release.Set();
            CompleteWrites();
            Assert.That(Dirty, Is.True);
            Assert.That(ReadValues(), Is.EqualTo(new[] { 10 }));

            SaveController.Save(useThreads: false);
            Assert.That(ReadValues(), Is.EqualTo(new[] { 10, 20 }));
            Assert.That(Dirty, Is.False);
        }

        [Test]
        public void CheckedSave_WaitsForOlderWriteAndPersistsLatestValueOnCallerThread()
        {
            int caller = Thread.CurrentThread.ManagedThreadId;
            BeginBlockedWrite(10);
            value.Value = 20;
            SaveController.MarkAsSaveIsRequired();
            Task release = ReleaseFirstWriteSoon();
            SaveController.SaveChecked();
            release.GetAwaiter().GetResult();
            Assert.That(ReadValues(), Is.EqualTo(new[] { 10, 20 }));
            Assert.That(wrapper.Threads[1], Is.EqualTo(caller));
            Assert.That(wrapper.MaxConcurrent, Is.EqualTo(1));
            Assert.That(Dirty, Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void FailedOrdinarySave_RemainsDirtyAndRetries(bool background)
        {
            wrapper.FailAttempt = 1;
            SaveController.MarkAsSaveIsRequired();
            LogAssert.Expect(LogType.Error, new Regex("\\[Save Controller\\]: Failed to save: simulated write failure"));
            SaveController.Save(useThreads: background);
            CompleteWrites();
            Assert.That(Dirty, Is.True);
            SaveController.Save(useThreads: false);
            Assert.That(wrapper.Attempts, Is.EqualTo(2));
            Assert.That(Dirty, Is.False);
        }

        [Test]
        public void FailedQueuedWrite_DoesNotPreventNextWrite()
        {
            wrapper.FailAttempt = 1;
            LogAssert.Expect(LogType.Error, new Regex("simulated write failure"));
            BeginBlockedWrite(10);
            value.Value = 20;
            SaveController.MarkAsSaveIsRequired();
            SaveController.Save();
            wrapper.Release.Set();
            CompleteWrites();
            Assert.That(ReadValues(), Is.EqualTo(new[] { 20 }));
            Assert.That(Dirty, Is.False);
        }

        [Test]
        public void CheckedFailure_ReachesCallerAndKeepsDirtyEvenForForcedCleanSave()
        {
            wrapper.FailAttempt = 1;
            Assert.Throws<IOException>(() => SaveController.SaveChecked());
            Assert.That(Dirty, Is.True);
            SaveController.SaveChecked();
            Assert.That(Dirty, Is.False);
        }

        [Test]
        public void SnapshotFailure_ReachesCheckedCallerAndCanRetry()
        {
            FlushProbe probe = liveSave.GetSaveObject<FlushProbe>(99);
            probe.Throw = true;
            Assert.Throws<InvalidOperationException>(() => SaveController.SaveChecked());
            Assert.That(Dirty, Is.True);
            Assert.That(wrapper.Attempts, Is.Zero);
            probe.Throw = false;
            SaveController.SaveChecked();
            Assert.That(Dirty, Is.False);
        }

        [Test]
        public void SnapshotFlush_RunsOnCallerThreadBeforeBackgroundIO()
        {
            FlushProbe probe = liveSave.GetSaveObject<FlushProbe>(99);
            int caller = Thread.CurrentThread.ManagedThreadId;
            SaveController.Save(true);
            CompleteWrites();
            Assert.That(probe.Thread, Is.EqualTo(caller));
            Assert.That(wrapper.Threads[0], Is.Not.EqualTo(caller));
        }

        [Test]
        public void WrapperWithoutThreadSupport_WritesOnCallerThread()
        {
            wrapper.Background = false;
            SaveController.Save(true);
            Assert.That(wrapper.Threads[0], Is.EqualTo(Thread.CurrentThread.ManagedThreadId));
            Assert.That(Dirty, Is.False);
        }

        [TestCase("custom")]
        [TestCase("preset")]
        [TestCase("delete")]
        [TestCase("read")]
        public void OtherFileOperations_WaitForPendingWrites(string operation)
        {
            BeginBlockedWrite(10);
            Task release = ReleaseFirstWriteSoon();
            switch (operation)
            {
                case "custom":
                    var custom = new GlobalSave();
                    custom.Init(0);
                    custom.GetSaveObject<SimpleIntSave>(314159).Value = 90;
                    SaveController.SaveCustom(custom);
                    break;
                case "preset": SaveController.PresetsSave("SavePresets/test"); break;
                case "delete": SaveController.DeleteSaveFile(); break;
                case "read": SaveController.GetGlobalSave(); break;
            }
            release.GetAwaiter().GetResult();
            Assert.That(wrapper.Operations[0], Is.EqualTo("save:save"));
            Assert.That(wrapper.Operations.Count, Is.EqualTo(2));
            Assert.That(wrapper.MaxConcurrent, Is.EqualTo(1));
            Assert.That(wrapper.Operations[1], Is.EqualTo(operation == "delete" ? "delete:save" :
                operation == "read" ? "load:save" : operation == "preset" ? "save:SavePresets/test" : "save:save"));
        }

        [Test]
        public void FailedAtomicSerialization_PreservesExistingFileAndRemovesTemporaryFile()
        {
            Serializer.SerializeChecked(42, "save");
            byte[] previous = File.ReadAllBytes(Path.Combine(directory, "save"));
            Assert.Throws<SerializationException>(() => Serializer.SerializeChecked(new NonSerializable(), "save"));
            Assert.That(File.ReadAllBytes(Path.Combine(directory, "save")), Is.EqualTo(previous));
            Assert.That(File.Exists(Path.Combine(directory, "save.pending")), Is.False);
            Serializer.SerializeChecked(43, "save");
            Assert.That(Serializer.Deserialize<int>("save"), Is.EqualTo(43));
        }

        [Test]
        public void DefaultWrapper_ReportsIOFailureAndCanRetryWithoutLosingDirty()
        {
            Set("saveWrapper", new DefaultSaveWrapper());
            string blocked = Path.Combine(directory, "not-a-directory");
            File.WriteAllText(blocked, "keep");
            SetPath(blocked);
            Assert.Catch<IOException>(() => SaveController.SaveChecked());
            Assert.That(Dirty, Is.True);
            SetPath(directory);
            SaveController.SaveChecked();
            Assert.That(Dirty, Is.False);
            Assert.That(File.Exists(Path.Combine(directory, "save")), Is.True);
        }

        [Test]
        public void RepeatedInit_KeepsOneReceiverAndDoesNotClearExistingData()
        {
            Set("globalSave", null);
            Set("isSaveLoaded", false);
            int loaded = 0;
            SimpleCallback listener = () => { loaded++; SaveController.Init(1); };
            SaveController.OnSaveLoaded += listener;
            SaveController.Init(1);
            object receiver = Get("callbackReciever");
            SaveController.GetSaveObject<SimpleIntSave>(314159).Value = 80;
            SaveController.Init(1, clearSave: true);
            Assert.That(Get("callbackReciever"), Is.SameAs(receiver));
            Assert.That(SaveController.GetSaveObject<SimpleIntSave>(314159).Value, Is.EqualTo(80));
            Assert.That(wrapper.Loads, Is.EqualTo(1));
            Assert.That(loaded, Is.EqualTo(1));

            typeof(SaveController).GetMethod("UnloadStatic", Flags).Invoke(null, null);
            Assert.That((UnityEngine.Object)receiver == null, Is.True);
            SaveController.Init(1);
            Assert.That(Get("callbackReciever"), Is.Not.SameAs(receiver));
            Assert.That(wrapper.Loads, Is.EqualTo(2));
        }

        [Test]
        public void PauseCallback_FlushesPendingAndCurrentDataBeforeReturning()
        {
            Set("globalSave", null);
            Set("isSaveLoaded", false);
            SaveController.Init(0, clearSave: true);
            value = SaveController.GetSaveObject<SimpleIntSave>(314159);
            BeginBlockedWrite(10);
            value.Value = 20;
            SaveController.MarkAsSaveIsRequired();
            Task release = ReleaseFirstWriteSoon();
            object receiver = Get("callbackReciever");
            receiver.GetType().GetMethod("OnApplicationPause", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(receiver, new object[] { true });
            release.GetAwaiter().GetResult();
            Assert.That(ReadValues(), Is.EqualTo(new[] { 10, 20 }));
            Assert.That(Dirty, Is.False);
        }

        [Test]
        public void CleanSave_IsSkippedUnlessForced_AndCheckedSaveRequiresInitialization()
        {
            SaveController.Save();
            Assert.That(wrapper.Attempts, Is.Zero);
            SaveController.Save(true, false);
            Assert.That(wrapper.Attempts, Is.EqualTo(1));
            Set("globalSave", null);
            Assert.Throws<InvalidOperationException>(() => SaveController.SaveChecked());
        }

        private void BeginBlockedWrite(int amount)
        {
            wrapper.BlockFirst = true;
            value.Value = amount;
            SaveController.MarkAsSaveIsRequired();
            SaveController.Save();
            Assert.That(wrapper.Entered.Wait(TimeSpan.FromSeconds(5)), Is.True, "Writer did not start.");
        }

        private Task ReleaseFirstWriteSoon() => Task.Run(() =>
        {
            Thread.Sleep(100);
            wrapper.Release.Set();
        });

        private int[] ReadValues()
        {
            return wrapper.Snapshots.ConvertAll(snapshot =>
            {
                snapshot.Init(0);
                return snapshot.GetSaveObject<SimpleIntSave>(314159).Value;
            }).ToArray();
        }

        private bool Dirty => (bool)Get("isSaveRequired");
        private static object Get(string name) => typeof(SaveController).GetField(name, Flags).GetValue(null);
        private static void Set(string name, object value) => typeof(SaveController).GetField(name, Flags).SetValue(null, value);
        private static void SetPath(string path) => typeof(Serializer).GetField("persistentDataPath", Flags).SetValue(null, path);
        private static void CompleteWrites()
        {
            object coordinator = Get("writeCoordinator");
            coordinator.GetType().GetMethod("Complete").Invoke(coordinator, null);
        }

        private sealed class NonSerializable { }

        [Serializable]
        public sealed class FlushProbe : ISaveObject
        {
            public bool Throw;
            [NonSerialized] public int Thread;
            public void Flush()
            {
                Thread = System.Threading.Thread.CurrentThread.ManagedThreadId;
                if (Throw) throw new InvalidOperationException("simulated flush failure");
            }
        }

        private sealed class TestWrapper : BaseSaveWrapper, IDisposable
        {
            public readonly ManualResetEventSlim Entered = new ManualResetEventSlim();
            public readonly ManualResetEventSlim Release = new ManualResetEventSlim();
            public readonly List<GlobalSave> Snapshots = new List<GlobalSave>();
            public readonly List<string> Operations = new List<string>();
            public readonly List<int> Threads = new List<int>();
            public bool BlockFirst, Background = true;
            public int FailAttempt, Attempts, Loads, MaxConcurrent;
            private int concurrent;

            public override bool UseThreads() => Background;
            public override GlobalSave Load(string name)
            {
                Loads++;
                Operations.Add("load:" + name);
                return new GlobalSave();
            }
            public override void Delete(string name) => Operations.Add("delete:" + name);
            public override void Save(GlobalSave snapshot, string name)
            {
                int active = Interlocked.Increment(ref concurrent);
                MaxConcurrent = Math.Max(MaxConcurrent, active);
                int attempt = Interlocked.Increment(ref Attempts);
                try
                {
                    if (BlockFirst && attempt == 1)
                    {
                        Entered.Set();
                        if (!Release.Wait(TimeSpan.FromSeconds(10))) throw new TimeoutException("Test writer timed out.");
                    }
                    if (attempt == FailAttempt) throw new IOException("simulated write failure");
                    Snapshots.Add(snapshot);
                    Threads.Add(System.Threading.Thread.CurrentThread.ManagedThreadId);
                    Operations.Add("save:" + name);
                }
                finally { Interlocked.Decrement(ref concurrent); }
            }
            public void Dispose() { Entered.Dispose(); Release.Dispose(); }
        }
    }
}
