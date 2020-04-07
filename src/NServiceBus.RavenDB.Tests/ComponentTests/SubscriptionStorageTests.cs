namespace NServiceBus.Persistence.ComponentTests
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Extensibility;
    using NUnit.Framework;
    using Unicast.Subscriptions;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;

    [TestFixture]
    class SubscriptionStorageTests
    {
        [OneTimeSetUp]
        public async Task OneTimeSetUp()
        {
            configuration = new PersistenceTestsConfiguration();
            await configuration.Configure();
            storage = configuration.SubscriptionStorage;
        }

        [OneTimeTearDown]
        public async Task OneTimeTearDown()
        {
            await configuration.Cleanup();
        }

        [SetUp]
        public void Setup()
        {
            configuration.RequiresSubscriptionSupport();
        }

        [Test]
        public async Task Should_not_have_duplicate_subscriptions()
        {
            var eventType = CreateUniqueMessageType();

            await storage.Subscribe(new Subscriber("address1", "endpoint1"), eventType, new ContextBag());
            await storage.Subscribe(new Subscriber("address1", "endpoint1"), eventType, new ContextBag());

            var subscribers = await storage.GetSubscriberAddressesForMessage(new[]
            {
                eventType
            }, new ContextBag());

            Assert.AreEqual(1, subscribers.Count());
            var subscription = subscribers.Single();
            Assert.AreEqual("endpoint1", subscription.Endpoint);
            Assert.AreEqual("address1", subscription.TransportAddress);
        }

        [Test]
        public async Task Should_find_all_transport_addresses_of_logical_endpoint()
        {
            var eventType = CreateUniqueMessageType();

            await storage.Subscribe(new Subscriber("address1", "endpoint1"), eventType, new ContextBag());
            await storage.Subscribe(new Subscriber("address2", "endpoint1"), eventType, new ContextBag());

            var subscribers = await storage.GetSubscriberAddressesForMessage(new[]
            {
                eventType
            }, new ContextBag());

            Assert.AreEqual(2, subscribers.Count());
            CollectionAssert.AreEquivalent(new[] {"address1", "address2"}, subscribers.Select(s => s.TransportAddress));
        }

        [Test]
        public async Task Should_update_endpoint_name_for_transport_address()
        {
            var eventType = CreateUniqueMessageType();

            await storage.Subscribe(new Subscriber("address1", "endpointA"), eventType, new ContextBag());
            await storage.Subscribe(new Subscriber("address1", "endpointB"), eventType, new ContextBag());

            var subscribers = await storage.GetSubscriberAddressesForMessage(new[]
            {
                eventType
            }, new ContextBag());

            Assert.AreEqual(1, subscribers.Count());
            Assert.AreEqual("endpointB", subscribers.Single().Endpoint);
        }

        [Test]
        public async Task Should_find_all_queried_message_types()
        {
            var eventType1 = CreateUniqueMessageType();
            var eventType2 = CreateUniqueMessageType();

            await storage.Subscribe(new Subscriber("address", "endpoint1"), eventType1, new ContextBag());
            await storage.Subscribe(new Subscriber("address", "endpoint1"), eventType2, new ContextBag());

            var subscribers = await storage.GetSubscriberAddressesForMessage(new[]
            {
                eventType1, eventType2
            }, new ContextBag());

            Assert.AreEqual(2, subscribers.Count());
            CollectionAssert.AreEquivalent(new[] {"address", "address"}, subscribers.Select(s => s.TransportAddress));
        }

        [Test]
        public async Task Should_not_unsubscribe_when_address_does_not_match()
        {
            var eventType1 = CreateUniqueMessageType();

            await storage.Subscribe(new Subscriber("address", "endpoint1"), eventType1, new ContextBag());
            await storage.Unsubscribe(new Subscriber("another address", "endpoint1"), eventType1, new ContextBag());

            var subscribers = await storage.GetSubscriberAddressesForMessage(new[]
            {
                eventType1
            }, new ContextBag());

            Assert.AreEqual("address", subscribers.Single().TransportAddress);
        }

        [Test]
        public async Task Should_unsubscribe_when_logical_endpoint_does_not_match()
        {
            var eventType1 = CreateUniqueMessageType();

            await storage.Subscribe(new Subscriber("address", "endpoint1"), eventType1, new ContextBag());
            await storage.Subscribe(new Subscriber("address", "endpoint2"), eventType1, new ContextBag());
            await storage.Unsubscribe(new Subscriber("address", "endpoint1"), eventType1, new ContextBag());

            var subscribers = await storage.GetSubscriberAddressesForMessage(new[]
            {
                eventType1
            }, new ContextBag());

            Assert.AreEqual(0, subscribers.Count());
        }

        [Test]
        public async Task Should_handle_legacy_subscription_message()
        {
            var eventType = CreateUniqueMessageType();

            await storage.Subscribe(new Subscriber("address", null), eventType, new ContextBag());

            var subscribers = await storage.GetSubscriberAddressesForMessage(new[]
            {
                eventType
            }, new ContextBag());

            Assert.AreEqual(1, subscribers.Count());
            var subscriber = subscribers.Single();
            Assert.AreEqual("address", subscriber.TransportAddress);
            Assert.IsNull(subscriber.Endpoint);
        }

        [Test]
        public async Task Should_add_endpoint_on_new_subscription()
        {
            var eventType = CreateUniqueMessageType();

            await storage.Subscribe(new Subscriber("address", null), eventType, new ContextBag());
            await storage.Subscribe(new Subscriber("address", "endpoint"), eventType, new ContextBag());

            var subscribers = await storage.GetSubscriberAddressesForMessage(new[]
            {
                eventType
            }, new ContextBag());

            Assert.AreEqual(1, subscribers.Count());
            var subscriber = subscribers.Single();
            Assert.AreEqual("address", subscriber.TransportAddress);
            Assert.AreEqual("endpoint", subscriber.Endpoint);
        }

        [Test]
        public async Task Should_not_remove_endpoint_on_legacy_subscriptions()
        {
            var eventType = CreateUniqueMessageType();

            await storage.Subscribe(new Subscriber("address", "endpoint"), eventType, new ContextBag());
            await storage.Subscribe(new Subscriber("address", null), eventType, new ContextBag());

            var subscribers = await storage.GetSubscriberAddressesForMessage(new[]
            {
                eventType
            }, new ContextBag());

            Assert.AreEqual(1, subscribers.Count());
            var subscriber = subscribers.Single();
            Assert.AreEqual("address", subscriber.TransportAddress);
            Assert.AreEqual("endpoint", subscriber.Endpoint);
        }

        [Test]
        public async Task Should_ignore_message_version_on_subscriptions()
        {
            await storage.Subscribe(new Subscriber("subscriberA@server1", "subscriberA"), new MessageType("SomeMessage", "1.0.0"), new ContextBag());

            var subscribers = await storage.GetSubscriberAddressesForMessage(new[]
            {
                new MessageType("SomeMessage", "2.0.0")
            }, new ContextBag());

            Assert.AreEqual("subscriberA", subscribers.Single().Endpoint);
        }

        static MessageType CreateUniqueMessageType()
        {
            return new MessageType(Guid.NewGuid().ToString("N"), "1.0.0");
        }

        PersistenceTestsConfiguration configuration;
        ISubscriptionStorage storage;
    }
}