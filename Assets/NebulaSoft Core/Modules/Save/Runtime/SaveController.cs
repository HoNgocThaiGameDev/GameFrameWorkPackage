using System;
using System.Collections;
using UnityEngine;

namespace NebulaSoft
{
    [StaticUnload]
    public static class SaveController
    {
        private const string SAVE_FILE_NAME = "save";

        private static GlobalSave globalSave;

        private static bool isSaveLoaded;
        public static bool IsSaveLoaded => isSaveLoaded;
        public static bool IsWriteBlocked => Serializer.IsWriteBlocked(SAVE_FILE_NAME);
        public static System.Collections.Generic.Dictionary<string, object> ExportCloudData()
        {
            if (!isSaveLoaded) throw new InvalidOperationException("Save is not initialized.");
            return globalSave.ExportCloudData();
        }

        public static void ApplyCloudData(System.Collections.Generic.IDictionary<string, object> data)
        {
            if (!isSaveLoaded) throw new InvalidOperationException("Save is not initialized.");
            globalSave.ApplyCloudData(data);
            MarkAsSaveIsRequired();
        }

        public static void Shutdown()
        {
            try { if (isSaveLoaded) SaveChecked(); }
            finally { UnloadStatic(); }
        }

        private static bool isSaveRequired;
        private static readonly object stateGate = new object();
        private static long saveRevision;
        private static bool isInitializing;
        private static UnityCallbackReciever callbackReciever;
        private static SaveWriteCoordinator writeCoordinator = new SaveWriteCoordinator();
        private static BaseSaveWrapper saveWrapper = BaseSaveWrapper.ActiveWrapper;

        public static float GameTime => globalSave.GameTime;

        public static DateTime LastExitTime => globalSave.LastExitTime;

        public static event SimpleCallback OnSaveLoaded;

        public static void Init(float autoSaveDelay, bool clearSave = false, float overrideTime = -1f)
        {
            if (isSaveLoaded || isInitializing) return;
            isInitializing = true;
            try
            {
                Serializer.Init();
                if (clearSave)
                {
                    writeCoordinator.Execute(() => saveWrapper.Delete(SAVE_FILE_NAME));
                    InitClear(overrideTime != -1f ? overrideTime : Time.time);
                }
                else
                    Load(overrideTime != -1f ? overrideTime : Time.time);

                GameObject receiverObject = new GameObject("[SAVE CALLBACK RECIEVER]");
                receiverObject.hideFlags = HideFlags.HideInHierarchy;
                if (Application.isPlaying) GameObject.DontDestroyOnLoad(receiverObject);
                callbackReciever = receiverObject.AddComponent<UnityCallbackReciever>();
                if (autoSaveDelay > 0)
                    callbackReciever.StartCoroutine(AutoSaveCoroutine(autoSaveDelay));
            }
            catch
            {
                UnloadStatic();
                throw;
            }
            finally { isInitializing = false; }
        }

        public static void UpdateTime(float time)
        {
            globalSave.Time = time;
        }

        public static T GetSaveObject<T>(int hash) where T : ISaveObject, new()
        {
            if (!isSaveLoaded)
            {
                Debug.LogError("Save controller has not been initialized");
                return default;
            }

            return globalSave.GetSaveObject<T>(hash);
        }

        public static T GetSaveObject<T>() where T : ISaveObject, new()
        {
            if (!isSaveLoaded) throw new InvalidOperationException("Save is not initialized.");
            return globalSave.GetSaveObject<T>();
        }

        public static T GetSaveObject<T>(string uniqueName) where T : ISaveObject, new()
        {
            if (!isSaveLoaded)
            {
                Debug.LogError("Save controller has not been initialized");
                return default;
            }
            return globalSave.GetSaveObject<T>(uniqueName);
        }

        public static T GetSaveObject<T>(string uniqueName, int legacyHash) where T : ISaveObject, new()
        {
            if (!isSaveLoaded) throw new InvalidOperationException("Save is not initialized.");
            return globalSave.GetSaveObject<T>(uniqueName, legacyHash);
        }

        private static void InitClear(float time)
        {
            globalSave = new GlobalSave();
            globalSave.Init(time);

            Debug.Log("[Save Controller]: Created clear save!");

            isSaveLoaded = true;
        }

        private static void Load(float time)
        {
            if (isSaveLoaded)
                return;

            // Try to read and deserialize file or create new one
            writeCoordinator.Execute(() => globalSave = saveWrapper.Load(SAVE_FILE_NAME));

            globalSave.Init(time);

            Debug.Log("[Save Controller]: Save is loaded!");

            isSaveLoaded = true;

            OnSaveLoaded?.Invoke();
        }

