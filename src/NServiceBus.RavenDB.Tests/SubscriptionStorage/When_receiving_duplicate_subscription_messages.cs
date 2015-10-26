using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
    public async Task shouldnt_create_additional_db_rows()
    {
        var storage = new SubscriptionPersister(store);

        await storage.Subscribe("testEndPoint@localhost", new List<MessageType>
        {
            new MessageType("SomeMessageType", "1.0.0.0")
        }, new ContextBag()).ConfigureAwait(false);

        await storage.Subscribe("testEndPoint@localhost", new List<MessageType>
        {
            new MessageType("SomeMessageType", "1.0.0.0")
        }, new ContextBag()).ConfigureAwait(false);

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