using System;
using System.Collections.Generic;
using System.Text;

namespace NebulaSoft
{
    /// <summary>SDK-independent cloud payload. Firestore adapters merge this map and validate versions in the same transaction.</summary>
    public static class SaveCloudData
    {
        public const int FormatVersion = 1;
        public const string Field = "frameworkSave";

        internal static string EncodeKey(string key) => Convert.ToBase64String(Encoding.UTF8.GetBytes(key)).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        public static void AddToUpdate(IDictionary<string, object> update, IDictionary<string, object> snapshot)
        {
            ValidateEnvelope(snapshot);
            // Firestore merge treats an empty map as replacement. Omit it when this client has no registered models.
            if (AsMap(snapshot["models"]).Count > 0) update[Field] = Clone(snapshot);
        }

        public static void ValidateWrite(IDictionary<string, object> existing, IDictionary<string, object> update)
        {
            if (existing == null) return;
            if (existing.TryGetValue("saveVersion", out var oldVersion) && update.TryGetValue("saveVersion", out var newVersion) &&
                Convert.ToInt32(oldVersion) > Convert.ToInt32(newVersion))
                throw new InvalidOperationException("Cloud progress was written by a newer client. Upload is blocked.");
            if (!existing.TryGetValue(Field, out var oldData) || !update.TryGetValue(Field, out var newData)) return;
            var oldEnvelope = AsMap(oldData);
            var newEnvelope = AsMap(newData);
            ValidateEnvelope(oldEnvelope);
            ValidateEnvelope(newEnvelope);
            var oldModels = AsMap(oldEnvelope["models"]);
            var newModels = AsMap(newEnvelope["models"]);
            if (newModels.Count == 0 && oldModels.Count > 0)
                throw new InvalidOperationException("An empty cloud update cannot replace existing models.");
            foreach (var item in newModels)
            {
                if (oldModels.TryGetValue(item.Key, out var previous) &&
                    Convert.ToInt32(AsMap(previous)["version"]) > Convert.ToInt32(AsMap(item.Value)["version"]))
                    throw new InvalidOperationException("A cloud model is newer than this client: " + item.Key);
            }
        }

        internal static IDictionary<string, object> AsMap(object value)
        {
            if (value is IDictionary<string, object> map) return map;
            if (value is Newtonsoft.Json.Linq.JObject json) return json.ToObject<Dictionary<string, object>>();
            throw new InvalidOperationException("Cloud save data must be an object.");
        }

        internal static void ValidateEnvelope(IDictionary<string, object> envelope)
        {
            if (envelope == null || !envelope.TryGetValue("version", out var version) || Convert.ToInt32(version) != FormatVersion ||
                !envelope.ContainsKey("models")) throw new InvalidOperationException("Unsupported cloud save format.");
        }

        public static Dictionary<string, object> Clone(IDictionary<string, object> source)
        {
            if (source == null) return null;
            var result = new Dictionary<string, object>(StringComparer.Ordinal);
            foreach (var pair in source)
                result.Add(pair.Key, pair.Value is IDictionary<string, object> nested ? Clone(nested) : pair.Value);
            return result;
        }

        public static bool HasConflict(IDictionary<string, object> local, IDictionary<string, object> remote)
        {
            ValidateEnvelope(local);
            ValidateEnvelope(remote);
            var left = AsMap(local["models"]);
            var right = AsMap(remote["models"]);
            if (left.Count != right.Count) return true;
            foreach (var pair in left)
            {
                if (!right.TryGetValue(pair.Key, out var other)) return true;
                var a = AsMap(pair.Value);
                var b = AsMap(other);
                if (Convert.ToInt32(a["version"]) != Convert.ToInt32(b["version"]) ||
                    !Newtonsoft.Json.Linq.JToken.DeepEquals(Newtonsoft.Json.Linq.JObject.Parse((string)a["json"]),
                        Newtonsoft.Json.Linq.JObject.Parse((string)b["json"]))) return true;
            }
            return false;
        }
    }
}
