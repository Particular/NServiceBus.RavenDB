using System.Collections.Generic;
using System.Linq;
using NServiceBus.Extensibility;
using NServiceBus.RavenDB.Persistence.SubscriptionStorage;
using NServiceBus.RavenDB.Tests;
using NServiceBus.Unicast.Subscriptions;
using NServiceBus.Unicast.Subscriptions.RavenDB;
using NUnit.Framework;

[TestFixture]
public class When_receiving_duplicate_subscription_messages : RavenDBPersistenceTestBase
{
    [Test]
    public void shouldnt_create_additional_db_rows()
    {
        var storage = new SubscriptionPersister(store);

        storage.Subscribe("testEndPoint@localhost", new List<MessageType>
        {
            new MessageType("SomeMessageType", "1.0.0.0")
        }, new ContextBag());
        storage.Subscribe("testEndPoint@localhost", new List<MessageType>
        {
            new MessageType("SomeMessageType", "1.0.0.0")
        }, new ContextBag());

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