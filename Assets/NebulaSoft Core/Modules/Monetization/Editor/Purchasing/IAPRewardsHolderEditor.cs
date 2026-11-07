using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NebulaSoft
{
    [CustomEditor(typeof(IAPRewardsHolder), true)]
    public class IAPRewardsHolderEditor : RewardsHolderEditor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var settings = IAPSettingsResolver.Resolve(out string error);
            using (new EditorGUI.DisabledScope(true)) EditorGUILayout.ObjectField("IAP Settings", settings, typeof(IAPSettings), false);
            if (error != null) EditorGUILayout.HelpBox(error, MessageType.Error);
            var key = serializedObject.FindProperty("productKeyId");
            string current = ((IAPRewardsHolder)target).ProductKeyId;
            var labels = new List<string> { "Select product" };
            var keys = new List<string> { "" };
            if (settings != null && settings.StoreItems != null)
                foreach (var item in settings.StoreItems)
                {
                    if (item == null) continue;
                    labels.Add($"{item.DisplayName} ({item.Key})");
                    keys.Add(item.Key);
                }
            int selected = keys.IndexOf(current);
            if (selected < 0)
            {
                labels.Add("Missing: " + current);
                keys.Add(current);
                selected = keys.Count - 1;
                EditorGUILayout.HelpBox("The selected product is missing from this catalog.", MessageType.Warning);
            }
            EditorGUI.BeginChangeCheck();
            int next = EditorGUILayout.Popup("Product", selected, labels.ToArray());
            if (EditorGUI.EndChangeCheck())
            {
                key.stringValue = keys[next];
                serializedObject.FindProperty("useLegacyProductKey").boolValue = false;
                var selectedProduct = settings != null ? settings.FindByKey(keys[next]) : null;
                if (selectedProduct != null && selectedProduct.HasLegacyKey)
                    serializedObject.FindProperty("productKey").intValue = (int)selectedProduct.ProductKeyType;
                serializedObject.ApplyModifiedProperties();
                current = keys[next];
            }
            // Runtime uses the same catalog binding; this field is only its editor preview.
            var product = settings != null ? settings.FindByKey(current) : null;
            var rewards = product != null ? product.RewardsSet : null;
            if (rewardSetProp.objectReferenceValue != rewards)
            {
                rewardSetProp.objectReferenceValue = rewards;
                serializedObject.ApplyModifiedProperties();
                serializedObject.Update();
                RevalidateViewsAgainstRewardSet();
                if (rewards != null) AutoPopulateFromRewardSet();
            }
            RevalidateViewsAgainstRewardSet();
            if (serializedObject.FindProperty("purchaseButton").objectReferenceValue == null)
                EditorGUILayout.HelpBox("Assign the IAPButton used by this offer.", MessageType.Warning);
            base.OnInspectorGUI();
        }
        protected override bool DisableRewardsSetProperty() => true;
    }
}
