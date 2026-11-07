using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System;

namespace NebulaSoft
{
    [StaticUnload]
    public class GameLoading : MonoBehaviour
    {
        private const float MINIMUM_LOADING_TIME = 2.0f;

        private static GameLoading gameLoading;

        [SerializeField] Initializer initializer;
        [SerializeField] LoadingGraphics loadingGraphics;

        [Space]
        [Tooltip("If manual mode is enabled, the loading screen will be active until GameLoading.MarkAsReadyToHide method has been called.")]
        [SerializeField] bool useManualControl;
        [SerializeField] bool checkNetworkConnection = true;

        private static AsyncOperation loadingOperation;

        private static bool isReadyToHide;

        private static string loadingMessage;
        private static List<LoadingTask> loadingTasks = new List<LoadingTask>();

        private Coroutine initCoroutine;

        public static int LoadingSceneBuildIndex = -1;

        private void Awake()
        {
            gameLoading = this;

            DontDestroyOnLoad(gameObject);

            loadingGraphics.Init(this);

            initCoroutine = StartCoroutine(BootstrapCoroutine());
        }

        private IEnumerator BootstrapCoroutine()
        {
            yield return null;
            yield return new WaitForEndOfFrame();

            initializer.Init();

            yield return ConnectionCheckCoroutine();
        }

        public void RetryConnection()
        {
            if(initCoroutine == null)
            {
                initCoroutine = StartCoroutine(ConnectionCheckCoroutine());
            }
        }

        private IEnumerator ConnectionCheckCoroutine()
        {
            loadingGraphics.HideErrorMessage();
            loadingGraphics.SetLoadingState(0.0f, "Checking connection..");

            if(checkNetworkConnection)
            {
                bool isConnected = false;

                NetworkConnection networkConnection = new NetworkConnection(NetworkConnection.ServiceProbeUrl);
                IEnumerator connectionCheck = networkConnection.CheckConnection((state) => isConnected = state);

                yield return connectionCheck;

                if (!isConnected)
                {
                    loadingGraphics.ShowErrorMessage("Connection error");

                    initCoroutine = null;

                    yield break;
                }
            }

            loadingGraphics.SetLoadingState(0.1f, "Loading..");

            yield return initializer.InitModulesCoroutine();
            if (Initializer.InitSettings.LastError != null)
            {
                loadingGraphics.ShowErrorMessage(Initializer.InitSettings.LastError);
                initCoroutine = null;
                yield break;
            }
            loadingTasks.Clear();
            string sdkError = null;
            try { initializer.InitSDKs(); }
            catch (Exception exception) { sdkError = exception.Message; }
            if (sdkError != null)
            {
                loadingGraphics.ShowErrorMessage("SDK initialization failed: " + sdkError);
                initCoroutine = null;
                yield break;
            }

            int taskIndex = 0;
            while (taskIndex < loadingTasks.Count)
            {
                if (!loadingTasks[taskIndex].IsActive)
                    loadingTasks[taskIndex].Activate();
                loadingTasks[taskIndex].CheckTimeout(Time.realtimeSinceStartupAsDouble);

                if (loadingTasks[taskIndex].IsFinished)
                {
                    var task = loadingTasks[taskIndex];
                    if (task.IsRequired && (task.Status == LoadingTask.CompleteStatus.Failed || task.Status == LoadingTask.CompleteStatus.TimedOut))
                    {
                        loadingGraphics.ShowErrorMessage("Startup task failed: " + task.GetType().Name);
                        initCoroutine = null;
                        yield break;
                    }
                    taskIndex++;
                }

                yield return null;
            }

            yield return null;
            yield return null;
            yield return null;

            float realtimeSinceStartup = Time.realtimeSinceStartup;

            int sceneIndex = LoadingSceneBuildIndex;
            if(sceneIndex == -1)
            {
                sceneIndex = SceneManager.GetActiveScene().buildIndex + 1;
            }
            if (sceneIndex < 0 || sceneIndex >= SceneManager.sceneCountInBuildSettings)
            {
                loadingGraphics.ShowErrorMessage("Startup scene is missing from Build Settings.");
                initCoroutine = null;
                yield break;
            }

            float minimumFinishTime = realtimeSinceStartup + MINIMUM_LOADING_TIME;

            loadingOperation = SceneManager.LoadSceneAsync(sceneIndex);

            yield return null;

            loadingMessage = "Loading..";

            while (!loadingOperation.isDone || realtimeSinceStartup < minimumFinishTime)
            {
                yield return null;

                realtimeSinceStartup = Time.realtimeSinceStartup;

                loadingGraphics.SetLoadingState(Mathf.Lerp(0.2f, 0.9f, loadingOperation.progress), loadingMessage);
            }

            loadingGraphics.SetLoadingState(1.0f, "Completed");

            if (useManualControl)
            {
                // Debug check if MarkAsReadyToHide is implemented
                Tween.DelayedCall(10, () =>
                {
                    if (!isReadyToHide)
                        Debug.LogError("[Loading]: Seems like you forget to call MarkAsReadyToHide method to finish the loading process.");
                });

                while (!isReadyToHide)
                {
                    yield return null;
                }
            }

            loadingGraphics.OnLoadingFinished();

            Destroy(gameObject);
        }

        public static void SetLoadingMessage(string message)
        {
            loadingMessage = message;

            float progress = 0.0f;
            if (loadingOperation != null)
                progress = loadingOperation.progress;
        }

        public static void AddTask(LoadingTask loadingTask)
        {
            if (loadingTask != null && !loadingTasks.Contains(loadingTask)) loadingTasks.Add(loadingTask);
        }

        public static void MarkAsReadyToHide()
        {
            isReadyToHide = true;
        }

        private static void UnloadStatic()
        {
            isReadyToHide = false;
            loadingTasks.Clear();
        }

        public delegate void LoadingCallback(float state, string message);
    }
}
