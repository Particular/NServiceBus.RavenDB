using System.Threading.Tasks;
using NServiceBus.Extensibility;
using NServiceBus.Persistence.RavenDB;
using NServiceBus.RavenDB.Tests;
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
}