using System.Collections.Generic;
using IngameDebugConsole;
using UnityEngine;

namespace NebulaSoft
{
    internal static class IngameDebugConsoleStartupBuffer
    {
        private struct BufferedLog
        {
            public string Message;
            public string StackTrace;
            public LogType Type;
        }

        private static readonly object SyncRoot = new object();
        private static readonly List<BufferedLog> Logs = new List<BufferedLog>(128);
        private static bool isSubscribed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            lock (SyncRoot)
            {
                Logs.Clear();
            }

            if (!isSubscribed)
            {
                Application.logMessageReceivedThreaded += CaptureLog;
                isSubscribed = true;
            }

            GameObject runnerObject = new GameObject("[IngameDebugConsole Startup Buffer]");
            Object.DontDestroyOnLoad(runnerObject);
            runnerObject.AddComponent<IngameDebugConsoleStartupBufferRunner>();
        }

        private static void CaptureLog(string message, string stackTrace, LogType type)
        {
            // Once the console exists, it receives new logs through its own callback.
            if (DebugLogManager.Instance != null)
                return;

            lock (SyncRoot)
            {
                Logs.Add(new BufferedLog
                {
                    Message = message,
                    StackTrace = stackTrace,
                    Type = type
                });
            }
        }

        internal static bool TryFlush()
        {
            DebugLogManager manager = DebugLogManager.Instance;
            if (manager == null)
                return false;

            if (isSubscribed)
            {
                Application.logMessageReceivedThreaded -= CaptureLog;
                isSubscribed = false;
            }

            BufferedLog[] bufferedLogs;
            lock (SyncRoot)
            {
                bufferedLogs = Logs.ToArray();
                Logs.Clear();
            }

            for (int i = 0; i < bufferedLogs.Length; i++)
            {
                BufferedLog log = bufferedLogs[i];
                manager.ReceivedLog(log.Message, log.StackTrace, log.Type);
            }

            return true;
        }
    }

    internal sealed class IngameDebugConsoleStartupBufferRunner : MonoBehaviour
    {
        private void Update()
        {
            if (IngameDebugConsoleStartupBuffer.TryFlush())
                Destroy(gameObject);
        }
    }
}
