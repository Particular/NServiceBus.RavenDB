namespace NServiceBus.RavenDB.Tests.SubscriptionStorage
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Persistence.RavenDB;
    using NServiceBus.RavenDB.Persistence.SubscriptionStorage;
    using NServiceBus.Unicast.Subscriptions;
    using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;
    using NUnit.Framework;
    using System;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    [TestFixture]
    public class When_storing_subscriptions : RavenDBPersistenceTestBase
    {
        public override void SetUp()
        {
            base.SetUp();

            store.Listeners.RegisterListener(new SubscriptionV1toV2Converter());

            persister = new SubscriptionPersister(store);
            msgType = new MessageType(typeof(MessageA));
            docId = new VersionedSubscriptionIdFormatter().FormatId(msgType);
        }

        [Test]
        public async Task Should_allow_for_null_machine_name()
        {
            var subscriptionsV3 = new SubscriptionV3
            {
                Id = docId,
                MessageType = msgType,
                Clients = new List<LegacyAddress>
                {
                    new LegacyAddress { Queue = "QueueA", Machine = null },
                    new LegacyAddress { Queue = "QueueB", Machine = null },
                }
            };

            await StoreAsType(subscriptionsV3.Id, typeof(Subscription), subscriptionsV3);

            await persister.Subscribe(new Subscriber("QueueB", "QueueB"), msgType, new ContextBag());

            using (store.DatabaseCommands.DisableAllCaching())
            {
                using (var session = store.OpenAsyncSession())
                {
                    var resultDoc = await session.LoadAsync<Subscription>(docId);

                    Assert.AreEqual(docId, resultDoc.Id);
                    Assert.AreEqual(msgType, resultDoc.MessageType);

                    Assert.AreEqual(2, resultDoc.Subscribers.Count);
                    Assert.AreEqual(2, resultDoc.LegacySubscriptions.Count);

                    Assert.IsTrue(resultDoc.Subscribers.Any(s => s.TransportAddress == "QueueA" && s.Endpoint == null)); // null because converted
                    Assert.IsTrue(resultDoc.Subscribers.Any(s => s.TransportAddress == "QueueB" && s.Endpoint == "QueueB")); // converted but overwritten

                    Assert.IsTrue(resultDoc.LegacySubscriptions.Any(s => s.Queue == "QueueA" && s.Machine == null));
                    Assert.IsTrue(resultDoc.LegacySubscriptions.Any(s => s.Queue == "QueueB" && s.Machine == null));
                }
            }
        }

        SubscriptionPersister persister;
        MessageType msgType;
        string docId;

        class SubscriptionV3
        {
            public string Id { get; set; }
            [JsonConverter(typeof(MessageTypeConverter))]
            public MessageType MessageType { get; set; }
            public List<LegacyAddress> Clients { get; set; }
        }

        Task StoreAsType(string documentId, Type storeAsType, object document)
        {
            var docJson = JObject.FromObject(document);
            var metadata = new JObject();
            metadata["Raven-Entity-Name"] = storeAsType.Name;
            metadata["Raven-Clr-Type"] = storeAsType.AssemblyQualifiedName;

            return store.AsyncDatabaseCommands.PutAsync(documentId, Etag.Empty, docJson, metadata);
        }
    }
}
