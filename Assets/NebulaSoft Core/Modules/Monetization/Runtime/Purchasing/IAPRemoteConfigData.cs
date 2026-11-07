using Newtonsoft.Json;
using System.Collections.Generic;

namespace NebulaSoft
{
    [System.Serializable]
    [JsonConverter(typeof(IAPContainerConverter))]
    public class IAPRemoteConfigData : RemoteConfigData
    {
        public override string Key => "iaps";
        public override bool PrettyPrint => true;

        [JsonProperty("price")]
        public List<Dictionary<string, float>> SerializedList => GenerateSerializedList();

        [JsonIgnore]
        public List<IAP> array = new()
        {
            new IAP { id = "com.product.test.id", price = 1.99f },
        };

        private List<Dictionary<string, float>> GenerateSerializedList()
        {
            List<Dictionary<string, float>> list = new List<Dictionary<string, float>>();
            foreach (IAP c in array)
            {
                list.Add(new Dictionary<string, float> { [c.id] = c.price });
            }

            return list;
        }

        public IAP GetOverride(string iapID)
        {
            if (array != null)
            {
                foreach (IAP iap in array)
                {
                    if (iap.id == iapID)
                    {
                        return iap;
                    }
                }
            }

            return null;
        }

        public class IAPContainerConverter : JsonConverter<IAPRemoteConfigData>
        {
            public override void WriteJson(JsonWriter writer, IAPRemoteConfigData value, JsonSerializer serializer)
            {
                writer.WriteStartObject();
                writer.WritePropertyName("price");
                writer.WriteStartArray();

                foreach (var currency in value.array)
                {
                    writer.WriteStartObject();
                    writer.WritePropertyName(currency.id);
                    writer.WriteValue(currency.price);
                    writer.WriteEndObject();
                }

                writer.WriteEndArray();
                writer.WriteEndObject();
            }

            public override IAPRemoteConfigData ReadJson(JsonReader reader, System.Type objectType, IAPRemoteConfigData existingValue, bool hasExistingValue, JsonSerializer serializer)
            {
                IAPRemoteConfigData container = new IAPRemoteConfigData();
                container.array.Clear();

                while (reader.Read())
                {
                    if (reader.TokenType == JsonToken.PropertyName && (string)reader.Value == "price")
                    {
                        reader.Read(); // Move to StartArray

                        while (reader.Read() && reader.TokenType != JsonToken.EndArray)
                        {
                            if (reader.TokenType == JsonToken.StartObject)
                            {
                                reader.Read(); // Move to PropertyName
                                string id = reader.Value.ToString();
                                reader.Read(); // Move to Value
                                float price = System.Convert.ToSingle(reader.Value);
                                reader.Read(); // Move to EndObject

                                container.array.Add(new IAP { id = id, price = price });
                            }
                        }
                    }

                    if (reader.TokenType == JsonToken.EndObject)
                        break;
                }

                return container;
            }
        }

        [System.Serializable]
        public class IAP
        {
            public string id;
            public float price;
        }
    }
}

