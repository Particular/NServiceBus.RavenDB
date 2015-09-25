namespace NServiceBus.SagaPersisters.RavenDB
{
    using NServiceBus.Sagas;
    using Raven.Client;

    static class SagaPersistenceOptionsExtensions
    {
        public static IAsyncDocumentSession GetSession(this SagaPersistenceOptions options)
        {
            return options.Context.Get<IAsyncDocumentSession>();
        }
    }
}