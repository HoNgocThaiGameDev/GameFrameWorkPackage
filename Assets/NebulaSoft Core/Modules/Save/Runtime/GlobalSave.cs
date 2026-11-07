using System;
using UnityEngine;
using System.Collections.Generic;

namespace NebulaSoft
{
    [Serializable]
    public class GlobalSave
    {
        [SerializeField] SavedDataContainer[] saveObjects;
        private List<SavedDataContainer> saveObjectsList;

        [System.Runtime.Serialization.OptionalField, SerializeField] int schemaVersion;
        public int SchemaVersion => schemaVersion;

        [SerializeField] float gameTime;
        public float GameTime => gameTime + (Time - lastFlushTime);

        [SerializeField] DateTime lastExitTime;
        public DateTime LastExitTime => lastExitTime;

        // Explicit file data avoids serializing CLR type/assembly names in new native saves.
        internal FileData ExportFileData() => new FileData
        {
            format = "NebulaSoft.Save", version = 1, schemaVersion = schemaVersion,
            gameTime = gameTime, lastExitTime = lastExitTime.ToBinary(), entries = saveObjects
        };

        internal static GlobalSave ImportFileData(FileData data)
        {
            if (data != null && (data.version > 1 || data.schemaVersion > 2))
                throw new NotSupportedException("Save format is newer than this client.");
            if (data == null || data.format != "NebulaSoft.Save" || data.version != 1)
                throw new System.IO.InvalidDataException("Unsupported save format or schema version.");
            if (data.entries == null) throw new System.IO.InvalidDataException("Save entries are missing.");
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var entry in data.entries)
            {
                if (entry == null) throw new System.IO.InvalidDataException("Save contains a missing entry.");
                if (!string.IsNullOrEmpty(entry.Key) && !keys.Add(entry.Key))
                    throw new System.IO.InvalidDataException("Save contains duplicate model IDs.");
                if (entry.ModelVersion < 0 || string.IsNullOrWhiteSpace(entry.Json))
                    throw new System.IO.InvalidDataException("Save contains an invalid model payload.");
                Newtonsoft.Json.Linq.JObject.Parse(entry.Json);
            }
            return new GlobalSave { schemaVersion = data.schemaVersion, gameTime = data.gameTime,
                lastExitTime = DateTime.FromBinary(data.lastExitTime), saveObjects = data.entries };
        }

        [Serializable]
        internal sealed class FileData
        {
            public string format;
            public int version;
            public int schemaVersion;
            public float gameTime;
            public long lastExitTime;
            public SavedDataContainer[] entries;
        }

        private float lastFlushTime = 0;

        public float Time { get; set; }

        public void Init(float time)
        {
            if (saveObjects == null)
            {
                saveObjectsList = new List<SavedDataContainer>();
            }
            else
            {
                saveObjectsList = new List<SavedDataContainer>(saveObjects);
            }

            for (int i = 0; i < saveObjectsList.Count; i++)
            {
                saveObjectsList[i].Restored = false;
            }

            Time = time;
            lastFlushTime = Time;
        }

        public void Flush(bool updateLastExitTime)
        {
            schemaVersion = 2;
            saveObjects = saveObjectsList.ToArray();

            for (int i = 0; i < saveObjectsList.Count; i++)
            {
                SavedDataContainer saveObject = saveObjectsList[i];

                saveObject.Flush();
            }

            gameTime += Time - lastFlushTime;

            lastFlushTime = Time;

            if(updateLastExitTime) lastExitTime = DateTime.Now;
        }

        // Call after Flush, on the main thread. No live ISaveObject reaches the writer.
        internal GlobalSave CreateSnapshot()
        {
            GlobalSave snapshot = (GlobalSave)MemberwiseClone();
            snapshot.saveObjects = new SavedDataContainer[saveObjectsList.Count];
            for (int i = 0; i < saveObjectsList.Count; i++)
                snapshot.saveObjects[i] = saveObjectsList[i].CreateSnapshot();
            snapshot.saveObjectsList = new List<SavedDataContainer>(snapshot.saveObjects);
            return snapshot;
        }

        public T GetSaveObject<T>(int hash) where T : ISaveObject, new()
        {
            SavedDataContainer container = saveObjectsList.Find((container) => string.IsNullOrEmpty(container.Key) && container.Hash == hash);

            if (container == null)
            {
                container = new SavedDataContainer(hash, new T());

                saveObjectsList.Add(container);

            }
            else
            {
                if (!container.Restored) container.Restore<T>();
            }
            return (T)container.SaveObject;
        }

