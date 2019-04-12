namespace NServiceBus.Persistence.RavenDB
{
    using System.Linq;
    using NServiceBus.RavenDB.Persistence.SubscriptionStorage;
    using Raven.Client.Documents;
    using Raven.Client.Documents.Session;

    static class SubscriptionV1toV2Converter
    {
        public static void Register(DocumentStore store)
        {
            store.OnBeforeConversionToDocument += OnBeforeConversionToDocument;
            store.OnAfterConversionToEntity += OnAfterConversionToEntity;
        }

        static void OnBeforeConversionToDocument(object sender, BeforeConversionToDocumentEventArgs args)
        {
            if (args.Entity is Subscription subscription)
            {
                var converted = LegacyAddress.ConvertMultipleToLegacyAddress(subscription.Subscribers);
                subscription.LegacySubscriptions.Clear();
                subscription.LegacySubscriptions.AddRange(converted);
            }
        }

        static void OnAfterConversionToEntity(object sender, AfterConversionToEntityEventArgs args)
        {
            if (args.Entity is Subscription subscription)
            {
                var clients = args.Document["Clients"];

                if (clients != null)
                {
                    // TODO: Ensure when Clients are in the document that it's being cast correctly and enters here
                    var converted = LegacyAddress.ParseMultipleToSubscriptionClient(subscription.LegacySubscriptions);

                    var legacySubscriptions = converted.Except(subscription.Subscribers).ToArray();
                    foreach (var legacySubscription in legacySubscriptions)
                    {
                        subscription.Subscribers.Add(legacySubscription);
                    }
                }
            }
        }
    }
}