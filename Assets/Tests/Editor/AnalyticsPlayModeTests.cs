using System.Collections;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

namespace NebulaSoft.Tests
{
    public sealed class AnalyticsPlayModeTests
    {
        private SceneSetup[] previousScenes;

        [UnityTest]
        public IEnumerator RegisteredSettings_FlushStartupQueueAndDestroyDisposesTheModule()
        {
            previousScenes = EditorSceneManager.GetSceneManagerSetup();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            yield return new EnterPlayMode();
            Assert.That(Application.isPlaying, Is.True);
            Application.runInBackground = true;
            EditorApplication.isPaused = false;

            AnalyticsController.Shutdown();
            var registry = AssetDatabase.LoadAssetAtPath<ProjectInitSettings>("Assets/Project Files/Data/Project Init Settings.asset");
            var registration = registry.GetModule<AnalyticsInitModule>();
            var service = AnalyticsController.Service;
            AnalyticsController.Events.LevelStart(7);
            Assert.That(service.PendingCount, Is.EqualTo(1));

            var owner = new GameObject("Analytics runtime test");
            LogAssert.Expect(LogType.Log, new Regex(@"\[Analytics\] level_start level=7 "));
            var initializerObject = typeof(Initializer).GetProperty(nameof(Initializer.GameObject));
            var previousOwner = initializerObject.GetValue(null);
            try
            {
                initializerObject.SetValue(null, owner);
                registration.CreateComponent();
                registration.CreateComponent();
            }
            finally { initializerObject.SetValue(null, previousOwner); }
            Assert.That(owner.GetComponents<AnalyticsModule>().Length, Is.EqualTo(1));
            Assert.That(service.IsReady, Is.True);
            Assert.That(service.PendingCount, Is.Zero);
            EditorApplication.QueuePlayerLoopUpdate();
            yield return null;

            LogAssert.Expect(LogType.Log, new Regex(@"\[Analytics\] iap_failed item_id=starter_pack reason=UserCancelled"));
            IAPManager.OnPurchaseFailed("starter_pack", PurchaseFailureReason.UserCancelled);
            Object.Destroy(owner);
            double destroyDeadline = EditorApplication.timeSinceStartup + 3;
            while (owner != null && EditorApplication.timeSinceStartup < destroyDeadline)
            {
                EditorApplication.QueuePlayerLoopUpdate();
                yield return null;
            }
            Assert.That(owner == null, Is.True, "Unity must finish its deferred Destroy before checking cleanup.");
            Assert.That(service.IsReady, Is.False);
            Assert.That(service.Track(new Analytics.AnalyticsEvent("after_destroy")), Is.False);
            Assert.That(AnalyticsController.Service, Is.Not.SameAs(service));
            yield return new ExitPlayMode();
        }

        [UnityTearDown]
        public IEnumerator RestoreEditor()
        {
            if (Application.isPlaying) yield return new ExitPlayMode();
            AnalyticsController.Shutdown();
            if (previousScenes != null && previousScenes.Length > 0)
                EditorSceneManager.RestoreSceneManagerSetup(previousScenes);
        }
    }
}
