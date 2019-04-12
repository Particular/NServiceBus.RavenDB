
namespace NServiceBus.RavenDB.Tests.SubscriptionStorage
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Persistence.RavenDB;
    using NServiceBus.RavenDB.Persistence.SubscriptionStorage;
    using NServiceBus.Unicast.Subscriptions;
    using NUnit.Framework;
    using System.Collections.Generic;
    using System.Linq;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using NServiceBus.Extensibility;
    using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;
    using Raven.Client.Documents;

    public class When_using_subscription_converter : RavenDBPersistenceTestBase
    {
        SubscriptionPersister persister;
        string docId;
        MessageType msgType;

        public override void SetUp()
        {
            base.SetUp();

            SubscriptionV1toV2Converter.Register((DocumentStore) store);

            persister = new SubscriptionPersister(store);
            msgType = new MessageType(typeof(MessageA));
            docId = new VersionedSubscriptionIdFormatter().FormatId(msgType);
        }

        [Test]
        public async Task Should_convert_forward()
        {
            var subscriptionsV3 = new SubscriptionV3
            {
                Id = docId,
                MessageType = msgType,
                Clients = new List<LegacyAddress>
                {
                    new LegacyAddress { Queue = "QueueA", Machine = "MachineA"},
                    new LegacyAddress { Queue = "QueueB", Machine = "MachineB"},
                }
            };

            await StoreAsType(subscriptionsV3.Id, typeof(Subscription), subscriptionsV3);

            await persister.Subscribe(new Subscriber("QueueC@MachineC", "QueueC"), msgType, new ContextBag());

            using (store.DatabaseCommands.DisableAllCaching())
            {
                using (var session = store.OpenAsyncSession())
                {
                    var resultDoc = await session.LoadAsync<Subscription>(docId);

                    Assert.AreEqual(docId, resultDoc.Id);
                    Assert.AreEqual(msgType, resultDoc.MessageType);

                    Assert.AreEqual(3, resultDoc.Subscribers.Count);
                    Assert.AreEqual(3, resultDoc.LegacySubscriptions.Count);

                    Assert.IsTrue(resultDoc.Subscribers.Any(s => s.TransportAddress == "QueueA@MachineA" && s.Endpoint == null)); // null because converted
                    Assert.IsTrue(resultDoc.Subscribers.Any(s => s.TransportAddress == "QueueB@MachineB" && s.Endpoint == null)); // null because converted
                    Assert.IsTrue(resultDoc.Subscribers.Any(s => s.TransportAddress == "QueueC@MachineC" && s.Endpoint == "QueueC"));

                    Assert.IsTrue(resultDoc.LegacySubscriptions.Any(s => s.Queue == "QueueA" && s.Machine == "MachineA"));
                    Assert.IsTrue(resultDoc.LegacySubscriptions.Any(s => s.Queue == "QueueB" && s.Machine == "MachineB"));
                    Assert.IsTrue(resultDoc.LegacySubscriptions.Any(s => s.Queue == "QueueC" && s.Machine == "MachineC"));
                }
            }
        }

        [Test]
        public async Task Should_convert_forward_with_overwrite()
        {
            var subscriptionsV3 = new SubscriptionV3
            {
                Id = docId,
                MessageType = msgType,
                Clients = new List<LegacyAddress>
                {
                    new LegacyAddress { Queue = "QueueA", Machine = "MachineA"},
                    new LegacyAddress { Queue = "QueueB", Machine = "MachineB"},
                }
            };

            await StoreAsType(subscriptionsV3.Id, typeof(Subscription), subscriptionsV3);

            await persister.Subscribe(new Subscriber("QueueB@MachineB", "QueueB"), msgType, new ContextBag());

            using (store.DatabaseCommands.DisableAllCaching())
            {
                using (var session = store.OpenAsyncSession())
                {
                    var resultDoc = await session.LoadAsync<Subscription>(docId);

                    Assert.AreEqual(docId, resultDoc.Id);
                    Assert.AreEqual(msgType, resultDoc.MessageType);

                    Assert.AreEqual(2, resultDoc.Subscribers.Count);
                    Assert.AreEqual(2, resultDoc.LegacySubscriptions.Count);

                    Assert.IsTrue(resultDoc.Subscribers.Any(s => s.TransportAddress == "QueueA@MachineA" && s.Endpoint == null)); // null because converted
                    Assert.IsTrue(resultDoc.Subscribers.Any(s => s.TransportAddress == "QueueB@MachineB" && s.Endpoint == "QueueB")); // converted but overwritten

                    Assert.IsTrue(resultDoc.LegacySubscriptions.Any(s => s.Queue == "QueueA" && s.Machine == "MachineA"));
                    Assert.IsTrue(resultDoc.LegacySubscriptions.Any(s => s.Queue == "QueueB" && s.Machine == "MachineB"));
                }
            }
        }

        [Test]
        public async Task Should_convert_backward()
        {
            await persister.Subscribe(new Subscriber("QueueA@MachineA", "QueueA"), msgType, new ContextBag());
            await persister.Subscribe(new Subscriber("QueueB@MachineB", "QueueB"), msgType, new ContextBag());
            await persister.Subscribe(new Subscriber("QueueC@MachineC", "QueueC"), msgType, new ContextBag());

            using (store.DatabaseCommands.DisableAllCaching())
            {
                using (var session = store.OpenAsyncSession())
                {
                    var resultDoc = await session.LoadAsync<Subscription>(docId);

                    Assert.AreEqual(docId, resultDoc.Id);
                    Assert.AreEqual(msgType, resultDoc.MessageType);

                    Assert.AreEqual(3, resultDoc.Subscribers.Count);
                    Assert.AreEqual(3, resultDoc.LegacySubscriptions.Count);

                    Assert.IsTrue(resultDoc.Subscribers.Any(s => s.TransportAddress == "QueueA@MachineA" && s.Endpoint == "QueueA"));
                    Assert.IsTrue(resultDoc.Subscribers.Any(s => s.TransportAddress == "QueueB@MachineB" && s.Endpoint == "QueueB"));
                    Assert.IsTrue(resultDoc.Subscribers.Any(s => s.TransportAddress == "QueueC@MachineC" && s.Endpoint == "QueueC"));

                    Assert.IsTrue(resultDoc.LegacySubscriptions.Any(s => s.Queue == "QueueA" && s.Machine == "MachineA"));
                    Assert.IsTrue(resultDoc.LegacySubscriptions.Any(s => s.Queue == "QueueB" && s.Machine == "MachineB"));
                    Assert.IsTrue(resultDoc.LegacySubscriptions.Any(s => s.Queue == "QueueC" && s.Machine == "MachineC"));
                }
            }
        }

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
