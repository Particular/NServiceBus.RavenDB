using System.Linq;
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
public class When_receiving_duplicate_subscription_messages : RavenDBPersistenceTestBase
{
    [Test]
    public async Task Should_not_create_additional_db_rows()
    {
        var storage = new SubscriptionPersister(store, UseClusterWideTransactions)
        {
            DisableAggressiveCaching = true
        };

        await storage.Subscribe(new Subscriber("testEndPoint@localhost", "testEndPoint"), new MessageType("SomeMessageType", "1.0.0.0"), new ContextBag());

        await storage.Subscribe(new Subscriber("testEndPoint@localhost", "testEndPoint"), new MessageType("SomeMessageType", "1.0.0.0"), new ContextBag());

        using (var session = store.OpenAsyncSession(GetSessionOptions()))
        {
            var subscriptions = await session
                .Query<Subscription>()
                .Customize(c => c.WaitForNonStaleResults())
                .CountAsync();

            Assert.That(subscriptions, Is.EqualTo(1));
        }
    }


    [Test]
    public async Task Should_overwrite_existing_subscription()
    {
        const string subscriberAddress = "testEndPoint@localhost";
        var messageType = new MessageType("SomeMessageType", "1.0.0.0");
        var subscriber_v6 = new Subscriber(subscriberAddress, "endpoint_name");
        var subscriber_v6_2 = new Subscriber(subscriberAddress, "new_endpoint_name");

        var storage = new SubscriptionPersister(store, UseClusterWideTransactions)
        {
            DisableAggressiveCaching = true
        };
        await storage.Subscribe(subscriber_v6, messageType, new ContextBag());
        await storage.Subscribe(subscriber_v6_2, messageType, new ContextBag());

        var subscriber = (await storage.GetSubscriberAddressesForMessage(new[]
        {
            messageType
        }, new ContextBag())).ToArray();

        Assert.That(subscriber.Length, Is.EqualTo(1));
        Assert.That(subscriber[0].TransportAddress, Is.EqualTo(subscriberAddress));
        Assert.That(subscriber[0].Endpoint, Is.EqualTo("new_endpoint_name"));
    }
}