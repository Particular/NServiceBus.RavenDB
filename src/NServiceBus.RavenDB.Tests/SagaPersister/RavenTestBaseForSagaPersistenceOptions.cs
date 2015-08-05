using NServiceBus.Extensibility;
using NServiceBus.RavenDB.Tests;
using NServiceBus.Saga;
using Raven.Client;

public static class RavenTestBaseForSagaPersistenceOptions
{
    public static SagaPersistenceOptions NewSagaPersistenceOptions<TSaga>(this RavenDBPersistenceTestBase testBase, out IDocumentSession session) where TSaga : Saga
    {
        var context = new ContextBag();
        session = testBase.OpenSession();
        context.Set(session);
        return new SagaPersistenceOptions(SagaMetadata.Create(typeof(TSaga)), context);
    }
}