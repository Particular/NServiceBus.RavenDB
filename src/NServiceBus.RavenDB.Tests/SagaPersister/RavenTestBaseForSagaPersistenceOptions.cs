using NServiceBus.Extensibility;
using NServiceBus.RavenDB.Tests;
using Raven.Client.Documents.Session;

static class RavenTestBaseForSagaPersistenceOptions
{
    public static IAsyncDocumentSession CreateAsyncSessionInContext(this RavenDBPersistenceTestBase testBase, out ContextBag context)
    {
        context = new ContextBag();
        var session = testBase.OpenAsyncSession();
        context.Set(session);
        return session;
    }
}