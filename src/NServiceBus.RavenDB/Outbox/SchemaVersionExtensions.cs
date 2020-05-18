namespace NServiceBus.Persistence.RavenDB
{
    using NServiceBus.RavenDB.Outbox;
    using Raven.Client.Documents.Session;

    static partial class SchemaVersionExtensions
    {
        public static void StoreSchemaVersionInMetadata(this IAsyncDocumentSession session, OutboxRecord entity)
        {
            var metadata = session.Advanced.GetMetadataFor(entity);
            metadata[OutboxRecordSchemaVersionMetadataKey] = OutboxRecord.SchemaVersion;
        }

        internal const string OutboxRecordSchemaVersionMetadataKey = MetadataKeyPrefix + "Outbox" + MetadataKeySchemaVersionSuffix;
    }
}
