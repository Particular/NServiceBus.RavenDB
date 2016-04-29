namespace NServiceBus.Persistence.RavenDB
{
    using Raven.Client.Listeners;
    using Raven.Json.Linq;

    class TimeoutDataV1toV2Converter : IDocumentConversionListener
    {
        public void BeforeConversionToDocument(string key, object entity, RavenJObject metadata)
        {
        }

        public void AfterConversionToDocument(string key, object entity, RavenJObject document, RavenJObject metadata)
        {
            // we will not save the old format, so no conversion needed
        }

        public void BeforeConversionToEntity(string key, RavenJObject document, RavenJObject metadata)
        {
            if (!IsTimeoutData(metadata))
            {
                return;
            }

            document["Destination"] = LegacyAddress.ParseToString(() => document["Destination"]);
        }

        public void AfterConversionToEntity(string key, RavenJObject document, RavenJObject metadata, object entity)
        {
        }

        static bool IsTimeoutData(RavenJObject ravenJObject)
        {
            var clrMetaData = ravenJObject["Raven-Clr-Type"];
            //not all raven document types have 'Raven-Clr-Type' metadata
            if (clrMetaData == null)
            {
                return false;
            }
            var clrType = clrMetaData.Value<string>();
            return !string.IsNullOrEmpty(clrType) && 
                clrType == "NServiceBus.TimeoutPersisters.RavenDB.TimeoutData, NServiceBus.RavenDB";
        }
    }
}