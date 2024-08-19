using System.Linq;
using System.Threading.Tasks;
using NServiceBus.Extensibility;
using NServiceBus.Persistence.RavenDB;
using NServiceBus.RavenDB.Tests;
using NServiceBus.Unicast.Subscriptions;
using NUnit.Framework;

[TestFixture]
public class When_listing_subscribers_for_message_types : RavenDBPersistenceTestBase
{
    [Test]
    public async Task The_names_of_all_subscribers_should_be_returned()
    {
        var storage = new SubscriptionPersister(store, UseClusterWideTransactions);
        var context = new ContextBag();

        await storage.Subscribe(TestClients.ClientA, MessageTypes.MessageA, context);
        await storage.Subscribe(TestClients.ClientA, MessageTypes.MessageB, context);
        await storage.Subscribe(TestClients.ClientB, MessageTypes.MessageA, context);
        await storage.Subscribe(TestClients.ClientA, MessageTypes.MessageAv2, context);

        var subscriptionsForMessageType = await storage.GetSubscriberAddressesForMessage(new[] { MessageTypes.MessageA }, context);

        Assert.That(subscriptionsForMessageType.Count(), Is.EqualTo(2));

        Assert.That(subscriptionsForMessageType.ElementAt(0).TransportAddress, Is.EqualTo(TestClients.ClientA.TransportAddress));
        Assert.That(subscriptionsForMessageType.ElementAt(0).Endpoint, Is.EqualTo(TestClients.ClientA.Endpoint));

        Assert.That(subscriptionsForMessageType.ElementAt(1).TransportAddress, Is.EqualTo(TestClients.ClientB.TransportAddress));
        Assert.That(subscriptionsForMessageType.ElementAt(1).Endpoint, Is.EqualTo(TestClients.ClientB.Endpoint));
    }

    [Test]
    public async Task Duplicates_should_not_be_generated_for_interface_inheritance_chains()
    {
        var storage = new SubscriptionPersister(store, UseClusterWideTransactions);
        var context = new ContextBag();

        await storage.Subscribe(TestClients.ClientA, new MessageType(typeof(ISomeInterface)), context);
        await storage.Subscribe(TestClients.ClientA, new MessageType(typeof(ISomeInterface2)), context);
        await storage.Subscribe(TestClients.ClientA, new MessageType(typeof(ISomeInterface3)), context);

        var subscriptionsForMessageType = await storage.GetSubscriberAddressesForMessage(new[]
                {
                    new MessageType(typeof(ISomeInterface)),
                    new MessageType(typeof(ISomeInterface2)),
                    new MessageType(typeof(ISomeInterface3))
                }, context);

        Assert.That(subscriptionsForMessageType.Count(), Is.EqualTo(1));
    }
}