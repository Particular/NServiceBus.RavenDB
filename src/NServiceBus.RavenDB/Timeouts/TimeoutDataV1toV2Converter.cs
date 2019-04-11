// TODO: No IDocumentConversionListener?

//namespace NServiceBus.Persistence.RavenDB
//{
//    using Newtonsoft.Json.Linq;

//    class TimeoutDataV1toV2Converter : IDocumentConversionListener
//    {
//        public void BeforeConversionToDocument(string key, object entity, JObject metadata)
//        {
//        }

//        public void AfterConversionToDocument(string key, object entity, JObject document, JObject metadata)
//        {
//            // we will not save the old format, so no conversion needed
//        }

//        public void BeforeConversionToEntity(string key, JObject document, JObject metadata)
//        {
//            if (!IsTimeoutData(metadata))
//            {
//                return;
//            }

//            document["Destination"] = LegacyAddress.ParseToString(() => document["Destination"]);
//        }

//        public void AfterConversionToEntity(string key, JObject document, JObject metadata, object entity)
//        {
//        }

//        static bool IsTimeoutData(JObject ravenJObject)
//        {
//            //metadata can be null when loading data using a transformers
//            var clrMetaData = ravenJObject?["Raven-Clr-Type"];
//            //not all raven document types have 'Raven-Clr-Type' metadata
//            if (clrMetaData == null)
//            {
//                return false;
//            }
//            var clrType = clrMetaData.Value<string>();
//            return !string.IsNullOrEmpty(clrType) &&
//                clrType == "NServiceBus.TimeoutPersisters.RavenDB.TimeoutData, NServiceBus.RavenDB";
//        }
//    }
//}