        public static void Save(bool forceSave = false, bool useThreads = true)
        {
            SaveCurrent(forceSave, useThreads, false);
        }

        public static void SaveChecked()
        {
            SaveCurrent(true, false, true);
        }

        private static void SaveCurrent(bool forceSave, bool useThreads, bool checkedWrite)
        {
            GlobalSave source = globalSave;
            long revision;
            lock (stateGate)
            {
                if (source == null)
                {
                    if (checkedWrite) throw new InvalidOperationException("Save is not initialized.");
                    return;
                }
                if (!forceSave && !isSaveRequired) return;
                isSaveRequired = true;
                revision = ++saveRevision;
            }

            try
            {
                source.Flush(true);
                GlobalSave snapshot = source.CreateSnapshot();
                BaseSaveWrapper wrapper = saveWrapper;
                writeCoordinator.Execute(() =>
                {
                    try
                    {
                        wrapper.Save(snapshot, SAVE_FILE_NAME);
                        lock (stateGate)
                        {
                            if (revision == saveRevision && ReferenceEquals(source, globalSave))
                                isSaveRequired = false;
                        }
                    }
                    catch (Exception exception)
                    {
                        if (checkedWrite) throw;
                        ReportSaveFailure(exception);
                    }
                }, useThreads && wrapper.UseThreads());
            }
            catch (Exception exception)
            {
                if (checkedWrite) throw;
                ReportSaveFailure(exception);
            }
        }

        private static void ReportSaveFailure(Exception exception)
        {
            Debug.LogError("[Save Controller]: Failed to save: " + exception.Message);
        }

        public static void SaveCustom(GlobalSave globalSave)
        {
            if(globalSave != null)
            {
                globalSave.Flush(false);

                GlobalSave snapshot = globalSave.CreateSnapshot();
                writeCoordinator.Execute(() => saveWrapper.Save(snapshot, SAVE_FILE_NAME));
            }
        }

        public static void MarkAsSaveIsRequired()
        {
            lock (stateGate)
            {
                saveRevision++;
                isSaveRequired = true;
            }
        }

        private static IEnumerator AutoSaveCoroutine(float saveDelay)
        {
            WaitForSeconds waitForSeconds = new WaitForSeconds(saveDelay);

            while (true)
            {
                yield return waitForSeconds;

                Save();
            }
        }

        public static void PresetsSave(string fullFileName)
        {
            globalSave.Flush(false);

            GlobalSave snapshot = globalSave.CreateSnapshot();
            writeCoordinator.Execute(() => saveWrapper.Save(snapshot, fullFileName));
        }

        public static void Info()
        {
            globalSave.Info();
        }

        public static void DeleteSaveFile()
        {
            writeCoordinator.Execute(() => saveWrapper.Delete(SAVE_FILE_NAME));
        }

        public static GlobalSave GetGlobalSave()
        {
            GlobalSave tempGlobalSave = null;
            writeCoordinator.Execute(() => tempGlobalSave = saveWrapper.Load(SAVE_FILE_NAME));

            tempGlobalSave.Init(Time.time);

            return tempGlobalSave;
        }

        private static void UnloadStatic()
        {
            writeCoordinator.Complete();
            UnityCallbackReciever receiver = callbackReciever;
            callbackReciever = null;
            if (receiver != null)
            {
                receiver.StopAllCoroutines();
                if (Application.isPlaying) UnityEngine.Object.Destroy(receiver.gameObject);
                else UnityEngine.Object.DestroyImmediate(receiver.gameObject);
            }
            globalSave = null;

            isSaveLoaded = false;
            isSaveRequired = false;
            saveRevision = 0;
            isInitializing = false;

            OnSaveLoaded = null;
        }

        private class UnityCallbackReciever : MonoBehaviour
        {
            private void OnDestroy()
            {
                if (callbackReciever != this) return;
#if UNITY_EDITOR
                SaveController.Save(true, false);
#endif
                callbackReciever = null;
            }

            private void OnApplicationFocus(bool focus)
            {
#if !UNITY_EDITOR
                if(!focus) SaveController.Save(false, false);
#endif
            }

            private void OnApplicationPause(bool paused)
            {
                if (paused && callbackReciever == this) SaveController.Save(false, false);
            }

            private void OnApplicationQuit()
            {
                if (callbackReciever == this) SaveController.Save(true, false);
            }
        }
    }
}
