using NServiceBus.Extensibility;
using NServiceBus.Outbox;
using NServiceBus.RavenDB.Tests;
using Raven.Client;

public static class RavenTestBaseForOutboxStorageOptions
{
    public static OutboxStorageOptions NewOptions(this RavenDBPersistenceTestBase testBase, out IDocumentSession session)
    {
        var context = new ContextBag();
        session = testBase.OpenSession();
        context.Set(session);
        return new OutboxStorageOptions(context);
    }
}