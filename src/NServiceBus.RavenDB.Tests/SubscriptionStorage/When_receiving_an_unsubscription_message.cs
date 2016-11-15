using System;
using System.Threading.Tasks;
using NServiceBus.Extensibility;
using NServiceBus.Persistence.RavenDB;
using NServiceBus.RavenDB.Persistence.SubscriptionStorage;
using NServiceBus.RavenDB.Tests;
using NServiceBus.Unicast.Subscriptions;
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

        await storage.Unsubscribe(TestClients.ClientA, MessageTypes.MessageA, context);
        await storage.Unsubscribe(TestClients.ClientA, MessageTypes.MessageB, context);

        WaitForIndexing(store);

        var clients = await storage.GetSubscriberAddressesForMessage(new []{ MessageTypes.MessageA, MessageTypes.MessageB }, context);

        Assert.IsEmpty(clients);
    }

    [Test]
    public async Task All_subscription_entries_for_specified_message_types_should_be_removed_even_if_version_changes()
    {
        await SubscriptionIndex.CreateAsync(store);

        var storage = new SubscriptionPersister(store);
        var context = new ContextBag();

        await storage.Subscribe(TestClients.ClientA, MessageTypes.MessageA, context);
        await storage.Subscribe(TestClients.ClientA, MessageTypes.MessageB, context);

        await storage.Unsubscribe(TestClients.ClientA, MessageTypes.MessageAv2, context);
        await storage.Unsubscribe(TestClients.ClientA, MessageTypes.MessageB, context);

        WaitForIndexing(store);

        var clients = await storage.GetSubscriberAddressesForMessage(new[] { MessageTypes.MessageAv2, MessageTypes.MessageB }, context);

        Assert.IsEmpty(clients);
    }

    // I don't trust the mirrored Subscribe/Unsubscribe test above. I think we should replace that with what I have below.

    [Test]
    public async Task Should_remove_matching_documents_for_any_version()
    {
        await SubscriptionIndex.CreateAsync(store);

        var storage = new SubscriptionPersister(store);
        var context = new ContextBag();

        await CreateSeedSubscription(MessageTypes.MessageA);
        await CreateSeedSubscription(MessageTypes.MessageAv11);
        await CreateSeedSubscription(MessageTypes.MessageAv2);
        await CreateSeedSubscription(MessageTypes.MessageB);

        await storage.Unsubscribe(TestClients.ClientA, MessageTypes.MessageA, context);

        WaitForIndexing(store);

        var messageAClients = await storage.GetSubscriberAddressesForMessage(new[] { MessageTypes.MessageAv2, MessageTypes.MessageAv11, MessageTypes.MessageA }, context);
        var messageBClients = await storage.GetSubscriberAddressesForMessage(new[] { MessageTypes.MessageB }, context);

        Assert.IsEmpty(messageAClients);
        Assert.AreEqual(1, messageBClients);
    }

    private Task CreateSeedSubscription(MessageType msgType, Guid? id = null)
    {
        var sub = new Subscription();
        sub.MessageType = msgType;
        sub.Subscribers.Add(new SubscriptionClient
        {
            Endpoint = "TestEndpoint",
            TransportAddress = "TestEndpoint@Machine"
        });

        var docId = $"Subscriptions/{id ?? Guid.NewGuid()}";

        return RavenUtils.StoreAsType(store, docId, typeof(Subscription), msgType);
    }
}