using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace NebulaSoft
{
    [System.Serializable]
    [JsonConverter(typeof(CurrencyContainerConverter))]
    public class CurrencyRemoteConfigData : RemoteConfigData
    {
        public override string Key => "currencies";
        public override bool PrettyPrint => true;

        public override bool Validate()
        {
            if (array == null || array.Count == 0) return false;
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var currency in array)
                if (currency == null || string.IsNullOrWhiteSpace(currency.name) || currency.defaultCount < 0 || !names.Add(currency.name))
                    return false;
            return true;
        }

        [JsonProperty("defaultCount")]
        public List<Dictionary<string, int>> SerializedList => GenerateSerializedList();

        [JsonIgnore]
        public List<Currency> array = new()
        {
            new Currency { name = "Coins", defaultCount = 0 },
        };

        private List<Dictionary<string, int>> GenerateSerializedList()
        {
            List<Dictionary<string, int>> list = new List<Dictionary<string, int>>();
            foreach (Currency c in array)
            {
                list.Add(new Dictionary<string, int> { [c.name] = c.defaultCount });
            }

            return list;
        }

        public Currency GetCurrencyOverride(CurrencyType currencyType)
            => GetCurrencyOverride(currencyType.ToString());

        public Currency GetCurrencyOverride(string currencyName)
        {
            if (array != null)
            {
                foreach (Currency currency in array)
                {
                    if (currency.name == currencyName && currency.defaultCount >= 0)
                    {
                        return currency;
                    }
                }
            }

            return null;
        }

        public class CurrencyContainerConverter : JsonConverter<CurrencyRemoteConfigData>
        {
            public override void WriteJson(JsonWriter writer, CurrencyRemoteConfigData value, JsonSerializer serializer)
            {
                writer.WriteStartObject();
                writer.WritePropertyName("defaultCount");
                writer.WriteStartArray();

                foreach (var currency in value.array)
                {
                    writer.WriteStartObject();
                    writer.WritePropertyName(currency.name);
                    writer.WriteValue(currency.defaultCount);
                    writer.WriteEndObject();
                }

                writer.WriteEndArray();
                writer.WriteEndObject();
            }

            public override CurrencyRemoteConfigData ReadJson(JsonReader reader, Type objectType, CurrencyRemoteConfigData existingValue, bool hasExistingValue, JsonSerializer serializer)
            {
                var payload = JObject.Load(reader);
                if (!(payload["defaultCount"] is JArray entries))
                    throw new JsonSerializationException("Currency config requires a defaultCount array.");
                var container = new CurrencyRemoteConfigData();
                container.array.Clear();
                foreach (var entry in entries)
                {
                    if (!(entry is JObject item) || item.Count != 1)
                        throw new JsonSerializationException("Each currency entry requires exactly one ID and integer balance.");
                    foreach (var property in item.Properties())
                    {
                        if (property.Value.Type != JTokenType.Integer)
                            throw new JsonSerializationException("Currency balances must be integers.");
                        container.array.Add(new Currency { name = property.Name, defaultCount = property.Value.Value<int>() });
                    }
                }

                return container;
            }
        }

        [System.Serializable]
        public class Currency
        {
            public string name;
            public int defaultCount;
        }
    }
}
