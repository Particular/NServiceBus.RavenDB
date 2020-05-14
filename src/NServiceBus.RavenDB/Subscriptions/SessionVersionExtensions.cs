namespace NServiceBus.Persistence.RavenDB
{
    using NServiceBus.RavenDB.Persistence.SubscriptionStorage;
    using Raven.Client.Documents.Session;

    static partial class SessionVersionExtensions
    {
        public static void StoreVersionInMetadata(this IAsyncDocumentSession session, Subscription entity)
        {
            var metadata = session.Advanced.GetMetadataFor(entity);
            metadata[SubscriptionVersionMetadataKey] = Subscription.SchemaVersion.ToString(3);
        }

        internal const string SubscriptionVersionMetadataKey = Prefix + "Subscription" + SchemaVersion;
    }
}