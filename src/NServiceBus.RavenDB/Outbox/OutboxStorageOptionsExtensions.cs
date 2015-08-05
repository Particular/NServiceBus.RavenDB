namespace NServiceBus.RavenDB.Outbox
{
    using NServiceBus.Outbox;
    using Raven.Client;

    static class OutboxStorageOptionsExtensions
    {
        public static IDocumentSession GetSession(this OutboxStorageOptions options)
        {
            return options.Context.Get<IDocumentSession>();
        }
    }
}