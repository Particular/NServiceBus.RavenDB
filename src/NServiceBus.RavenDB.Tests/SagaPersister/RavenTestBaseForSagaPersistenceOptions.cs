using NServiceBus.Extensibility;
using NServiceBus.RavenDB.Tests;
using Raven.Client;

public static class RavenTestBaseForSagaPersistenceOptions
{
    public static ContextBag CreateContextWithSessionPresent(this RavenDBPersistenceTestBase testBase, out IDocumentSession session)
    {
        var context = new ContextBag();
        session = testBase.OpenSession();
        context.Set(session);
        return context;
    }
}