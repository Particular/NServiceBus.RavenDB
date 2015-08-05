namespace NServiceBus.SagaPersisters.RavenDB
{
    using NServiceBus.Saga;
    using Raven.Client;

    static class SagaPersistenceOptionsExtensions
    {
        public static IDocumentSession GetSession(this SagaPersistenceOptions options)
        {
            return options.Context.Get<IDocumentSession>();
        }
    }
}