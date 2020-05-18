namespace NServiceBus.Persistence.RavenDB
{
    using NServiceBus.TimeoutPersisters.RavenDB;
    using Raven.Client.Documents.Session;

    static partial class SchemaVersionExtensions
    {
        public static void StoreSchemaVersionInMetadata(this IAsyncDocumentSession session, TimeoutData entity)
        {
            var metadata = session.Advanced.GetMetadataFor(entity);
            metadata[TimeoutDataSchemaVersionMetadataKey] = TimeoutData.SchemaVersion;
        }

        internal const string TimeoutDataSchemaVersionMetadataKey = MetadataKeyPrefix + "TimeoutData" + MetadataKeySchemaVersionSuffix;
    }
}
