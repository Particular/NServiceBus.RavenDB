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
        // arrange
        var subscriber = new Subscriber("SomeTransportAddress", "SomeEndpoint");
        var storage = new SubscriptionPersister(store, UseClusterWideTransactions);

        // act
        await storage.Subscribe(subscriber, new MessageType("MessageType1", "1.0.0.0"), new ContextBag());
        await WaitForIndexing();

        // assert
        using (var session = store.OpenAsyncSession(GetSessionOptions()))
        {
            var subscription = await session
                .Query<Subscription>()
                .SingleOrDefaultAsync();

            var metadata = session.Advanced.GetMetadataFor(subscription);

            Assert.AreEqual(Subscription.SchemaVersion, metadata[SchemaVersionExtensions.SubscriptionSchemaVersionMetadataKey]);
        }
    }
}
