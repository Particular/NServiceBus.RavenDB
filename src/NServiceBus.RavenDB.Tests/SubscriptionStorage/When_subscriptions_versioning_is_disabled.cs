using System.Linq;
using System.Threading.Tasks;
using NServiceBus.Extensibility;
using NServiceBus.Persistence.RavenDB;
using NServiceBus.RavenDB.Tests;
using NServiceBus.Unicast.Subscriptions;
using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;
using NUnit.Framework;

[TestFixture]
public class When_subscriptions_versioning_is_disabled : RavenDBPersistenceTestBase
{
    [Test]
    public async Task Should_ignore_message_version()
    {
        var subscriberAddress_v1 = "v1@localhost";
        var subscriberAddress_v2 = "v2@localhost";
        var messageType_v1 = new MessageType("SomeMessageType", "1.0.0.0");
        var messageType_v2 = new MessageType("SomeMessageType", "2.0.0.0");
        var subscriber_v1 = new Subscriber(subscriberAddress_v1, "some_endpoint_name");
        var subscriber_v2 = new Subscriber(subscriberAddress_v2, "another_endpoint_name");

        var storage = new SubscriptionPersister(store, UseClusterWideTransactions)
        {
            DisableAggressiveCaching = true
        };

        await storage.Subscribe(subscriber_v1, messageType_v1, new ContextBag());
        await storage.Subscribe(subscriber_v2, messageType_v2, new ContextBag());

        var subscribers_looked_up_by_v1 = (await storage.GetSubscriberAddressesForMessage(new[]
        {
            messageType_v1
        }, new ContextBag())).ToArray();

        var subscribers_looked_up_by_v2 = (await storage.GetSubscriberAddressesForMessage(new[]
        {
            messageType_v2
        }, new ContextBag())).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(subscribers_looked_up_by_v1.Length, Is.EqualTo(2));
            Assert.That(subscribers_looked_up_by_v2.Length, Is.EqualTo(2));
        });
    }

    [Test]
    public async Task At_unsubscribe_time_should_ignore_message_version()
    {
        var subscriberAddress = "subscriber@localhost";
        var endpointName = "endpoint_name";
        var messageType_v1 = new MessageType("SomeMessageType", "1.0.0.0");
        var messageType_v2 = new MessageType("SomeMessageType", "2.0.0.0");
        var subscriber_v1 = new Subscriber(subscriberAddress, endpointName);
        var subscriber_v2 = new Subscriber(subscriberAddress, endpointName);

        var storage = new SubscriptionPersister(store, UseClusterWideTransactions)
        {
            DisableAggressiveCaching = true
        };

        await storage.Subscribe(subscriber_v1, messageType_v1, new ContextBag());

        var subscribers = (await storage.GetSubscriberAddressesForMessage(new[]
        {
            messageType_v1
        }, new ContextBag())).ToArray();

        Assert.That(subscribers.Length, Is.EqualTo(1));

        await storage.Unsubscribe(subscriber_v2, messageType_v2, new ContextBag());

        var subscribers_looked_up_by_v1 = (await storage.GetSubscriberAddressesForMessage(new[]
        {
            messageType_v1
        }, new ContextBag())).ToArray();

        Assert.That(subscribers_looked_up_by_v1.Length, Is.EqualTo(0));
    }
}