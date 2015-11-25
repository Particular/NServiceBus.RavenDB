using NServiceBus.Extensibility;
using NServiceBus.RavenDB.Tests;
using Raven.Client;

public static class RavenTestBaseForSagaPersistenceOptions
{
    public static ContextBag CreateContextWithAsyncSessionPresent(this RavenDBPersistenceTestBase testBase, out IAsyncDocumentSession session)
    {
        var context = new ContextBag();
        session = testBase.OpenAsyncSession();
        context.Set(session);
        return context;
    }
}