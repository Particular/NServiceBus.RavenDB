using System.Linq;
using NServiceBus.Extensibility;
using NServiceBus.RavenDB.Tests;
using NServiceBus.Unicast.Subscriptions;
using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;
using NServiceBus.Unicast.Subscriptions.RavenDB;
using NUnit.Framework;

[TestFixture]
public class When_listing_subscribers_for_message_types : RavenDBPersistenceTestBase
{
    [Test]
    public void The_names_of_all_subscribers_should_be_returned()
    {
        var storage = new SubscriptionPersister(store);
        var query = new QuerySubscriptions(store);
        var options = new SubscriptionStorageOptions(new ContextBag());

        storage.Subscribe(TestClients.ClientA, MessageTypes.MessageA, options);
        storage.Subscribe(TestClients.ClientA, MessageTypes.MessageB, options);
        storage.Subscribe(TestClients.ClientB, MessageTypes.MessageA, options);
        storage.Subscribe(TestClients.ClientA, MessageTypes.MessageAv2, options);

        var subscriptionsForMessageType = query.GetSubscriberAddressesForMessage(MessageTypes.MessageA);

        Assert.AreEqual(2, subscriptionsForMessageType.Count());
        Assert.AreEqual(TestClients.ClientA, subscriptionsForMessageType.First());
    }

    [Test]
    public void Duplicates_should_not_be_generated_for_interface_inheritance_chains()
    {
        var storage = new SubscriptionPersister(store);
        var query = new QuerySubscriptions(store);
        var options = new SubscriptionStorageOptions(new ContextBag());

        storage.Subscribe(TestClients.ClientA, new[]
                {
                    new MessageType(typeof(ISomeInterface))
                }, options);
        storage.Subscribe(TestClients.ClientA, new[]
                {
                    new MessageType(typeof(ISomeInterface2))
                }, options);
        storage.Subscribe(TestClients.ClientA, new[]
                {
                    new MessageType(typeof(ISomeInterface3))
                }, options);

        var subscriptionsForMessageType = query.GetSubscriberAddressesForMessage(new[]
                {
                    new MessageType(typeof(ISomeInterface)),
                    new MessageType(typeof(ISomeInterface2)),
                    new MessageType(typeof(ISomeInterface3))
                });

        Assert.AreEqual(1, subscriptionsForMessageType.Count());
    }
}