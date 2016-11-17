using System;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus.Extensibility;
using NServiceBus.Persistence.RavenDB;
using NServiceBus.RavenDB.Persistence.SubscriptionStorage;
using NServiceBus.RavenDB.Tests;
using NServiceBus.Unicast.Subscriptions;
using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;
using NUnit.Framework;

[TestFixture]
public class When_receiving_an_unsubscribe_message : RavenDBPersistenceTestBase
{
    [Test]
    public async Task All_subscription_entries_for_specified_message_types_should_be_removed()
    {
        await SubscriptionIndex.CreateAsync(store);

        var storage = new SubscriptionPersister(store);
        var context = new ContextBag();

        await storage.Subscribe(TestClients.ClientA, MessageTypes.MessageA, context);
        await storage.Subscribe(TestClients.ClientA, MessageTypes.MessageB, context);

        // When_receiving_a_subscription_message.A_subscription_entry_should_be_added_to_the_database() ensures the Arrange above is valid

        await storage.Unsubscribe(TestClients.ClientA, MessageTypes.MessageA, context);
        await storage.Unsubscribe(TestClients.ClientA, MessageTypes.MessageB, context);

        WaitForIndexing(store);

        var clients = await storage.GetSubscriberAddressesForMessage(new[] { MessageTypes.MessageA, MessageTypes.MessageB }, context);

        Assert.IsEmpty(clients);
    }

    [Test]
    public async Task Should_remove_matching_documents_for_any_version()
    {
        await SubscriptionIndex.CreateAsync(store);

        var storage = new SubscriptionPersister(store);
        var context = new ContextBag();

        var idA = Guid.NewGuid();
        var idAv11 = Guid.NewGuid();
        var idAv2 = Guid.NewGuid();
        var idB = Guid.NewGuid();

        var ids = new[] { idA, idAv11, idAv2, idB };
        var docIds = ids.Select(id => $"Subscriptions/{id}").ToArray();

        await CreateSeedSubscription(MessageTypes.MessageA, idA, TestClients.ClientA);
        await CreateSeedSubscription(MessageTypes.MessageAv11, idAv11, TestClients.ClientA);
        await CreateSeedSubscription(MessageTypes.MessageAv2, idAv2, TestClients.ClientA);
        await CreateSeedSubscription(MessageTypes.MessageB, idB, TestClients.ClientA);

        await storage.Unsubscribe(TestClients.ClientA, MessageTypes.MessageA, context);

        WaitForIndexing(store);

        var messageAClients = await storage.GetSubscriberAddressesForMessage(new[] { MessageTypes.MessageAv2, MessageTypes.MessageAv11, MessageTypes.MessageA }, context);
        var messageBClients = await storage.GetSubscriberAddressesForMessage(new[] { MessageTypes.MessageB }, context);

        Subscription[] subs;
        using (var session = store.OpenAsyncSession())
        {
            subs = await session.LoadAsync<Subscription>(docIds);
        }

        Assert.IsEmpty(messageAClients);
        Assert.AreEqual(1, messageBClients.Count());
        Assert.IsNull(subs[0]); // A
        Assert.IsNull(subs[1]); // Av11
        Assert.IsNull(subs[2]); // Av2
        Assert.IsNotNull(subs[3]);
        Assert.AreEqual(1, subs[3].Subscribers.Count); // B
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