namespace NServiceBus.Persistence.RavenDB
{
    using NServiceBus.RavenDB.Persistence.SubscriptionStorage;
    using Raven.Client.Documents.Session;

    static partial class SchemaVersionExtensions
    {
        public static void StoreSchemaVersionInMetadata(this IAsyncDocumentSession session, Subscription entity)
        {
            var metadata = session.Advanced.GetMetadataFor(entity);
            metadata[SubscriptionSchemaVersionMetadataKey] = Subscription.SchemaVersion;
        }

        internal const string SubscriptionSchemaVersionMetadataKey = MetadataKeyPrefix + "Subscription" + MetadataKeySchemaVersionSuffix;
    }
}