namespace NServiceBus.RavenDB.Outbox
{
    using NServiceBus.Outbox;
    using Raven.Client;

    static class OutboxStorageOptionsExtensions
    {
        public static IAsyncDocumentSession GetSession(this OutboxStorageOptions options)
        {
            return options.Context.Get<IAsyncDocumentSession>();
        }
    }
}