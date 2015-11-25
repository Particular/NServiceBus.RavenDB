using NServiceBus.Extensibility;
using NServiceBus.RavenDB.Tests;
using NServiceBus.SagaPersisters.RavenDB;
using Raven.Client;

static class RavenTestBaseForSagaPersistenceOptions
{
    public static ContextBag CreateContextWithAsyncSessionPresent(this RavenDBPersistenceTestBase testBase, out IAsyncDocumentSession session)
    {
        var context = new ContextBag();
        session = testBase.OpenAsyncSession();
        context.Set(session);
        return context;
    }
    public static RavenDBSynchronizedStorageSession CreateSynchronizedStorageSession(this RavenDBPersistenceTestBase testBase)
    {
        var session = testBase.OpenAsyncSession();
        var synchronizedSession = new RavenDBSynchronizedStorageSession(session, true);
        return synchronizedSession;
    }
}