using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace NebulaSoft
{
    [CustomEditor(typeof(IAPSettings))]
    public class IAPSettingsEditor : Editor
    {
        private ReorderableList products;
        private SerializedProperty items;
        private void OnEnable()
        {
            items = serializedObject.FindProperty("storeItems");
            products = new ReorderableList(serializedObject, items, true, true, true, true);
            products.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Products — drag to reorder");
            products.elementHeightCallback = index => (items.GetArrayElementAtIndex(index).isExpanded ? 9 : 1) * (EditorGUIUtility.singleLineHeight + 3) + 8;
            products.drawElementCallback = DrawProduct;
            products.onAddCallback = list => AddProduct(false);
        }
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("isEnabled"), new GUIContent("Enable IAP"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("useFakeStore"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fakeStoreMode"), new GUIContent("Legacy fake store UI", "Unity IAP 4 only. Unity IAP 5 test mode uses the framework Dummy adapter."));
            EditorGUILayout.HelpBox("Add products here, then select a product on IAPRewardsHolder. Keep keys stable after release. Store IDs must match your store catalog.", MessageType.Info);
            products.DoLayoutList();
            using (new EditorGUI.DisabledScope(products.index < 0 || products.index >= items.arraySize))
                if (GUILayout.Button("Duplicate selected product")) AddProduct(true);
            serializedObject.ApplyModifiedProperties();
            string platform = EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android ? "Android" : EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS ? "iOS" : null;
            foreach (var error in ((IAPSettings)target).ValidateCatalog(platform)) EditorGUILayout.HelpBox(error, MessageType.Error);
        }
        private void DrawProduct(Rect rect, int index, bool active, bool focused)
        {
            var item = items.GetArrayElementAtIndex(index);
            rect.y += 3;
            rect.height = EditorGUIUtility.singleLineHeight;
            string key = item.FindPropertyRelative("key").stringValue;
            if (string.IsNullOrWhiteSpace(key) && item.FindPropertyRelative("useLegacyKey").boolValue)
                key = IAPItem.GetLegacyKey((ProductKeyType)item.FindPropertyRelative("productKeyType").intValue);
            item.isExpanded = EditorGUI.Foldout(rect, item.isExpanded, string.IsNullOrWhiteSpace(key) ? "New product — configure key and store IDs" : key, true);
            if (!item.isExpanded) return;
            foreach (var field in new[] { "key", "displayName", "androidID", "iOSID", "productType", "defaultUSDPrice", "rewardsSet" })
            {
                rect.y += EditorGUIUtility.singleLineHeight + 3;
                EditorGUI.PropertyField(rect, item.FindPropertyRelative(field));
            }
            rect.y += EditorGUIUtility.singleLineHeight + 3;
            using (new EditorGUI.DisabledScope(true))
                EditorGUI.TextField(rect, "Legacy save mapping", item.FindPropertyRelative("useLegacyKey").boolValue ? ((ProductKeyType)item.FindPropertyRelative("productKeyType").intValue).ToString() : "None (key-based save)");
        }
        private void AddProduct(bool duplicate)
        {
            serializedObject.Update();
            int source = products.index;
            int index = items.arraySize;
            if (duplicate) items.InsertArrayElementAtIndex(source);
            else items.arraySize++;
            if (duplicate) index = source + 1;
            var item = items.GetArrayElementAtIndex(index);
            item.FindPropertyRelative("key").stringValue = "";
            item.FindPropertyRelative("androidID").stringValue = "";
            item.FindPropertyRelative("iOSID").stringValue = "";
            item.FindPropertyRelative("useLegacyKey").boolValue = false;
            item.FindPropertyRelative("productKeyType").intValue = 0;
            if (!duplicate)
            {
                item.FindPropertyRelative("displayName").stringValue = "";
                item.FindPropertyRelative("productType").intValue = 0;
                item.FindPropertyRelative("defaultUSDPrice").floatValue = 0.99f;
                item.FindPropertyRelative("rewardsSet").objectReferenceValue = null;
            }
            item.isExpanded = true;
            products.index = index;
            serializedObject.ApplyModifiedProperties();
        }
    }
}
