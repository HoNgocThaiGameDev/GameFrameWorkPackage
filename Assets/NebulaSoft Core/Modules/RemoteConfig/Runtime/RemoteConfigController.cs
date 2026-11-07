using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace NebulaSoft
{
    [StaticUnload]
    public static class RemoteConfigController
    {
        internal const int MaxPayloadBytes = 256 * 1024;

        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            ObjectCreationHandling = ObjectCreationHandling.Replace,
            TypeNameHandling = TypeNameHandling.None,
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            MaxDepth = 32
        };

        private static JObject configData;
        private static JObject bundledData;

        internal static void SetDefaults(string config)
        {
            // A new bootstrap must not retain overrides from a previous endpoint or session.
            configData = null;
            bundledData = TryParse(config, out var parsed) ? parsed : null;
        }

        public static void Shutdown() => UnloadStatic();

        public static void Init(string config)
        {
            if (!TryApply(config))
                Debug.LogWarning("[Remote Config] Invalid or empty payload. Keeping the active configuration.");
        }

        internal static bool TryApply(string config)
        {
            if (!TryParse(config, out var candidate)) return false;
            configData = candidate;
            return true;
        }

        private static bool TryParse(string config, out JObject candidate)
        {
            candidate = null;
            if (string.IsNullOrWhiteSpace(config) || Encoding.UTF8.GetByteCount(config) > MaxPayloadBytes)
                return false;

            try
            {
                using var reader = new JsonTextReader(new StringReader(config.TrimStart('\uFEFF')))
                {
                    DateParseHandling = DateParseHandling.None,
                    MaxDepth = SerializerSettings.MaxDepth
                };
                candidate = JObject.Load(reader, new JsonLoadSettings
                {
                    DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error
                });

                while (reader.Read())
                    if (reader.TokenType != JsonToken.Comment) return false;

                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        public static T TryGetConfig<T>(string key) where T : RemoteConfigData
        {
            return TryGetValue(key, out T value) ? value : null;
        }

        public static bool TryGetValue<T>(string key, out T value)
        {
            return TryReadValue(configData, key, out value) || TryReadValue(bundledData, key, out value);
        }

        private static bool TryReadValue<T>(JObject snapshot, string key, out T value)
        {
            value = default;
            if (string.IsNullOrEmpty(key) || snapshot == null ||
                !snapshot.TryGetValue(key, StringComparison.Ordinal, out JToken token) || token.Type == JTokenType.Null)
                return false;

            try
            {
                T candidate = token.ToObject<T>(JsonSerializer.Create(SerializerSettings));
                if (candidate is null) return false;
                if (candidate is RemoteConfigData typedConfig && !typedConfig.Validate())
                {
                    Debug.LogWarning($"[Remote Config] Validation failed for key '{key}'.");
                    return false;
                }

                value = candidate;
                return true;
            }
            catch (Exception)
            {
                // Legacy schema converters can also throw conversion or format exceptions.
                Debug.LogWarning($"[Remote Config] Failed to deserialize key '{key}'.");
                return false;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void UnloadStatic()
        {
            configData = null;
            bundledData = null;
        }
    }
}
