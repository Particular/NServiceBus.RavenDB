using System.Threading.Tasks;
using NServiceBus.Extensibility;
using NServiceBus.RavenDB.Persistence.SubscriptionStorage;
using NServiceBus.RavenDB.Tests;
using NServiceBus.Routing;
using NServiceBus.Unicast.Subscriptions;
using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;
using NServiceBus.Unicast.Subscriptions.RavenDB;
using NUnit.Framework;
using Raven.Client;

[TestFixture]
public class When_receiving_a_subscription_message : RavenDBPersistenceTestBase
{
    [Test]
    public async Task A_subscription_entry_should_be_added_to_the_database()
    {
        var clientEndpoint = new Subscriber("TestEndpoint", new Endpoint("TestEndpoint"));

        var messageTypes = new[]
        {
            new MessageType("MessageType1", "1.0.0.0"),
            new MessageType("MessageType2", "1.0.0.0")
        };

        var storage = new SubscriptionPersister(store);

        await storage.Subscribe(clientEndpoint, messageTypes, new ContextBag());

        using (var session = store.OpenAsyncSession())
        {
            var subscriptions = await session
                .Query<Subscription>()
                .Customize(c => c.WaitForNonStaleResults())
                .CountAsync();

            Assert.AreEqual(2, subscriptions);
        }
    }
}