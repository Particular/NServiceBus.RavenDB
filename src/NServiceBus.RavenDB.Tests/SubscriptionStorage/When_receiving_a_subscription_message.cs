using System.Linq;
using System.Transactions;
using NServiceBus;
using NServiceBus.RavenDB.Persistence;
using NServiceBus.RavenDB.Persistence.SubscriptionStorage;
using NServiceBus.Unicast.Subscriptions;
using NUnit.Framework;

[TestFixture]
public class When_receiving_a_subscription_message 
{
    [Test]
    public void A_subscription_entry_should_be_added_to_the_database()
    {
        var clientEndpoint = Address.Parse("TestEndpoint");

        var messageTypes = new[]
            {
                new MessageType("MessageType1", "1.0.0.0"),
                new MessageType("MessageType2", "1.0.0.0")
            };

        using (var store = DocumentStoreBuilder.Build())
        {
            var storage = new RavenSubscriptionStorage(new StoreAccessor(store));

            using (var transaction = new TransactionScope())
            {
                storage.Subscribe(clientEndpoint, messageTypes);
                transaction.Complete();
            }

            using (var session = store.OpenSession())
            {
                var subscriptions = session
                    .Query<Subscription>()
                    .Customize(c => c.WaitForNonStaleResults())
                    .Count();

                Assert.AreEqual(2, subscriptions);
            }
        }
    }
}