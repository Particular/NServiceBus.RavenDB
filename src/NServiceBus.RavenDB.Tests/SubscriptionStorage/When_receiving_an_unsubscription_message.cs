using System.Linq;
using NServiceBus.RavenDB.Persistence;
using NServiceBus.RavenDB.Persistence.SubscriptionStorage;
using NUnit.Framework;

[TestFixture]
public class When_receiving_an_unsubscribe_message 
{
    [Test]
    public void All_subscription_entries_for_specified_message_types_should_be_removed()
    {
        using (var store = DocumentStoreBuilder.Build())
        {
            var storage = new RavenSubscriptionStorage(new StoreAccessor(store));

            storage.Subscribe(TestClients.ClientA, MessageTypes.All);

            storage.Unsubscribe(TestClients.ClientA, MessageTypes.All);

            var clients = storage.GetSubscriberAddressesForMessage(MessageTypes.All);
            Assert.IsFalse(clients.Any(a => a == TestClients.ClientA));
        }
    }
}