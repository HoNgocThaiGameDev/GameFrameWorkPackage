using UnityEditor;
using NebulaSoft.Integrations.Firebase;

namespace NebulaSoft
{
    [CustomEditor(typeof(AnalyticsSettings))]
    public sealed class AnalyticsSettingsEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.HelpBox("Assign a provider config asset, automatic bridges and queue capacity. Editor uses Console unless a custom provider explicitly allows Editor execution.", MessageType.Info);
            var settings = (AnalyticsSettings)target;
            bool firebase = settings.ProviderConfig is FirebaseAnalyticsProviderConfig ||
                settings.ProviderConfig == null && settings.Provider == AnalyticsBackend.Firebase;
            if (!firebase) return;
            if (FirebaseBackendFactory.IsAnalyticsEnabled)
                EditorGUILayout.HelpBox("Firebase adapter enabled. Configure this game's Firebase app before testing an Android/iOS build.", MessageType.Info);
            else
                EditorGUILayout.HelpBox(FirebaseBackendFactory.AnalyticsSetupMessage, MessageType.Warning);
        }
    }
}
