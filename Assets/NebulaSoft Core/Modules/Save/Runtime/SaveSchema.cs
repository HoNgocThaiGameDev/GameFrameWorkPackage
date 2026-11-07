using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace NebulaSoft
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class SaveModelAttribute : Attribute
    {
        public string Id { get; }
        public int Version { get; }
        public bool CloudSync { get; set; }
        public SaveModelAttribute(string id, int version = 0)
        {
            if (string.IsNullOrWhiteSpace(id) || version < 0) throw new ArgumentException("A stable save ID and nonnegative version are required.");
            Id = id;
            Version = version;
        }
    }

    /// <summary>Register breaking changes before loading a model. Additive fields use the model's constructor defaults.</summary>
    public static class SaveSchema
    {
        private static readonly Dictionary<Type, Dictionary<int, Action<JObject>>> migrations = new Dictionary<Type, Dictionary<int, Action<JObject>>>();

        public static void RegisterMigration<T>(int fromVersion, Action<JObject> migrate) where T : ISaveObject
        {
            if (fromVersion < 0 || migrate == null) throw new ArgumentException("A migration requires its source version and transformation.");
            if (!migrations.TryGetValue(typeof(T), out var steps))
                migrations.Add(typeof(T), steps = new Dictionary<int, Action<JObject>>());
            if (steps.ContainsKey(fromVersion)) throw new InvalidOperationException("Migration is already registered for this model/version.");
            steps.Add(fromVersion, migrate);
        }

        internal static SaveModelAttribute Definition(Type type) =>
            (SaveModelAttribute)Attribute.GetCustomAttribute(type, typeof(SaveModelAttribute));

        internal static JObject Migrate(Type type, string json, int version)
        {
            int current = Definition(type)?.Version ?? 0;
            if (version > current) throw new InvalidOperationException("Save model is newer than this client: " + type.FullName);
            var payload = string.IsNullOrEmpty(json) ? new JObject() : JObject.Parse(json);
            while (version < current)
            {
                if (!migrations.TryGetValue(type, out var steps) || !steps.TryGetValue(version, out var step))
                    throw new InvalidOperationException("Missing migration for " + type.FullName + " from version " + version);
                step(payload);
                version++;
            }
            return payload;
        }

        internal static string MergeFields(string original, string updated)
        {
            var target = string.IsNullOrEmpty(original) ? new JObject() : JObject.Parse(original);
            return MergeValue(target, JObject.Parse(updated)).ToString(Newtonsoft.Json.Formatting.None);
        }

        private static JToken MergeValue(JToken original, JToken updated)
        {
            if (original is JObject oldObject && updated is JObject newObject)
            {
                foreach (var property in newObject.Properties())
                    oldObject[property.Name] = MergeValue(oldObject[property.Name], property.Value);
                return oldObject;
            }
            if (original is JArray oldArray && updated is JArray newArray)
            {
                var merged = new JArray();
                for (int i = 0; i < newArray.Count; i++)
                {
                    JToken previous = null;
                    if (newArray[i] is JObject item)
                    {
                        var identity = item.Properties().Where(property => property.Name == "id" || property.Name == "ID" ||
                            property.Name.EndsWith("Id", StringComparison.Ordinal) || property.Name.EndsWith("Key", StringComparison.Ordinal)).ToArray();
                        if (identity.Length > 0)
                        {
                            var matches = oldArray.OfType<JObject>().Where(old => identity.All(property =>
                                JToken.DeepEquals(old[property.Name], property.Value))).Take(2).ToArray();
                            if (matches.Length > 1) throw new InvalidOperationException("Collection IDs are ambiguous. Register a migration before saving this model.");
                            previous = matches.FirstOrDefault();
                        }
                        else if (oldArray.Count == newArray.Count) previous = oldArray[i];
                    }
                    merged.Add(MergeValue(previous, newArray[i]));
                }
                return merged;
            }
            return updated.DeepClone();
        }
    }
}
