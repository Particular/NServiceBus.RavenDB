using System.Linq;
using System.Threading.Tasks;
using NServiceBus.Extensibility;
using NServiceBus.RavenDB.Tests;
using NServiceBus.Unicast.Subscriptions.RavenDB;
using NUnit.Framework;

[TestFixture]
public class When_receiving_an_unsubscribe_message : RavenDBPersistenceTestBase
{
    [Test]
    public async Task All_subscription_entries_for_specified_message_types_should_be_removed()
    {
        var storage = new SubscriptionPersister(store);
        var context = new ContextBag();

        await storage.Subscribe(TestClients.ClientA, MessageTypes.All, context);

        await storage.Unsubscribe(TestClients.ClientA, MessageTypes.All, context);

        var clients = await storage.GetSubscriberAddressesForMessage(MessageTypes.All, context);

        Assert.IsFalse(clients.Any(a => a == TestClients.ClientA));
    }
}