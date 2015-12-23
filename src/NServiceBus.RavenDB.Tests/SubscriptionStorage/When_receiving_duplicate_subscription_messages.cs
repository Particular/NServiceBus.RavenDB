using System.Collections.Generic;
using System.Threading.Tasks;
using NServiceBus.Extensibility;
using NServiceBus.RavenDB.Persistence.SubscriptionStorage;
using NServiceBus.RavenDB.Tests;
using NServiceBus.Routing;
using NServiceBus.Unicast.Subscriptions;
using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;
using NServiceBus.Unicast.Subscriptions.RavenDB;
using NUnit.Framework;
using Raven.Client;

[TestFixture]
public class When_receiving_duplicate_subscription_messages : RavenDBPersistenceTestBase
{
    [Test]
    public async Task should_not_create_additional_db_rows()
    {
        var storage = new SubscriptionPersister(store, new IndividualSubscriptionDocumentAccess());

        await storage.Subscribe(new Subscriber("testEndPoint@localhost", new Endpoint("testEndPoint")), new List<MessageType>
        {
            new MessageType("SomeMessageType", "1.0.0.0")
        }, new ContextBag());

        await storage.Subscribe(new Subscriber("testEndPoint@localhost", new Endpoint("testEndPoint")), new List<MessageType>
        {
            new MessageType("SomeMessageType", "1.0.0.0")
        }, new ContextBag());

        using (var session = store.OpenAsyncSession())
        {
            var subscriptions = await session
                .Query<SubscriptionDocument>()
                .Customize(c => c.WaitForNonStaleResults())
                .CountAsync();

            Assert.AreEqual(1, subscriptions);
        }
    }
}