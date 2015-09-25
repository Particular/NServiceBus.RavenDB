using System.Linq;
using System.Threading.Tasks;
using NServiceBus.Extensibility;
using NServiceBus.RavenDB.Tests;
using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;
using NServiceBus.Unicast.Subscriptions.RavenDB;
using NUnit.Framework;

[TestFixture]
public class When_receiving_an_unsubscribe_message : RavenDBPersistenceTestBase
{
    [Test]
    public async Task All_subscription_entries_for_specified_message_types_should_be_removed()
    {
        var storage = new SubscriptionPersister(store);
        var query = new QuerySubscriptions(store);
        var options = new SubscriptionStorageOptions(new ContextBag());

        await storage.Subscribe(TestClients.ClientA, MessageTypes.All, options);

        await storage.Unsubscribe(TestClients.ClientA, MessageTypes.All, options);

        var clients = await query.GetSubscriberAddressesForMessage(MessageTypes.All);
        Assert.IsFalse(clients.Any(a => a == TestClients.ClientA));
    }
}