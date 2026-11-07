using UnityEngine;
using UnityEditor;

namespace NebulaSoft
{
    public class EditorApplovinContainer : EditorAdsContainer
    {
        public EditorApplovinContainer(string containerName, string propertyName) : base(containerName, propertyName)
        {
        }

        protected override void SpecialButtons()
        {
            GUILayout.Space(8);

            EditorGUILayout.HelpBox("Tested with Applovin v8.1.0", MessageType.Info);
        }
    }
}