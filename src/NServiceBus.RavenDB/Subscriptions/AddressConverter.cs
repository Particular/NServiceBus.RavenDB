
namespace NServiceBus.RavenDB.Persistence.SubscriptionStorage
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using Raven.Imports.Newtonsoft.Json;

    class AddressConverter : JsonConverter
    {
        static Type ListOfAddressType;

        static AddressConverter()
        {
            var addresstype = Type.GetType("NServiceBus.Address, NServiceBus.Core");
            ListOfAddressType = typeof(List<>).MakeGenericType(addresstype);
        }

        public override bool CanConvert(Type objectType)
        {

            return (objectType == ListOfAddressType);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (objectType == ListOfAddressType)
            {
                var o = serializer.Deserialize(reader, ListOfAddressType);

                var list = o as IList;
                var result = new List<string>();

                if (list != null)
                {
                    result.AddRange(from object item in list select item.ToString());
                }

                return result;
            }

            return serializer.Deserialize<List<string>>(reader);
        }
    }
}