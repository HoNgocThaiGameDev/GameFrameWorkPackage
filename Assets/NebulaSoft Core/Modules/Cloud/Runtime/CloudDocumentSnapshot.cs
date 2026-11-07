using System;
using System.Collections;
using System.Collections.Generic;

namespace NebulaSoft.Cloud
{
    /// <summary>Detached data. Adapters normalize timestamps to UTC DateTime and numbers to CLR primitives.</summary>
    public sealed class CloudDocumentSnapshot
    {
        private readonly Dictionary<string, object> data;
        public bool Exists { get; }
        public CloudDocumentSnapshot(bool exists, IDictionary<string, object> data = null)
        {
            Exists = exists;
            this.data = CopyMap(data);
        }
        public Dictionary<string, object> ToDictionary() => CopyMap(data);
        public bool TryGetValue<T>(string field, out T value)
        {
            if (data.TryGetValue(field, out var raw))
            {
                if (raw == null && default(T) is null) { value = default; return true; }
                if (raw is T typed) { value = (T)CopyValue(typed); return true; }
            }
            value = default;
            return false;
        }
        private static Dictionary<string, object> CopyMap(IDictionary<string, object> source)
        {
            var result = new Dictionary<string, object>();
            if (source != null) foreach (var pair in source) result.Add(pair.Key, CopyValue(pair.Value));
            return result;
        }
        private static object CopyValue(object value)
        {
            if (value is IDictionary<string, object> map) return CopyMap(map);
            if (value is byte[] bytes) return bytes.Clone();
            if (value is IList list)
            {
                var copy = new List<object>(list.Count);
                foreach (var item in list) copy.Add(CopyValue(item));
                return copy;
            }
            if (value == null || value is string || value is bool || value is DateTime || value is long ||
                value is int || value is double || value is float) return value;
            throw new CloudException(CloudError.InvalidData, "Unsupported cloud value type: " + value.GetType().FullName);
        }
    }
}
