using UnityEngine;

namespace NebulaSoft
{
    [System.Serializable]
    public class SavedDataContainer
    {
        [SerializeField] int hash;
        public int Hash => hash;

        [System.Runtime.Serialization.OptionalField, SerializeField] string key;
        public string Key => key;

        [SerializeField] string json;
        [System.Runtime.Serialization.OptionalField, SerializeField] int modelVersion;
        [System.Runtime.Serialization.OptionalField, SerializeField] bool cloudSync;
        public int ModelVersion => modelVersion;
        public bool CloudSync => cloudSync;
        public bool Restored { get; set; }

        [System.NonSerialized] ISaveObject saveObject;
        public ISaveObject SaveObject => saveObject;

        public SavedDataContainer(int hash, ISaveObject saveObject)
        {
            this.hash = hash;
            this.saveObject = saveObject;
            var definition = SaveSchema.Definition(saveObject.GetType());
            modelVersion = definition?.Version ?? 0;
            cloudSync = definition?.CloudSync ?? false;
            Restored = true;
        }

        public void Flush()
        {
            if (saveObject != null) saveObject.Flush();
            if (Restored) json = SaveSchema.MergeFields(json, JsonUtility.ToJson(saveObject));
        }

        public SavedDataContainer(string key, ISaveObject saveObject) : this(0, saveObject)
        {
            this.key = key;
        }

        internal void MigrateKey(string stableKey) => key = stableKey;

        public void Restore<T>() where T : ISaveObject
        {
            var migrated = SaveSchema.Migrate(typeof(T), json, modelVersion);
            // Deserialize into a constructed instance so fields added by a later release keep their defaults.
            T candidate = System.Activator.CreateInstance<T>();
            JsonUtility.FromJsonOverwrite(migrated.ToString(Newtonsoft.Json.Formatting.None), candidate);
            saveObject = candidate;
            json = migrated.ToString(Newtonsoft.Json.Formatting.None);
            var definition = SaveSchema.Definition(typeof(T));
            modelVersion = definition?.Version ?? 0;
            cloudSync = definition?.CloudSync ?? false;
            Restored = true;
        }

        internal string Json => json;
        internal SavedDataContainer(string key, string payload, int version)
        {
            this.key = key;
            json = payload;
            modelVersion = version;
            cloudSync = true;
        }

        internal void ApplyCloudData(string payload, int version)
        {
            if (saveObject != null)
                JsonUtility.FromJsonOverwrite(payload, saveObject);
            json = payload;
            modelVersion = version;
        }

        internal SavedDataContainer CreateSnapshot()
        {
            SavedDataContainer snapshot = (SavedDataContainer)MemberwiseClone();
            snapshot.saveObject = null;
            snapshot.Restored = false;
            return snapshot;
        }
    }
}
