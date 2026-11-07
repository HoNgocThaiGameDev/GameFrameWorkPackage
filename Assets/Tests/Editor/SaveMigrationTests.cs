using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace NebulaSoft.Tests
{
    public sealed class SaveMigrationTests
    {
        private static readonly FieldInfo PathField = typeof(Serializer).GetField("persistentDataPath", BindingFlags.NonPublic | BindingFlags.Static);
        private object previousPath;
        private string directory;

        [OneTimeSetUp]
        public void RegisterMigrations()
        {
            SaveSchema.RegisterMigration<VersionedModel>(0, data => { data["count"] = data["oldCount"]; data.Remove("oldCount"); });
            SaveSchema.RegisterMigration<VersionedModel>(1, data => data["bonus"] = 7);
        }

        [OneTimeTearDown]
        public void RemoveFixtureMigrations()
        {
            // The Editor can rerun this fixture without reloading the static registry.
            var field = typeof(SaveSchema).GetField("migrations", BindingFlags.NonPublic | BindingFlags.Static);
            var registry = (System.Collections.IDictionary)field.GetValue(null);
            registry.Remove(typeof(VersionedModel));
        }

        [SetUp]
        public void SetUp()
        {
            previousPath = PathField.GetValue(null);
            directory = Path.Combine(Path.GetTempPath(), "NebulaMigrations_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            PathField.SetValue(null, directory);
        }

        [TearDown]
        public void TearDown()
        {
            Serializer.DeleteFileAtPDP("save");
            Directory.Delete(directory, true);
            PathField.SetValue(null, previousPath);
        }

        [Test]
        public void ModelMigrations_RunInOrder_KeepUnknownFields_AndAddConstructorDefaults()
        {
            var global = LoadModel("{\"oldCount\":23,\"unknownFeature\":{\"value\":99}}", 0);
            var model = global.GetSaveObject<VersionedModel>();
            Assert.That(model.count, Is.EqualTo(23));
            Assert.That(model.bonus, Is.EqualTo(7));
            Assert.That(model.newlyAdded, Is.EqualTo(42));
            model.count = 24;
            global.Flush(false);
            Serializer.SerializeChecked(global, "save");
            var file = JObject.Parse(File.ReadAllText(FilePath));
            var entry = file["entries"][0];
            Assert.That((int)entry["modelVersion"], Is.EqualTo(2));
            var payload = JObject.Parse((string)entry["json"]);
            Assert.That((int)payload["unknownFeature"]["value"], Is.EqualTo(99));
            Assert.That(payload["oldCount"], Is.Null);
            var restored = Serializer.Deserialize<GlobalSave>("save");
            restored.Init(0);
            Assert.That(restored.GetSaveObject<VersionedModel>().count, Is.EqualTo(24));
        }

        [Test]
        public void NewerModel_IsRejectedWithoutChangingItsRawData()
        {
            var global = LoadModel("{\"count\":23,\"future\":true}", 3);
            string original = File.ReadAllText(FilePath);
            Assert.Throws<InvalidOperationException>(() => global.GetSaveObject<VersionedModel>());
            Assert.That(File.ReadAllText(FilePath), Is.EqualTo(original));
        }

        [Test]
        public void UnknownItemFields_FollowStableIdsWhenACollectionIsReordered()
        {
            var global = LoadModel("{\"items\":[{\"id\":\"a\",\"amount\":1,\"future\":7},{\"id\":\"b\",\"amount\":2,\"future\":9}]}", 0, "collection");
            var model = global.GetSaveObject<CollectionModel>("collection");
            Array.Reverse(model.items);
            global.Flush(false); Serializer.SerializeChecked(global, "save");
            var payload = JObject.Parse((string)JObject.Parse(File.ReadAllText(FilePath))["entries"][0]["json"]);
            Assert.That((string)payload["items"][0]["id"], Is.EqualTo("b"));
            Assert.That((int)payload["items"][0]["future"], Is.EqualTo(9));
            Assert.That((int)payload["items"][1]["future"], Is.EqualTo(7));
        }

        [Test]
        public void MissingMigration_FailsBeforePublishingAModel()
        {
            var global = LoadModel("{\"count\":23}", 0, "missing-chain");
            Assert.Throws<InvalidOperationException>(() => global.GetSaveObject<MissingMigrationModel>());
            Assert.That(File.ReadAllText(FilePath), Does.Contain("count"));
        }

        [Test]
        public void CorruptPrimary_RecoversBackup_AndKeepsTheBackupOnRepair()
        {
            var global = new GlobalSave(); global.Init(0);
            var model = global.GetSaveObject<SimpleIntSave>("amount");
            model.Value = 10; global.Flush(false); Serializer.SerializeChecked(global, "save");
            model.Value = 20; global.Flush(false); Serializer.SerializeChecked(global, "save");
            byte[] backup = File.ReadAllBytes(FilePath + ".bak");
            File.WriteAllText(FilePath, "damaged");
            LogAssert.Expect(LogType.Warning, new Regex("Recovered save from backup"));
            var recovered = Serializer.Deserialize<GlobalSave>("save"); recovered.Init(0);
            Assert.That(recovered.GetSaveObject<SimpleIntSave>("amount").Value, Is.EqualTo(10));
            recovered.Flush(false); Serializer.SerializeChecked(recovered, "save");
            Assert.That(File.ReadAllBytes(FilePath + ".bak"), Is.EqualTo(backup));
        }

        [Test]
        public void BothFilesCorrupt_BlockWritesUntilExplicitReset()
        {
            File.WriteAllText(FilePath, "primary evidence");
            File.WriteAllText(FilePath + ".bak", "backup evidence");
            Assert.Throws<IOException>(() => Serializer.Deserialize<GlobalSave>("save"));
            Assert.That(Serializer.IsWriteBlocked("save"), Is.True);
            var fresh = new GlobalSave(); fresh.Init(0); fresh.Flush(false);
            Assert.Throws<IOException>(() => Serializer.SerializeChecked(fresh, "save"));
            Assert.That(File.ReadAllText(FilePath), Is.EqualTo("primary evidence"));
            Assert.That(File.ReadAllText(FilePath + ".bak"), Is.EqualTo("backup evidence"));
            Serializer.DeleteFileAtPDP("save");
            Serializer.SerializeChecked(fresh, "save");
            Assert.That(Serializer.IsWriteBlocked("save"), Is.False);
        }

        [Test]
        public void FutureEnvelope_CannotBeDowngradedEvenWithAnOldBackup()
        {
            var global = LoadModel("{\"count\":23}", 2);
            var data = JObject.Parse(File.ReadAllText(FilePath));
            File.Copy(FilePath, FilePath + ".bak");
            data["version"] = 99;
            File.WriteAllText(FilePath, data.ToString());
            Assert.Throws<NotSupportedException>(() => Serializer.Deserialize<GlobalSave>("save"));
            Assert.Throws<IOException>(() => Serializer.SerializeChecked(global, "save"));
            Assert.That((int)JObject.Parse(File.ReadAllText(FilePath))["version"], Is.EqualTo(99));
        }

        [Test]
        public void CloudExport_IsOptIn_AndDoesNotIncludeLocalOnlyData()
        {
            var global = new GlobalSave(); global.Init(0);
            global.GetSaveObject<VersionedModel>().count = 8;
            global.GetSaveObject<SimpleStringSave>("local-only").Value = "private";
            var payload = global.ExportCloudData();
            var models = (Dictionary<string, object>)payload["models"];
            Assert.That(models.Count, Is.EqualTo(1));
            Assert.That(Newtonsoft.Json.JsonConvert.SerializeObject(payload), Does.Not.Contain("private"));
        }

        [Test]
        public void CloudRestore_MigratesKnownModels_AndKeepsUnrecognizedModelsOnDisk()
        {
            var local = new GlobalSave(); local.Init(0);
            var live = local.GetSaveObject<VersionedModel>();
            var cloud = Envelope("upgrade", 0, "{\"oldCount\":35,\"extra\":true}");
            var unknown = Model("next-release", 8, "{\"nested\":{\"value\":4}}");
            ((Dictionary<string, object>)cloud["models"])[Key("next-release")] = unknown;
            local.ApplyCloudData(cloud);
            Assert.That(live.count, Is.EqualTo(35));
            Assert.That(live.bonus, Is.EqualTo(7));
            local.Flush(false); Serializer.SerializeChecked(local, "save");
            string raw = File.ReadAllText(FilePath);
            Assert.That(raw, Does.Contain("next-release"));
            Assert.That(raw, Does.Contain("nested"));
            Assert.That(((Dictionary<string, object>)local.ExportCloudData()["models"]).Count, Is.EqualTo(1));
        }

        [Test]
        public void CloudValidation_RejectsVersionDowngrades_AndAllowsUnrelatedModels()
        {
            var remote = new Dictionary<string, object> { { "saveVersion", 4 }, { SaveCloudData.Field, Envelope("upgrade", 3, "{}") } };
            var local = new Dictionary<string, object> { { "saveVersion", 4 }, { SaveCloudData.Field, Envelope("upgrade", 2, "{}") } };
            Assert.Throws<InvalidOperationException>(() => SaveCloudData.ValidateWrite(remote, local));
            local[SaveCloudData.Field] = Envelope("other", 0, "{}");
            Assert.DoesNotThrow(() => SaveCloudData.ValidateWrite(remote, local));
            remote["saveVersion"] = 5;
            Assert.Throws<InvalidOperationException>(() => SaveCloudData.ValidateWrite(remote, local));
        }

        [Test]
        public void CloudRestore_FailureInLaterModel_DoesNotMutateEarlierModels()
        {
            var local = new GlobalSave(); local.Init(0);
            var live = local.GetSaveObject<VersionedModel>(); live.count = 9;
            var cloud = Envelope("upgrade", 2, "{\"count\":80}");
            ((Dictionary<string, object>)cloud["models"])[Key("invalid")] = Model("invalid", -1, "{}");
            Assert.Throws<InvalidOperationException>(() => local.ApplyCloudData(cloud));
            Assert.That(live.count, Is.EqualTo(9));
        }

        [Test]
        public void EmptyCloudExport_IsOmittedFromMergeWritesToPreserveOtherClientsModels()
        {
            var global = new GlobalSave(); global.Init(0);
            var update = new Dictionary<string, object> { { "saveVersion", 4 } };
            SaveCloudData.AddToUpdate(update, global.ExportCloudData());
            Assert.That(update.ContainsKey(SaveCloudData.Field), Is.False);
            var existing = new Dictionary<string, object> { { SaveCloudData.Field, Envelope("unknown", 7, "{}") } };
            Assert.DoesNotThrow(() => SaveCloudData.ValidateWrite(existing, update));
            update[SaveCloudData.Field] = global.ExportCloudData();
            Assert.Throws<InvalidOperationException>(() => SaveCloudData.ValidateWrite(existing, update));
        }

        private string FilePath => Path.Combine(directory, "save");
        private GlobalSave LoadModel(string json, int version, string key = "upgrade")
        {
            var file = new JObject { ["format"] = "NebulaSoft.Save", ["version"] = 1, ["schemaVersion"] = 2,
                ["gameTime"] = 0, ["lastExitTime"] = 0,
                ["entries"] = new JArray(new JObject { ["key"] = key, ["json"] = json, ["modelVersion"] = version, ["cloudSync"] = true }) };
            File.WriteAllText(FilePath, file.ToString(Newtonsoft.Json.Formatting.None));
            var global = Serializer.Deserialize<GlobalSave>("save"); global.Init(0); return global;
        }
        private static string Key(string key) => Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(key)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        private static Dictionary<string, object> Model(string key, int version, string json) => new Dictionary<string, object>
            { { "key", key }, { "version", version }, { "json", json } };
        private static Dictionary<string, object> Envelope(string key, int version, string json) => new Dictionary<string, object>
            { { "version", 1 }, { "models", new Dictionary<string, object> { { Key(key), Model(key, version, json) } } } };

        [Serializable, SaveModel("upgrade", 2, CloudSync = true)]
        public sealed class VersionedModel : ISaveObject
        {
            public int count;
            public int bonus;
            public int newlyAdded = 42;
            public void Flush() { }
        }
        [Serializable, SaveModel("missing-chain", 3)]
        public sealed class MissingMigrationModel : ISaveObject
        {
            public int count;
            public void Flush() { }
        }
        [Serializable]
        public sealed class CollectionModel : ISaveObject
        {
            public CollectionItem[] items;
            public void Flush() { }
        }
        [Serializable]
        public sealed class CollectionItem
        {
            public string id;
            public int amount;
        }
    }
}
