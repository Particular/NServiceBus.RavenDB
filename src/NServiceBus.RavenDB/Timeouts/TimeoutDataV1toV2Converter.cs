namespace NServiceBus.RavenDB.Timeouts
{
    using NServiceBus.RavenDB.Internal;
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

            document["Destination"] = LegacyAddress.Parse(() => document["Destination"]);
        }

        public void AfterConversionToEntity(string key, RavenJObject document, RavenJObject metadata, object entity)
        {
        }

        static bool IsTimeoutData(RavenJObject ravenJObject)
        {
            var clrType = ravenJObject["Raven-Clr-Type"].Value<string>();
            return !string.IsNullOrEmpty(clrType) && clrType == "NServiceBus.TimeoutPersisters.RavenDB.TimeoutData, NServiceBus.RavenDB";
        }
    }
}