using System.Threading.Tasks;
using NServiceBus.Extensibility;
using NServiceBus.Persistence.RavenDB;
using NServiceBus.RavenDB.Persistence.SubscriptionStorage;
using NServiceBus.RavenDB.Tests;
using NServiceBus.Unicast.Subscriptions;
using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;
using NUnit.Framework;
using Raven.Client.Documents;

[TestFixture]
public class When_storing_subscription : RavenDBPersistenceTestBase
{
    [Test]
    public async Task Should_store_schema_version()
    {
        var clientEndpoint = new Subscriber("TestEndpoint", "TestEndpoint");

        var storage = new SubscriptionPersister(store);

        await storage.Subscribe(clientEndpoint, new MessageType("MessageType1", "1.0.0.0"), new ContextBag());

        WaitForIndexing();

        using (var session = store.OpenAsyncSession())
        {
            var subscriptions = await session
                .Query<Subscription>()
                .SingleOrDefaultAsync();

            var metadata = session.Advanced.GetMetadataFor(subscriptions);

            Assert.AreEqual(Subscription.SchemaVersion.ToString(3), metadata[SessionVersionExtensions.SubscriptionVersionMetadataKey]);
        }
    }
}