        public T GetSaveObject<T>(string uniqueName) where T : ISaveObject, new()
        {
            return GetSaveObject<T>(uniqueName, uniqueName?.GetHashCode() ?? 0);
        }

        // Pass the original release hash explicitly when migrating across runtimes.
        public T GetSaveObject<T>(string uniqueName, int legacyHash) where T : ISaveObject, new()
        {
            if (string.IsNullOrWhiteSpace(uniqueName)) throw new ArgumentException("A stable save key is required.", nameof(uniqueName));
            var container = saveObjectsList.Find(value => string.Equals(value.Key, uniqueName, StringComparison.Ordinal));
            if (container == null)
            {
                container = saveObjectsList.Find(value => string.IsNullOrEmpty(value.Key) && value.Hash == legacyHash);
                if (container != null) container.MigrateKey(uniqueName);
                else
                {
                    container = new SavedDataContainer(uniqueName, new T());
                    saveObjectsList.Add(container);
                }
            }
            if (!container.Restored) container.Restore<T>();
            if (!(container.SaveObject is T result))
                throw new InvalidOperationException("Save key '" + uniqueName + "' is already used by a different model type.");
            return result;
        }

        public T GetSaveObject<T>() where T : ISaveObject, new()
        {
            string oldKey = typeof(T).FullName;
            string key = SaveSchema.Definition(typeof(T))?.Id ?? oldKey;
            if (key != oldKey && !saveObjectsList.Exists(value => value.Key == key))
                saveObjectsList.Find(value => value.Key == oldKey)?.MigrateKey(key);
            return GetSaveObject<T>(key, oldKey.GetHashCode());
        }

        public Dictionary<string, object> ExportCloudData()
        {
            var models = new Dictionary<string, object>(StringComparer.Ordinal);
            foreach (var entry in saveObjectsList)
            {
                if (!entry.CloudSync || !entry.Restored || string.IsNullOrEmpty(entry.Key)) continue;
                entry.Flush();
                models.Add(SaveCloudData.EncodeKey(entry.Key), new Dictionary<string, object>
                {
                    { "key", entry.Key }, { "version", entry.ModelVersion }, { "json", entry.Json }
                });
            }
            return new Dictionary<string, object> { { "version", SaveCloudData.FormatVersion }, { "models", models } };
        }

        public void ApplyCloudData(IDictionary<string, object> envelope)
        {
            SaveCloudData.ValidateEnvelope(envelope);
            var changes = new List<Action>();
            foreach (var pair in SaveCloudData.AsMap(envelope["models"]))
            {
                var model = SaveCloudData.AsMap(pair.Value);
                string key = (string)model["key"];
                if (string.IsNullOrWhiteSpace(key) || SaveCloudData.EncodeKey(key) != pair.Key)
                    throw new InvalidOperationException("Cloud save key does not match its model.");
                int version = Convert.ToInt32(model["version"]);
                if (version < 0) throw new InvalidOperationException("Invalid model version.");
                string payload = (string)model["json"];
                Newtonsoft.Json.Linq.JObject.Parse(payload);
                var entry = saveObjectsList.Find(value => value.Key == key);
                if (entry == null)
                {
                    var incoming = new SavedDataContainer(key, payload, version);
                    changes.Add(() => saveObjectsList.Add(incoming));
                    continue;
                }
                if (!entry.CloudSync) continue;
                if (entry.SaveObject == null && entry.ModelVersion > version)
                    throw new InvalidOperationException("Cannot downgrade an unrecognized local model: " + key);
                if (entry.SaveObject != null)
                {
                    var type = entry.SaveObject.GetType();
                    payload = SaveSchema.Migrate(type, payload, version).ToString(Newtonsoft.Json.Formatting.None);
                    var candidate = Activator.CreateInstance(type);
                    JsonUtility.FromJsonOverwrite(payload, candidate);
                    version = SaveSchema.Definition(type)?.Version ?? 0;
                }
                string merged = SaveSchema.MergeFields(entry.Json, payload);
                int targetVersion = version;
                changes.Add(() => entry.ApplyCloudData(merged, targetVersion));
            }
            foreach (var apply in changes) apply();
        }

        public void Info()
        {
            foreach (var container in saveObjectsList)
            {
                Debug.Log("Hash: " + container.Hash);
                Debug.Log("Save Object: " + container.SaveObject);
            }
        }
    }
}
