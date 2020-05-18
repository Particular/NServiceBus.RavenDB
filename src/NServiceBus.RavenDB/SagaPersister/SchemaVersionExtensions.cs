namespace NServiceBus.Persistence.RavenDB
{
    using NServiceBus.RavenDB.Persistence.SagaPersister;
    using Raven.Client.Documents.Session;

    static partial class SchemaVersionExtensions
    {
        public static void StoreSchemaVersionInMetadata(this IAsyncDocumentSession session, SagaDataContainer entity)
        {
            var metadata = session.Advanced.GetMetadataFor(entity);
            metadata[SagaDataContainerSchemaVersionMetadataKey] = SagaDataContainer.SchemaVersion;
        }

        public static void StoreSchemaVersionInMetadata(this IAsyncDocumentSession session, SagaUniqueIdentity entity)
        {
            var metadata = session.Advanced.GetMetadataFor(entity);
            metadata[SagaUniqueIdentitySchemaVersionMetadataKey] = SagaUniqueIdentity.SchemaVersion;
        }

        internal const string SagaDataContainerSchemaVersionMetadataKey = MetadataKeyPrefix + "SagaDataContainer" + MetadataKeySchemaVersionSuffix;

        internal const string SagaUniqueIdentitySchemaVersionMetadataKey = MetadataKeyPrefix + "SagaUniqueIdentity" + MetadataKeySchemaVersionSuffix;
    }
}
