namespace NServiceBus.RavenDB.Tests.SubscriptionStorage
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Newtonsoft.Json;
    using NServiceBus.Extensibility;
    using NServiceBus.Persistence.RavenDB;
    using NServiceBus.RavenDB.Persistence.SubscriptionStorage;
    using NServiceBus.Support;
    using NServiceBus.Unicast.Subscriptions;
    using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;
    using NUnit.Framework;
    using Raven.Client.Documents;
    using LegacyAddress = NServiceBus.RavenDB.Tests.LegacyAddress;

    [TestFixture]
    public class When_converting_old_subscription_to_new_subscription : RavenDBPersistenceTestBase
    {
        public override void SetUp()
        {
            base.SetUp();

            var concreteStore = (DocumentStore) store;

            SubscriptionV1toV2Converter.Register(concreteStore);

            // TODO: This was converted from an AfterConversionToDocument listener (FakeSubscriptionClrType) setting Raven-Clr-Type to the string below. Needs testing.
            concreteStore.OnBeforeStore += (s,e) => e.DocumentMetadata["@collection"] = "NServiceBus.RavenDB.Persistence.SubscriptionStorage.Subscription, NServiceBus.RavenDB";

            persister = new SubscriptionPersister(store);
        }

        [Test]
        public async Task Should_allow_old_subscriptions()
        {
            var session = store.OpenAsyncSession();
            var messageType = MessageTypes.MessageA;
            await session.StoreAsync(new OldSubscription
            {
                Clients = new List<LegacyAddress>
                {
                    new LegacyAddress("timeouts", RuntimeEnvironment.MachineName),
                    new LegacyAddress("mytestendpoint", RuntimeEnvironment.MachineName)
                },
                MessageType = messageType
            }, new VersionedSubscriptionIdFormatter().FormatId(messageType))
            .ConfigureAwait(false);
            await session.SaveChangesAsync().ConfigureAwait(false);

            List<Subscriber> subscriptions = null;

            var exception = await Catch(async () => { subscriptions = (await persister.GetSubscriberAddressesForMessage(new []{ MessageTypes.MessageA }, new ContextBag())).ToList(); });
            Assert.Null(exception);

            Assert.AreEqual($"timeouts@{RuntimeEnvironment.MachineName}", subscriptions[0].TransportAddress);
            Assert.AreEqual(null, subscriptions[0].Endpoint);

            Assert.AreEqual($"mytestendpoint@{RuntimeEnvironment.MachineName}", subscriptions[1].TransportAddress);
            Assert.AreEqual(null, subscriptions[1].Endpoint);


        }

        [Test]
        public async Task Should_allow_old_subscriptions_without_machine_name()
        {
            var session = store.OpenAsyncSession();
            var messageType = MessageTypes.MessageA;
            await session.StoreAsync(new OldSubscription
            {
                Clients = new List<LegacyAddress>
                {
                    new LegacyAddress("timeouts", null),
                    new LegacyAddress("mytestendpoint", null)
                },
                MessageType = messageType
            }, new VersionedSubscriptionIdFormatter().FormatId(messageType)).ConfigureAwait(false);
            await session.SaveChangesAsync().ConfigureAwait(false);

            List<Subscriber> subscriptions = null;
            var exception = await Catch(async () => { subscriptions = (await persister.GetSubscriberAddressesForMessage(new []{ MessageTypes.MessageA }, new ContextBag())).ToList(); });
            Assert.Null(exception);

            var timeoutsSubscriber = new Subscriber("timeouts", "timeouts");
            var mytestendpointSubscriber = new Subscriber("mytestendpoint", "mytestendpoint");

            Assert.AreEqual(timeoutsSubscriber.TransportAddress, subscriptions[0].TransportAddress);
            Assert.AreEqual(null, subscriptions[0].Endpoint);

            Assert.AreEqual(mytestendpointSubscriber.TransportAddress, subscriptions[1].TransportAddress);
            Assert.AreEqual(null, subscriptions[1].Endpoint);
        }

        [Test]
        public async Task Should_allow_old_subscriptions_with_empty_clients()
        {
            var session = store.OpenAsyncSession();
            var messageType = MessageTypes.MessageA;
            await session.StoreAsync(new OldSubscription
            {
                Clients = new List<LegacyAddress>(),
                MessageType = messageType
            }, new VersionedSubscriptionIdFormatter().FormatId(messageType)).ConfigureAwait(false);
            await session.SaveChangesAsync().ConfigureAwait(false);

            List<Subscriber> subscriptions = null;
            var exception = await Catch(async () => { subscriptions = (await persister.GetSubscriberAddressesForMessage(new []{ MessageTypes.MessageA }, new ContextBag())).ToList(); });
            Assert.Null(exception);
            Assert.IsEmpty(subscriptions);
        }

        [Test]
        public async Task Should_allow_new_subscriptions()
        {
            var session = store.OpenAsyncSession();
            var messageType = MessageTypes.MessageA;

            await session.StoreAsync(new Subscription
            {
                Subscribers = new List<SubscriptionClient>
                {
                    new SubscriptionClient { TransportAddress = "timeouts" + "@" + RuntimeEnvironment.MachineName,  Endpoint = "timeouts" },
                    new SubscriptionClient { TransportAddress = "mytestendpoint" + "@" + RuntimeEnvironment.MachineName, Endpoint = "mytestendpoint" }
                },
                MessageType = messageType
            }, new VersionedSubscriptionIdFormatter().FormatId(messageType)).ConfigureAwait(false);

            await session.SaveChangesAsync().ConfigureAwait(false);

            var exception = await Catch(async () => { (await persister.GetSubscriberAddressesForMessage(new[] { MessageTypes.MessageA }, new ContextBag())).ToList(); });
            Assert.Null(exception);
        }

        [Test]
        public async Task The_old_subscription_can_be_overwritten()
        {
            var session = store.OpenAsyncSession();

            await session.StoreAsync(new OldSubscription
            {
                Clients = new List<LegacyAddress>
                {
                    new LegacyAddress("timeouts", RuntimeEnvironment.MachineName),
                    new LegacyAddress("mytestendpoint", RuntimeEnvironment.MachineName)
                },
                MessageType = MessageTypes.MessageA
            }, new VersionedSubscriptionIdFormatter().FormatId(MessageTypes.MessageA))
            .ConfigureAwait(false);
            await session.SaveChangesAsync().ConfigureAwait(false);

            List<Subscriber> subscriptions = null;

            var timeoutsSubscriber = new Subscriber("timeouts" + "@" + RuntimeEnvironment.MachineName, "timeouts");
            var mytestendpointSubscriber = new Subscriber("mytestendpoint" + "@" + RuntimeEnvironment.MachineName, "mytestendpoint");

            await persister.Subscribe(timeoutsSubscriber, MessageTypes.MessageA, null);
            await persister.Subscribe(mytestendpointSubscriber, MessageTypes.MessageA, null);

            await Catch(async () => { subscriptions = (await persister.GetSubscriberAddressesForMessage(new[] { MessageTypes.MessageA }, new ContextBag())).ToList(); });

            Assert.AreEqual(timeoutsSubscriber.TransportAddress, subscriptions[0].TransportAddress);
            Assert.AreEqual(timeoutsSubscriber.Endpoint, subscriptions[0].Endpoint);

            Assert.AreEqual(mytestendpointSubscriber.TransportAddress, subscriptions[1].TransportAddress);
            Assert.AreEqual(mytestendpointSubscriber.Endpoint, subscriptions[1].Endpoint);
        }

        SubscriptionPersister persister;

        class OldSubscription
        {
            public string Id { get; set; }

            [JsonConverter(typeof(MessageTypeConverter))]
            // ReSharper disable once UnusedAutoPropertyAccessor.Global
            public MessageType MessageType { get; set; }

            public List<LegacyAddress> Clients { get; set; }
        }
    }
}