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
        public bool Restored { get; set; }

        [System.NonSerialized] ISaveObject saveObject;
        public ISaveObject SaveObject => saveObject;

        public SavedDataContainer(int hash, ISaveObject saveObject)
        {
            this.hash = hash;
            this.saveObject = saveObject;
            Restored = true;
        }

        public void Flush()
        {
            if (saveObject != null) saveObject.Flush();
            if (Restored) json = JsonUtility.ToJson(saveObject);
        }

        public SavedDataContainer(string key, ISaveObject saveObject) : this(0, saveObject)
        {
            this.key = key;
        }

        internal void MigrateKey(string stableKey) => key = stableKey;

        public void Restore<T>() where T : ISaveObject
        {
            saveObject = JsonUtility.FromJson<T>(json);
            Restored = true;
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
