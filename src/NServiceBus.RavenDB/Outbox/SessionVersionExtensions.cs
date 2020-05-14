namespace NServiceBus.Persistence.RavenDB
{
    using NServiceBus.RavenDB.Outbox;
    using Raven.Client.Documents.Session;

    static partial class SessionVersionExtensions
    {
        public static void StoreVersionInMetadata(this IAsyncDocumentSession session, OutboxRecord entity)
        {
            var metadata = session.Advanced.GetMetadataFor(entity);
            metadata[OutboxRecordVersionMetadataKey] = OutboxRecord.SchemaVersion.ToString(3);
        }

        internal const string OutboxRecordVersionMetadataKey = Prefix + "Outbox" + SchemaVersion;
    }
}