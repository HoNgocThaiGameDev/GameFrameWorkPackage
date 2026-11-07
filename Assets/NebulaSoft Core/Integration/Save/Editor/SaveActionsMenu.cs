using UnityEngine;
using UnityEditor;

namespace NebulaSoft
{
    public static class SaveActionsMenu
    {
        [MenuItem("Actions/Remove Save", priority = 1)]
        [MenuItem("Edit/Clear Save", priority = 270)]
        private static void RemoveSave()
        {
            PlayerPrefs.DeleteAll();
            SaveController.DeleteSaveFile();

            Debug.Log("Save files are removed!");
        }

        [MenuItem("Actions/Remove Save", true)]
        private static bool RemoveSaveValidation()
        {
            return !Application.isPlaying;
        }
    }
}