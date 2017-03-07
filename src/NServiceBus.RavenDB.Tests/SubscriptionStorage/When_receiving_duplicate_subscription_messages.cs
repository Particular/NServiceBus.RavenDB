using System.Linq;
using System.Threading.Tasks;
using NServiceBus.Extensibility;
using NServiceBus.Persistence.RavenDB;
using NServiceBus.RavenDB.Persistence.SubscriptionStorage;
using NServiceBus.RavenDB.Tests;
using NServiceBus.Unicast.Subscriptions;
using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;
using NUnit.Framework;
using Raven.Client;

[TestFixture]
public class When_receiving_duplicate_subscription_messages : RavenDBPersistenceTestBase
{
    public override void SetUp()
    {
        base.SetUp();
        SubscriptionIndex.Create(store);
    }

    [Test]
    public async Task should_not_create_additional_db_rows()
    {
        var storage = new SubscriptionPersister(store);

        await storage.Subscribe(new Subscriber("testEndPoint@localhost", "testEndPoint"), new MessageType("SomeMessageType", "1.0.0.0"), new ContextBag());

        await storage.Subscribe(new Subscriber("testEndPoint@localhost", "testEndPoint"), new MessageType("SomeMessageType", "1.0.0.0"), new ContextBag());

        using (var session = store.OpenAsyncSession())
        {
            var subscriptions = await session
                .Query<Subscription>()
                .Customize(c => c.WaitForNonStaleResults())
                .CountAsync();

            Assert.AreEqual(1, subscriptions);
        }
    }

    [Test]
    public async Task should_not_create_additional_db_rows_even_if_event_version_changed()
    {
        var storage = new SubscriptionPersister(store);

        await storage.Subscribe(new Subscriber("testEndPoint@localhost", "testEndPoint"), new MessageType("SomeMessageType", "1.0.0.0"), new ContextBag());

        await storage.Subscribe(new Subscriber("testEndPoint@localhost", "testEndPoint"), new MessageType("SomeMessageType", "2.0.0.0"), new ContextBag());

        using(var session = store.OpenAsyncSession())
        {
            var subscriptions = await session
                .Query<Subscription>()
                .Customize(c => c.WaitForNonStaleResults())
                .CountAsync();

            Assert.AreEqual(1, subscriptions);
        }
    }


    [Test]
    public async Task should_overwrite_existing_subscription()
    {
        const string subscriberAddress = "testEndPoint@localhost";
        var messageType = new MessageType("SomeMessageType", "1.0.0.0");
        var subscriber_v6 = new Subscriber(subscriberAddress, "endpoint_name");
        var subscriber_v6_2 = new Subscriber(subscriberAddress, "new_endpoint_name");

        var storage = new SubscriptionPersister(store);
        await storage.Subscribe(subscriber_v6, messageType, new ContextBag());
        await storage.Subscribe(subscriber_v6_2, messageType, new ContextBag());

        WaitForIndexing(store);

        var subscriber = (await storage.GetSubscriberAddressesForMessage(new[]
        {
            messageType
        }, new ContextBag())).ToArray();

        Assert.AreEqual(1, subscriber.Length);
        Assert.AreEqual(subscriberAddress, subscriber[0].TransportAddress);
        Assert.AreEqual("new_endpoint_name", subscriber[0].Endpoint);
    }

    [Test]
    public async Task should_overwrite_existing_subscription_event_if_event_version_changed()
    {
        await SubscriptionIndex.CreateAsync(store);

        const string subscriberAddress = "testEndPoint@localhost";
        var messageTypeV1 = new MessageType("SomeMessageType", "1.0.0.0");
        var messageTypeV2 = new MessageType("SomeMessageType", "2.0.0.0");
        var subscriber_v6 = new Subscriber(subscriberAddress, "endpoint_name");
        var subscriber_v6_2 = new Subscriber(subscriberAddress, "new_endpoint_name");

        var storage = new SubscriptionPersister(store);
        await storage.Subscribe(subscriber_v6, messageTypeV1, new ContextBag());
        await storage.Subscribe(subscriber_v6_2, messageTypeV2, new ContextBag());

        WaitForIndexing(store);

        var subscriber = (await storage.GetSubscriberAddressesForMessage(new[]
        {
            messageTypeV1
        }, new ContextBag())).ToArray();

        Assert.AreEqual(1, subscriber.Length);
        Assert.AreEqual(subscriberAddress, subscriber[ 0 ].TransportAddress);
        Assert.AreEqual("new_endpoint_name", subscriber[ 0 ].Endpoint);
    }
}