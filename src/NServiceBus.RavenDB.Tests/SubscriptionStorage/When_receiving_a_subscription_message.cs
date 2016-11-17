using System.Threading.Tasks;
using NServiceBus.Extensibility;
using NServiceBus.Persistence.RavenDB;
using NServiceBus.RavenDB.Persistence.SubscriptionStorage;
using NServiceBus.RavenDB.Tests;
using NServiceBus.Unicast.Subscriptions;
using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;
using NUnit.Framework;
using Raven.Client;
using System;
using System.Linq;

[TestFixture]
public class When_receiving_a_subscription_message : RavenDBPersistenceTestBase
{
    public override void SetUp()
    {
        base.SetUp();
        SubscriptionIndex.Create(store);
    }

    [Test]
    public async Task A_subscription_entry_should_be_added_to_the_database()
    {
        var clientEndpoint = new Subscriber("TestEndpoint", "TestEndpoint");

        var storage = new SubscriptionPersister(store);

        await storage.Subscribe(clientEndpoint, new MessageType("MessageType1", "1.0.0.0"), new ContextBag());

        using (var session = store.OpenAsyncSession())
        {
            var subscriptions = await session
                .Query<Subscription>()
                .Customize(c => c.WaitForNonStaleResults())
                .CountAsync();

            Assert.AreEqual(1, subscriptions);
        }
    }

    [Test]
    public async Task Versioned_subscription_should_update_all_documents()
    {
        var storage = new SubscriptionPersister(store);
        var context = new ContextBag();

        var sub1Id = Guid.NewGuid();
        var sub2Id = Guid.NewGuid();

        await CreateSeedSubscription(MessageTypes.MessageA, sub1Id, TestClients.ClientA, TestClients.ClientB);
        await CreateSeedSubscription(MessageTypes.MessageAv11, sub2Id, TestClients.ClientC);

        await storage.Subscribe(TestClients.ClientD, MessageTypes.MessageAv2, context);

        WaitForIndexing(store);

        var msgAClients = await storage.GetSubscriberAddressesForMessage(new[] { MessageTypes.MessageA }, context);
        var msgAV11Clients = await storage.GetSubscriberAddressesForMessage(new[] { MessageTypes.MessageAv11 }, context);
        var msgAV2Clients = await storage.GetSubscriberAddressesForMessage(new[] { MessageTypes.MessageAv2 }, context);

        Subscription sub1, sub2;
        using (var session = store.OpenAsyncSession())
        {
            sub1 = await session.LoadAsync<Subscription>($"Subscriptions/{sub1Id}");
            sub2 = await session.LoadAsync<Subscription>($"Subscriptions/{sub2Id}");
        }

        Assert.AreEqual(4, msgAClients.Count());
        Assert.AreEqual(4, msgAV11Clients.Count());
        Assert.AreEqual(4, msgAV2Clients.Count());
        Assert.IsNotNull(sub1);
        Assert.IsNotNull(sub2);
        Assert.AreEqual(4, sub1.Subscribers.Count);
        Assert.AreEqual(4, sub2.Subscribers.Count);
    }

    Task CreateSeedSubscription(MessageType msgType, Guid id, params Subscriber[] subscribers)
    {
        var sub = new Subscription();
        sub.MessageType = msgType;
        sub.Subscribers.AddRange(subscribers.Select(s => new SubscriptionClient
        {
            Endpoint = s.Endpoint,
            TransportAddress = s.TransportAddress
        }));

        var docId = $"Subscriptions/{id}";

        return RavenUtils.StoreAsType(store, docId, typeof(Subscription), sub);
    }
}