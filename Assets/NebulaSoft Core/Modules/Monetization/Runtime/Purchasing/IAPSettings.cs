using System;
using System.Collections.Generic;
using UnityEngine;

namespace NebulaSoft
{
    [CreateAssetMenu(fileName = "IAP Settings", menuName = "NebulaSoft/IAP Settings")]
    public class IAPSettings : ScriptableObject
    {
        [SerializeField] bool isEnabled = true;
        public bool IsEnabled => isEnabled;
        [SerializeField] bool useFakeStore;
        public bool UseFakeStore => useFakeStore;
        [SerializeField] FakeStoreMode fakeStoreMode;
        public FakeStoreMode FakeStoreMode => fakeStoreMode;
        [SerializeField, Hide] IAPItem[] storeItems;
        public IAPItem[] StoreItems => storeItems;

        public IAPItem FindByKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key) || storeItems == null) return null;
            foreach (var item in storeItems)
                if (item != null && item.Key == key) return item;
            return null;
        }

        public IAPItem FindLegacy(ProductKeyType key)
        {
            if (storeItems == null) return null;
            foreach (var item in storeItems)
                if (item != null && item.HasLegacyKey && item.ProductKeyType == key) return item;
            return null;
        }

        public List<string> ValidateCatalog(string platform = null)
        {
            var errors = new List<string>();
            var keys = new HashSet<string>(StringComparer.Ordinal);
            var android = new HashSet<string>(StringComparer.Ordinal);
            var ios = new HashSet<string>(StringComparer.Ordinal);
            var legacy = new HashSet<ProductKeyType>();
            var saveKeys = new HashSet<string>(StringComparer.Ordinal);
            if (storeItems == null) return errors;
            for (int i = 0; i < storeItems.Length; i++)
            {
                var item = storeItems[i];
                if (item == null) { errors.Add($"Product {i + 1} is null."); continue; }
                if (string.IsNullOrWhiteSpace(item.Key) || item.Key != item.Key.Trim()) errors.Add($"Product {i + 1}: enter a key without surrounding whitespace.");
                else if (!keys.Add(item.Key)) errors.Add($"Duplicate key: {item.Key}");
                if (!saveKeys.Add(item.SaveKey)) errors.Add($"Duplicate save identity: {item.SaveKey}");
                if (item.HasLegacyKey && !legacy.Add(item.ProductKeyType)) errors.Add($"Duplicate legacy mapping: {item.ProductKeyType}");
                if (!string.IsNullOrWhiteSpace(item.AndroidID) && !android.Add(item.AndroidID)) errors.Add($"Duplicate Android ID: {item.AndroidID}");
                if (!string.IsNullOrWhiteSpace(item.IOSID) && !ios.Add(item.IOSID)) errors.Add($"Duplicate iOS ID: {item.IOSID}");
                if (string.IsNullOrWhiteSpace(item.AndroidID) && string.IsNullOrWhiteSpace(item.IOSID)) errors.Add($"{item.Key}: enter a store ID.");
                else if (platform == "Android" && string.IsNullOrWhiteSpace(item.AndroidID)) errors.Add($"{item.Key}: Android ID is missing.");
                else if (platform == "iOS" && string.IsNullOrWhiteSpace(item.IOSID)) errors.Add($"{item.Key}: iOS ID is missing.");
            }
            return errors;
        }
    }
}
