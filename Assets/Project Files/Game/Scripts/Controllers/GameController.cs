using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using NebulaSoft.Analytics;

namespace NebulaSoft
{
    /// <summary>Game scene controller.</summary>
    [StaticUnload]
    public sealed class GameController : MonoBehaviour
    {
        private static bool isGameActivated;
        private static bool isGameFinished;
        private static int rewardedRevives;
        private static LevelTracker analytics;
        private LevelTracker ownedAnalytics;
        public static bool IsGameActivated => isGameActivated;
        public static LevelTracker AnalyticsSession => analytics;
        public static event Action<int> Revived;

        private void Awake()
        {
            isGameActivated = false;
            isGameFinished = false;
            rewardedRevives = 0;
            analytics = ownedAnalytics = new LevelTracker(AnalyticsController.Events, () => Time.realtimeSinceStartupAsDouble);
            UIController ui = GetComponent<UIController>();
            ui?.Init();
            ui?.InitPages();
            AdsManager.EnableBanner();
        }

        private void Start()
        {
            UIController.ShowPage<UIGame>();
            Overlay.Hide(0f);
            ActivateGame();
        }

        public static void ActivateGame()
        {
            if (isGameActivated || isGameFinished) return;
            isGameActivated = true;
            analytics?.Start(ActiveSession.Current.DisplayLevelIndex + 1);
        }
        public static void GameComplete()
        {
            if (isGameFinished) return;
            isGameFinished = true;
            isGameActivated = false;
            analytics?.End(true, "completed");
            ActiveSession.Current.OnLevelCompleted();
            QuestService.ReportProgress(QuestGoalType.CompleteLevels);
        }
        public static void ShowCompleteUI() => UIController.ShowPage<UIComplete>();
        public static void GameOver(bool allowRevive, float uiDelay = 0f, bool resetWinStreakOnFinalFailure = false)
        {
            if (isGameFinished) return;
            isGameFinished = true;
            isGameActivated = false;
            allowRevive = allowRevive && CanOfferRewardedRevive();
            analytics?.Fail("game_over");
            if (!allowRevive) analytics?.End(false, "game_over");
            if (uiDelay > 0f) Tween.DelayedCall(uiDelay, () => UIGameOver.Show(allowRevive, resetWinStreakOnFinalFailure));
            else UIGameOver.Show(allowRevive, resetWinStreakOnFinalFailure);
        }
        internal static bool CanOfferRewardedRevive()
        {
            return (GameData.Data == null || GameData.Data.CanOfferRewardedRevive(rewardedRevives)) &&
                AdsManager.IsRewardBasedVideoLoaded();
        }

        internal static void ReviveFromRewardedAd()
        {
            rewardedRevives++;
            Revive(GameData.Data != null ? GameData.Data.ReviveExtraSeconds : 20);
        }

        public static void Revive(int seconds)
        {
            analytics?.Revive("continue");
            isGameFinished = false;
            isGameActivated = true;
            UIController.HidePage<UIGameOver>();
            Revived?.Invoke(seconds);
        }
        public static void OnLevelCompleted() { analytics?.End(true, "completed"); ActiveSession.Current.OnLevelCompleted(); }
        public static void OnLevelFailed() => analytics?.End(false, "failed");
        public static void Replay(SimpleCallback callback = null)
        {
            if (analytics != null && analytics.Level > 0) AnalyticsController.Events.LevelRetry(analytics.Level);
            analytics?.End(false, "retry");
            callback?.Invoke(); LoadScene(GameConsts.SCENE_GAME);
        }
        public static void OnCompleteRewardRecieved() => LoadScene(GameConsts.SCENE_GAME);
        public static void LoadMenu(SimpleCallback callback = null) { callback?.Invoke(); LoadScene(GameConsts.SCENE_MENU); }
        public static void Unload(SimpleCallback callback) { EndOpenAttempt(); callback?.Invoke(); }
        private static void LoadScene(string scene) { if (SceneUtils.DoesSceneExist(scene)) { EndOpenAttempt(); SceneManager.LoadScene(scene); } }
        private static void EndOpenAttempt() => analytics?.End(false, isGameFinished ? "failed" : "quit");
        private void OnApplicationPause(bool paused) => ownedAnalytics?.SetPaused(paused);
        private void OnApplicationQuit() => ownedAnalytics?.End(false, "quit");
        private void OnDestroy()
        {
            ownedAnalytics?.End(false, "quit");
            if (analytics == ownedAnalytics) analytics = null;
        }
        private static void UnloadStatic()
        {
            isGameActivated = false;
            isGameFinished = false;
            rewardedRevives = 0;
            analytics = null;
            Revived = null;
        }
    }
}
