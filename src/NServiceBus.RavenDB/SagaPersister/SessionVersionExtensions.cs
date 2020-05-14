namespace NServiceBus.Persistence.RavenDB
{
    using NServiceBus.RavenDB.Persistence.SagaPersister;
    using Raven.Client.Documents.Session;

    static partial class SessionVersionExtensions
    {
        public static void StoreVersionInMetadata(this IAsyncDocumentSession session, SagaDataContainer entity)
        {
            var metadata = session.Advanced.GetMetadataFor(entity);
            metadata[SagaDataVersionMetadataKey] = SagaDataContainer.SchemaVersion.ToString(3);
        }

        public static void StoreVersionInMetadata(this IAsyncDocumentSession session, SagaUniqueIdentity entity)
        {
            var metadata = session.Advanced.GetMetadataFor(entity);
            metadata[SagaUniqueIdentityVersionMetadataKey] = SagaUniqueIdentity.SchemaVersion.ToString(3);
        }

        internal const string SagaDataVersionMetadataKey = Prefix + "Saga" + SchemaVersion;

        internal const string SagaUniqueIdentityVersionMetadataKey = Prefix + "SagaUniqueIdentity" + SchemaVersion;
    }
}