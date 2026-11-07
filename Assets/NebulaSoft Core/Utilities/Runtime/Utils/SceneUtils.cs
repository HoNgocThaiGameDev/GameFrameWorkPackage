using UnityEngine.SceneManagement;

namespace NebulaSoft
{
    public static class SceneUtils
    {
        /// <summary>
        /// Returns true if the scene 'name' exists and is in your Build settings, false otherwise
        /// </summary>
        public static bool DoesSceneExist(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                var scenePath = SceneUtility.GetScenePathByBuildIndex(i);
                var lastSlash = scenePath.LastIndexOf("/");
                var sceneName = scenePath.Substring(lastSlash + 1, scenePath.LastIndexOf(".") - lastSlash - 1);

                if (string.Compare(name, sceneName, true) == 0)
                    return true;
            }

            return false;
        }

        public static int GetBuildIndexByName(string sceneName)
        {
#if UNITY_EDITOR
            int enabledIndex = 0;
            foreach (UnityEditor.EditorBuildSettingsScene s in UnityEditor.EditorBuildSettings.scenes)
            {
                if (!s.enabled) continue;

                string name = System.IO.Path.GetFileNameWithoutExtension(s.path);
                if (name == sceneName)
                    return enabledIndex;

                enabledIndex++;
            }

            return -1;
#else
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string path = SceneUtility.GetScenePathByBuildIndex(i);
                string name = System.IO.Path.GetFileNameWithoutExtension(path);

                if (name == sceneName)
                    return i;
            }

            return -1;
#endif
        }
    }
}