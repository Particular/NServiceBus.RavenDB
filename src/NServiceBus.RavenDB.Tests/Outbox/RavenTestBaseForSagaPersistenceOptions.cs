using NServiceBus.RavenDB.Outbox;
using NServiceBus.RavenDB.Tests;

static class RavenTestBaseForOutboxStorageOptions
{
    public static RavenDBOutboxTransaction CreateTransaction(this RavenDBPersistenceTestBase testBase)
    {
        return new RavenDBOutboxTransaction(testBase.OpenSession());
    }
}