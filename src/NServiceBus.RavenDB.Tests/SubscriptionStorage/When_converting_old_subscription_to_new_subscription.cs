namespace NServiceBus.RavenDB.Tests.SubscriptionStorage
{
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.RavenDB.Persistence.SubscriptionStorage;
    using NServiceBus.RavenDB.Timeouts;
    using NServiceBus.Support;
    using NServiceBus.Unicast.Subscriptions;
    using NServiceBus.Unicast.Subscriptions.RavenDB;
    using NUnit.Framework;
    using Raven.Client.Listeners;
    using Raven.Imports.Newtonsoft.Json;
    using Raven.Json.Linq;

    [TestFixture]
    public class When_converting_old_subscription_to_new_subscription : RavenDBPersistenceTestBase
    {
        QuerySubscriptions query;

        public override void SetUp()
        {
            base.SetUp();

            store.Listeners.RegisterListener(new FakeSubscriptionClrType());
            store.Listeners.RegisterListener(new SubscriptionV1toV2Converter());

            query = new QuerySubscriptions(store);
        }

        [Test]
        public void Should_allow_old_subscriptions()
        {
            var session = store.OpenSession();
            var messageType = MessageTypes.MessageA.Single();
            session.Store(new OldSubscription
            {
                Clients = new List<LegacyAddress>
                {
                    new LegacyAddress("timeouts", RuntimeEnvironment.MachineName),
                    new LegacyAddress("mytestendpoint", RuntimeEnvironment.MachineName),
                },
                MessageType = messageType,
            }, Subscription.FormatId(messageType));
            session.SaveChanges();

            List<string> subscriptions = null;
            Assert.DoesNotThrow(() =>
            {
                subscriptions = query.GetSubscriberAddressesForMessage(MessageTypes.MessageA).ToList();
            });
            Assert.AreEqual("timeouts" + "@" + RuntimeEnvironment.MachineName, subscriptions.ElementAt(0));
            Assert.AreEqual("mytestendpoint" + "@" + RuntimeEnvironment.MachineName, subscriptions.ElementAt(1));
        }

        [Test]
        public void Should_allow_old_subscriptions_without_machine_name()
        {
            var session = store.OpenSession();
            var messageType = MessageTypes.MessageA.Single();
            session.Store(new OldSubscription
            {
                Clients = new List<LegacyAddress>
                {
                    new LegacyAddress("timeouts", null),
                    new LegacyAddress("mytestendpoint", null),
                },
                MessageType = messageType,
            }, Subscription.FormatId(messageType));
            session.SaveChanges();

            List<string> subscriptions = null;
            Assert.DoesNotThrow(() =>
            {
                subscriptions = query.GetSubscriberAddressesForMessage(MessageTypes.MessageA).ToList();
            });
            Assert.AreEqual("timeouts", subscriptions.ElementAt(0));
            Assert.AreEqual("mytestendpoint", subscriptions.ElementAt(1));
        }

        [Test]
        public void Should_allow_old_subscriptions_with_empty_clients()
        {
            var session = store.OpenSession();
            var messageType = MessageTypes.MessageA.Single();
            session.Store(new OldSubscription
            {
                Clients = new List<LegacyAddress>(),
                MessageType = messageType,
            }, Subscription.FormatId(messageType));
            session.SaveChanges();

            List<string> subscriptions = null;
            Assert.DoesNotThrow(() =>
            {
                subscriptions = query.GetSubscriberAddressesForMessage(MessageTypes.MessageA).ToList();
            });
            Assert.IsEmpty(subscriptions);
        }

        [Test]
        public void Should_allow_new_subscriptions()
        {
            var session = store.OpenSession();
            var messageType = MessageTypes.MessageA.Single();
            session.Store(new Subscription()
            {
                Clients = new List<string>
                {
                    "timeouts" + "@" + RuntimeEnvironment.MachineName,
                    "mytestendpoint" + "@" + RuntimeEnvironment.MachineName
                },
                MessageType = messageType,
            }, Subscription.FormatId(messageType));
            session.SaveChanges();

            Assert.DoesNotThrow(() =>
            {
                query.GetSubscriberAddressesForMessage(MessageTypes.MessageA).ToList();
            });
        }

        class FakeSubscriptionClrType : IDocumentConversionListener
        {
            public void BeforeConversionToDocument(string key, object entity, RavenJObject metadata)
            {
            }

            public void AfterConversionToDocument(string key, object entity, RavenJObject document, RavenJObject metadata)
            {
                metadata["Raven-Clr-Type"] = "NServiceBus.RavenDB.Persistence.SubscriptionStorage.Subscription, NServiceBus.RavenDB";
            }

            public void BeforeConversionToEntity(string key, RavenJObject document, RavenJObject metadata)
            {
            }

            public void AfterConversionToEntity(string key, RavenJObject document, RavenJObject metadata, object entity)
            {
            }
        }

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