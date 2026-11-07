using UnityEditor;
using UnityEngine;

namespace NebulaSoft
{
    public static class LivesSystemEditorActions
    {
        [MenuItem("Actions/Lives System/Full Lives")]
        private static void FullLives()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Action works only in play mode!");

                return;
            }

            LivesSystem.AddLife(int.MaxValue, false);

            Debug.Log("FullLives action performed");
        }

        [MenuItem("Actions/Lives System/No Lives")]
        private static void NoLives()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Action works only in play mode!");

                return;
            }

            LivesSystem.TakeLife(int.MaxValue);

            Debug.Log("NoLives action performed");
        }

        [MenuItem("Actions/Lives System/Take Life")]
        private static void TakeLife()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Action works only in play mode!");

                return;
            }

            LivesSystem.TakeLife();

            Debug.Log("TakeLife action performed");
        }

        [MenuItem("Actions/Lives System/Add Life")]
        private static void AddLife()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Action works only in play mode!");

                return;
            }

            LivesSystem.AddLife();

            Debug.Log("AddLife action performed");
        }

        [MenuItem("Actions/Lives System/Show Add Life Panel")]
        private static void ShowAddLifePanel()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Action works only in play mode!");

                return;
            }

            if (!UIAddLivesPanel.Exists())
            {
                Debug.Log("UIAddLivesPanel page doesn't exist!");

                return;
            }

            UIAddLivesPanel.Show((bool rewardedVideoWatched) =>
            {
                Debug.Log("Panel Closed; RV watched: " + rewardedVideoWatched);
            });
        }

        [MenuItem("Actions/Lives System/Enable Infinite Mode (30 seconds)")]
        private static void EnableInfiniteMode()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Action works only in play mode!");

                return;
            }

            LivesSystem.EnableInfiniteMode(30);

            Debug.Log("EnableInfiniteMode action performed");
        }

        [MenuItem("Actions/Lives System/Disable Infinite Mode")]
        private static void DisableInfiniteMode()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Action works only in play mode!");

                return;
            }

            LivesSystem.DisableInfiniteMode();

            Debug.Log("DisableInfiniteMode action performed");
        }
    }
}
