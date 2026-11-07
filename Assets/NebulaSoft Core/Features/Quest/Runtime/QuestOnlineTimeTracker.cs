using UnityEngine;

namespace NebulaSoft
{
    public sealed class QuestOnlineTimeTracker : MonoBehaviour
    {
        private const float SaveIntervalSeconds = 1f;

        private float elapsedSeconds;
        private bool isForeground;

        public void Initialise()
        {
            isForeground = Application.isFocused;
        }

        private void Update()
        {
            if (!isForeground || !QuestService.IsInitialized)
                return;

            elapsedSeconds += Time.unscaledDeltaTime;
            if (elapsedSeconds >= SaveIntervalSeconds)
                CommitElapsedTime(false);
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
                CommitElapsedTime(true);

            isForeground = hasFocus;
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
                CommitElapsedTime(true);

            isForeground = !paused;
        }

        private void OnApplicationQuit()
        {
            CommitElapsedTime(true);
        }

        private void CommitElapsedTime(bool saveImmediately)
        {
            if (elapsedSeconds <= 0f)
                return;

            float secondsToCommit = elapsedSeconds;
            elapsedSeconds = 0f;
            if (QuestService.ReportOnlineSeconds(secondsToCommit) && saveImmediately)
                SaveController.Save(false, false);
        }
    }
}
