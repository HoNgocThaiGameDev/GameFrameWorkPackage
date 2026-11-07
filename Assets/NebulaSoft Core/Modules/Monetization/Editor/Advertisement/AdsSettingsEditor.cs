using UnityEngine;
using UnityEditor;

namespace NebulaSoft
{
    [CustomEditor(typeof(AdsSettings))]
    public class AdsSettingsEditor : CustomInspector
    {
        private readonly EditorAdsContainer[] adsContainers = new EditorAdsContainer[]
        {
            new EditorDummyContainer("Dummy", "dummyContainer"),
        };

        protected override void OnEnable()
        {
            base.OnEnable();

            for (int i = 0; i < adsContainers.Length; i++)
            {
                adsContainers[i].Init(serializedObject);
            }

            serializedObject.ApplyModifiedProperties();
        }

        public override void OnInspectorGUI()
        {
            if (serializedObject.FindProperty("moduleSettings").objectReferenceValue != null)
            {
                serializedObject.Update();
                EditorGUILayout.PropertyField(serializedObject.FindProperty("moduleSettings"));
                EditorGUILayout.HelpBox("AdsModuleSettings owns provider and placements. Legacy provider fields are ignored. Edit the linked module settings to configure Ads.", MessageType.Info);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("noAdsRewardSprite"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("loadAdsOnStart"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("interstitialFirstStartDelay"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("interstitialStartDelay"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("interstitialShowingDelay"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("loadingAdDuration"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("loadingMessage"));
                serializedObject.ApplyModifiedProperties();
                return;
            }
            base.OnInspectorGUI();

            serializedObject.Update();

            GUILayout.Space(8);

            for (int i = 0; i < adsContainers.Length; i++)
            {
                adsContainers[i].DrawContainer();
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
