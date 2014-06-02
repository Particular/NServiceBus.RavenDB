using System.Collections.Generic;
using System.Linq;
using NServiceBus;
using NServiceBus.RavenDB.Persistence.SubscriptionStorage;
using NServiceBus.Unicast.Subscriptions;
using NUnit.Framework;

[TestFixture]
public class When_receiving_duplicate_subscription_messages 
{
    [Test]
    public void shouldnt_create_additional_db_rows()
    {

        using (var store = DocumentStoreBuilder.Build())
        {
            var storage = new SubscriptionStorage(store);

            storage.Subscribe(new Address("testEndPoint", "localhost"), new List<MessageType>
                {
                    new MessageType("SomeMessageType", "1.0.0.0")
                });
            storage.Subscribe(new Address("testEndPoint", "localhost"), new List<MessageType>
                {
                    new MessageType("SomeMessageType", "1.0.0.0")
                });


            using (var session = store.OpenSession())
            {
                var subscriptions = session
                    .Query<Subscription>()
                    .Customize(c => c.WaitForNonStaleResults())
                    .Count();

                Assert.AreEqual(1, subscriptions);
            }
        }
    }
}