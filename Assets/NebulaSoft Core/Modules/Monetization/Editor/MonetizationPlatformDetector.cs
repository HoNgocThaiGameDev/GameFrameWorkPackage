using System;
using UnityEditor;
using UnityEngine;

namespace NebulaSoft
{
    [InitializeOnLoad]
    public static class MonetizationPlatformDetector
    {
        private const string PREFS_KEY = "MonetizationModuleInitialized";
        private const string PREFS_TIMER_KEY = "MonetizationModuleTimer";

        private static readonly BuildTarget[] SUPPORTED_BUILT_TARGETS = new BuildTarget[]
        {
            BuildTarget.Android,
            BuildTarget.iOS,
            BuildTarget.WebGL
        };

        static MonetizationPlatformDetector()
        {
            Init();
        }

        private static void Init()
        {
            if (Application.isBatchMode) return;

            BuildTarget activeBuildTarget = EditorUserBuildSettings.activeBuildTarget;
            foreach (BuildTarget target in SUPPORTED_BUILT_TARGETS)
                if (activeBuildTarget == target) return;

            MonetizationSettings monetizationSettings = EditorUtils.GetAsset<MonetizationSettings>();
            if (monetizationSettings == null || !monetizationSettings.IsModuleActive) return;

            State currentState = (State)PlayerPrefs.GetInt(PREFS_KEY, 0);

            if (currentState == State.Ignore) return;

            DateTime remindLaterTime = DateTime.Parse(PlayerPrefs.GetString(PREFS_TIMER_KEY, DateTime.MinValue.ToString()));
            if (currentState == State.RemindLater && DateTime.Now < remindLaterTime) return;

            int option = EditorUtility.DisplayDialogComplex(
                "Monetization Module Alert",
                "The monetization module is only supported on Android and iOS platforms. Please adjust your build settings accordingly.",
                "Disable Module",
                "Keep Enabled",
                "Remind Me Later"
            );

            switch (option)
            {
                case 0: // Disable
                    DisableMonetizationModule();
                    break;
                case 1: // Leave
                    IgnoreMonetizationSettings();
                    break;
                case 2: // Remind me later
                    DelayPopup(120);
                    break;
            }
        }

        private static void DelayPopup(int minutes)
        {
            PlayerPrefs.SetInt(PREFS_KEY, (int)State.RemindLater);
            PlayerPrefs.SetString(PREFS_TIMER_KEY, DateTime.Now.AddMinutes(minutes).ToString());
        }

        private static void DisableMonetizationModule()
        {
            MonetizationSettings monetizationSettings = EditorUtils.GetAsset<MonetizationSettings>();
            if (monetizationSettings == null) return;

            SerializedObject serializedObject = new SerializedObject(monetizationSettings);
            serializedObject.Update();
            serializedObject.FindProperty("isModuleActive").boolValue = false;
            serializedObject.ApplyModifiedProperties();

            Monetization.UpdateData(monetizationSettings);
        }

        private static void IgnoreMonetizationSettings()
        {
            PlayerPrefs.SetInt(PREFS_KEY, (int)State.Ignore);
        }

        private enum State
        {
            Undefined, Ignore, RemindLater
        }
    }
}