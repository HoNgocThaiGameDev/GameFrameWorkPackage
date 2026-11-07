#pragma warning disable 0649

using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;

namespace NebulaSoft
{
    [DefaultExecutionOrder(-999)]
    public class Initializer : MonoBehaviour
    {
        private static Initializer initializer;

        [SerializeField] ProjectInitSettings initSettings;
        [SerializeField] SDKInitializer sdkInitializer;
        [SerializeField] SystemMessage systemMessage;
        [SerializeField] MusicSource globalMusicSource;
        [SerializeField] EventSystem eventSystem;

        public static GameObject GameObject { get; private set; }
        public static Transform Transform { get; private set; }

        public static ProjectInitSettings InitSettings { get; private set; }

        public void Init()
        {
            if (initializer != null) return;

            initializer = this;

            InitSettings = initSettings;

            GameObject = gameObject;
            Transform = transform;

#if MODULE_INPUT_SYSTEM
            eventSystem.gameObject.GetOrSetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            eventSystem.gameObject.GetOrSetComponent<StandaloneInputModule>();
#endif

            systemMessage.Init();

            Overlay.Bind(new Overlay(gameObject));

            AnalyticsModules.Init();

            DontDestroyOnLoad(gameObject);
        }

        public IEnumerator InitModulesCoroutine()
        {
            yield return initSettings.Prepare();
            if (initSettings.LastError != null) yield break;
            try { InitModules(); }
            catch (System.Exception) { /* ProjectInitSettings records the failed module for the loading UI. */ }
        }

        public void InitModules()
        {
            initSettings.Init(this);

            StaticModules.InitStaticModules();

            if (initializer == this && globalMusicSource != null)
            {
                globalMusicSource.Init();
                globalMusicSource.Activate();
            }
        }

        public void InitSDKs()
        {
            sdkInitializer.Init();
        }

        private void OnDestroy()
        {
            if (initializer != this) return;
            if (sdkInitializer != null) sdkInitializer.Shutdown();
            if (initSettings != null) initSettings.Shutdown();
            initializer = null;
            GameObject = null;
            Transform = null;
            InitSettings = null;
        }
    }
}
