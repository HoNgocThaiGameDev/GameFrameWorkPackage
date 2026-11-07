using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
using UnityEditor;
#endif
using UnityEngine;
using System.IO;

namespace NebulaSoft
{
    public class SavePresets
    {
        private const string PRESET_FOLDER_PREFIX = "SavePresets/";
        private const string PRESETS_FOLDER_NAME = "SavePresets";
        private const string SAVE_FILE_NAME = "save";
        public static bool saveDataMofied = false;
        private const char SEPARATOR = '/';
        public const string DEFAULT_DIRECTORY = "Custom";
        public const string META_SUFFIX = ".meta";

        private static void LoadSaveFromPath(string presetPath)
        {
#if UNITY_EDITOR
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[Save Presets]: Preset can't be activated in playmode!");
                return;
            }

            if (EditorApplication.isCompiling)
            {
                Debug.LogError("[Save Presets]: Preset can't be activated during compiling!");
                return;
            }

            if (!File.Exists(presetPath))
            {
                Debug.LogError(string.Format("[Save Presets]: Preset  at path {0} doesn’t  exist!", presetPath));
                return;
            }

            string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

            if (currentSceneName.Equals("Init"))
            {
                EditorSceneManager.OpenScene(Path.Combine(CoreEditor.FOLDER_SCENES, "Game.unity"));
            }

            // Replace current save file with the preset
            File.Copy(presetPath, GetSavePath(), true);

            // Start game
            EditorApplication.isPlaying = true;
#endif
        }

        private static void CreateSavePreset(string saveName, string tabName = DEFAULT_DIRECTORY)
        {
#if UNITY_EDITOR
            if (EditorApplication.isPlaying)
                SaveController.Save(true, false);

            if (string.IsNullOrEmpty(saveName))
            {
                Debug.LogError("[Save Presets]: Preset name can't be empty!");
                return;
            }

            if (!Directory.Exists(GetDirectoryPath())) //Creating SavePresets folder
            {
                Directory.CreateDirectory(GetDirectoryPath());
            }

            if (!Directory.Exists(GetDirectoryPath(tabName))) //Creating custom folder
            {
                Directory.CreateDirectory(GetDirectoryPath(tabName));
            }

            string savePath = GetSavePath();

            string presetPath = GetPresetPath(saveName, tabName);

            if (EditorApplication.isPlaying)
            {
                SaveController.PresetsSave(PRESET_FOLDER_PREFIX + tabName + SEPARATOR + saveName);
            }
            else
            {
                if (!File.Exists(savePath))
                {
                    Debug.LogError("[Save Presets]: Save file doesn’t exist!");

                    return;
                }

                File.Copy(savePath, presetPath, true);
            }

            File.SetCreationTime(presetPath, DateTime.Now);

            saveDataMofied = true;
#endif
        }



        public static void LoadSave(string saveName, string tabName = DEFAULT_DIRECTORY)
        {
            string presetPath = GetPresetPath(saveName, tabName);
            LoadSaveFromPath(presetPath);
        }

        public static void CreateSave(string saveName, string tabName = DEFAULT_DIRECTORY, string id = "")
        {
#if UNITY_EDITOR
            if (id.Length == 0)
            {
                id = saveName;
            }

            CreateSavePreset(saveName, tabName);
            SetId(saveName, tabName, id);
#endif
        }

        public static void SetId(string name, string tabName, string id)
        {
            string presetPath = GetPresetPath(name, tabName) + META_SUFFIX;
            File.WriteAllText(presetPath, id);
        }

        private static string GetPresetPathById(string id)
        {
            string directoryPath = SavePresets.GetDirectoryPath();
            string[] directoryEntries = Directory.GetDirectories(directoryPath);
            string[] fileEntries;

            for (int i = 0; i < directoryEntries.Length; i++)
            {
                fileEntries = Directory.GetFiles(directoryEntries[i]);

                for (int j = 0; j < fileEntries.Length; j++)
                {
                    if (fileEntries[j].EndsWith(SavePresets.META_SUFFIX))
                    {
                        if (File.ReadAllText(fileEntries[j]).Equals(id))
                        {
                            return fileEntries[j].Replace(SavePresets.META_SUFFIX,string.Empty);
                        }
                    }
                }
            }

            return string.Empty;
        }

        public static void LoadSaveById(string id)
        {
            string presetPath = GetPresetPathById(id);

            if (presetPath.Length == 0)
            {
                Debug.LogError(string.Format("[Save Presets]: Preset with id {0} doesn’t  exist!", id));
                return;
            }

            LoadSaveFromPath(presetPath);
        }

        public static void RemoveSave(string saveName, string tabName = DEFAULT_DIRECTORY)
        {
            string presetPath = GetPresetPath(saveName, tabName);

            if (File.Exists(presetPath))
            {
                File.Delete(presetPath);
            }

            presetPath += META_SUFFIX;

            if (File.Exists(presetPath))
            {
                File.Delete(presetPath);
            }

            saveDataMofied = true;
        }

        public static bool IsSaveExist(string saveName, string tabName = DEFAULT_DIRECTORY)
        {
            string presetPath = GetPresetPath(saveName, tabName);
            return File.Exists(presetPath);
        }


        public static bool IsSaveExistById(string id)
        {
            string presetPath = GetPresetPathById(id);

            if(presetPath.Length == 0) // id isn`t found
            {
                return false;
            }

            if (File.Exists(presetPath))
            {
                return true;
            }
            else // remove meta file
            {
                File.Delete(presetPath + META_SUFFIX);
                return false;
            }
        }



        public static void RemoveSaveById(string id)
        {
            string presetPath = GetPresetPathById(id);

            if (presetPath.Length == 0) // id isn`t found
            {
                return;
            }

            if (File.Exists(presetPath))
            {
                File.Delete(presetPath);
            }

            presetPath += META_SUFFIX;

            if (File.Exists(presetPath))
            {
                File.Delete(presetPath);
            }
            saveDataMofied = true;
        }

        public static string GetSavePath()
        {
            return Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);
        }

        public static string GetPresetPath(string saveName, string tabName)
        {
            return Path.Combine(Application.persistentDataPath, PRESETS_FOLDER_NAME, tabName, saveName);
        }

        public static string GetDirectoryPath()
        {
            return Path.Combine(Application.persistentDataPath, PRESETS_FOLDER_NAME);
        }

        public static string GetDirectoryPath(string tabName)
        {
            return Path.Combine(Application.persistentDataPath, PRESETS_FOLDER_NAME, tabName);
        }

        public static string GetFileName(string path)
        {
            return Path.GetFileName(path);
        }

        public static string GetDirectoryName(string path)
        {
            return Path.GetDirectoryName(path);
        }
    }
}
