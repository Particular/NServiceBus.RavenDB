using NServiceBus.Extensibility;
using NServiceBus.RavenDB.Tests;
using NServiceBus.SagaPersisters.RavenDB;
using Raven.Client;

static class RavenTestBaseForSagaPersistenceOptions
{
    public static ContextBag CreateContextWithAsyncSessionPresent(this RavenDBPersistenceTestBase testBase, out IDocumentSession session)
    {
        var context = new ContextBag();
        session = testBase.OpenSession();
        context.Set(session);
        return context;
    }
    public static RavenDBSynchronizedStorageSession CreateSynchronizedStorageSession(this RavenDBPersistenceTestBase testBase)
    {
        var session = testBase.OpenSession();
        var synchronizedSession = new RavenDBSynchronizedStorageSession(session, true);
        return synchronizedSession;
    }
}