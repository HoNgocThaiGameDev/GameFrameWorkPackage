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
            return GetSaveObject<T>(typeof(T).FullName);
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
