namespace NServiceBus.RavenDB.Persistence.SubscriptionStorage
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
        }

        public void AfterConversionToEntity(string key, RavenJObject document, RavenJObject metadata, object entity)
        {
            var subscription = entity as Subscription;

            if (subscription == null)
            {
                return;
            }

            var clients = document["Clients"];

            if (clients != null)
            {
                subscription.Subscribers = LegacyAddress.ParseMultipleToSubscriptionClient((RavenJArray)document["Clients"]);
            }
        }
    }
}