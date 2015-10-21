namespace NServiceBus.RavenDB.Timeouts
{
    using NServiceBus.RavenDB.Internal;
    using Raven.Client.Listeners;
    using Raven.Json.Linq;

    class SubscriptionV1toV2Converter : IDocumentConversionListener
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
            if (!IsSubscription(metadata))
            {
                return;
            }

            document["Clients"] = LegacyAddress.ParseMultiple(() => (RavenJArray)document["Clients"]);
        }

        public void AfterConversionToEntity(string key, RavenJObject document, RavenJObject metadata, object entity)
        {
        }

        static bool IsSubscription(RavenJObject ravenJObject)
        {
            var clrType = ravenJObject["Raven-Clr-Type"].Value<string>();
            return !string.IsNullOrEmpty(clrType) && clrType == "NServiceBus.RavenDB.Persistence.SubscriptionStorage.Subscription, NServiceBus.RavenDB";
        }
    }
}