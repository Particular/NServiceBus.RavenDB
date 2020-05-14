namespace NServiceBus.Persistence.RavenDB
{
    using NServiceBus.TimeoutPersisters.RavenDB;
    using Raven.Client.Documents.Session;

    static partial class SessionVersionExtensions
    {
        public static void StoreVersionInMetadata(this IAsyncDocumentSession session, TimeoutData entity)
        {
            var metadata = session.Advanced.GetMetadataFor(entity);
            metadata[TimeoutSchemaVersionMetadataKey] = TimeoutData.SchemaVersion.ToString(3);
        }

        internal const string TimeoutSchemaVersionMetadataKey = Prefix + "Timeout" + SchemaVersion;
    }
}