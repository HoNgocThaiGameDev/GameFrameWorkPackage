using UnityEditor;

namespace NebulaSoft
{
    [CustomEditor(typeof(AnalyticsSettings))]
    public sealed class AnalyticsSettingsEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.HelpBox("Editor sessions use Console. Add Analytics to Project Init Settings and assign these settings. Gameplay calls AnalyticsController.Events.", MessageType.Info);
            if (((AnalyticsSettings)target).Provider != AnalyticsBackend.Firebase) return;
#if FIREBASE_ANALYTICS
            EditorGUILayout.HelpBox("Firebase adapter enabled. Configure this game's Firebase app before testing an Android/iOS build.", MessageType.Info);
#else
            EditorGUILayout.HelpBox("Import FirebaseAnalytics.unitypackage and enable FIREBASE_ANALYTICS for Android/iOS to use Firebase in a player build. FIREBASE alone is not sufficient.", MessageType.Warning);
#endif
        }
    }
}
