namespace NServiceBus.RavenDB.Tests.Persistence
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.RavenDB.Persistence.SubscriptionStorage;
    using NUnit.Framework;
    using Raven.Abstractions.Data;
    using Raven.Json.Linq;

    public class RavenSerializerAssumptions
    {
        /// <summary>
        /// This test validates the assumption that RavenDB will not throw out values of properties it cannot deserialize.
        /// This means that old versions of NServiceBus.RavenDB deployed side-by-side can modify subscription storage
        /// documents without fear of losing changes added in new properties that are unknown to the older version.
        /// </summary>
        [Test]
        public async Task Serializer_keeps_non_matching_properties()
        {
            using (var db = new ReusableDB())
            {
                var store = db.NewStore().Initialize();

                // Create JSON doc mimicking a V4 document containing a new property
                var likeV4 = new LikeV4Subscriptions();
                likeV4.Clients = new List<LegacyAddress> { new LegacyAddress("A", "B") };
                likeV4.Subscriptions = new List<SubscriptionClient> { new SubscriptionClient { Endpoint = "C", TransportAddress = "D" } };
                var docJson = RavenJObject.FromObject(likeV4);

                // Create metadata to make it look like an older version
                var docMetadata = new RavenJObject();
                var fakeDocType = typeof(LikeV3Subscriptions);
                docMetadata["Raven-Entity-Name"] = fakeDocType.Name;
                // Doesn't appear to matter, but $"{fakeDocType.FullName}, {fakeDocType.Assembly.GetName().Name}" may be more accurate
                docMetadata["Raven-Clr-Type"] = fakeDocType.AssemblyQualifiedName;

                // Store document as "old" format
                await store.AsyncDatabaseCommands.PutAsync("TestDocument/1", Etag.Empty, docJson, docMetadata);

                // Load and modify the document using the "old" type that doesn't know about new property
                using (var session = store.OpenAsyncSession())
                {
                    var subs = await session.LoadAsync<LikeV3Subscriptions>("TestDocument/1");
                    subs.Clients.Add(new LegacyAddress("E", "F"));
                    await session.SaveChangesAsync();
                }

                var resultDoc = await store.AsyncDatabaseCommands.GetAsync("TestDocument/1");
                var resultJson = resultDoc.DataAsJson;

                var clients = resultJson["Clients"] as RavenJArray;
                var subscriptions = resultJson["Subscriptions"] as RavenJArray;

                Assert.IsNotNull(clients);
                Assert.IsNotNull(subscriptions);
                Assert.AreEqual(1, subscriptions.Length);
                Assert.AreEqual(2, clients.Length);

                var sub = (RavenJObject)subscriptions[0];
                Assert.AreEqual("C", sub["Endpoint"].Value<string>());
                Assert.AreEqual("D", sub["TransportAddress"].Value<string>());

                var client0 = (RavenJObject)clients[0];
                var client1 = (RavenJObject)clients[1];
                Assert.AreEqual("A", client0["Queue"].Value<string>());
                Assert.AreEqual("B", client0["Machine"].Value<string>());
                Assert.AreEqual("E", client1["Queue"].Value<string>());
                Assert.AreEqual("F", client1["Machine"].Value<string>());
            }
        }

        /// <summary>
        /// In order for JSON.NET to be able to deserialize a LegacyAddress in netcoreapp2.0, it needs to be able to find a constructor where the
        /// parameter names are (case-insensitive) matches to the property names. "queue" works, "queueName" doesn't.
        /// </summary>
        [Test]
        public void LegacyAddressNamingToSupportNetCoreJsonSerializer()
        {
            var legacyAddress = typeof(LegacyAddress);
            var ignoreCase = StringComparison.InvariantCultureIgnoreCase;

            var extractedParams = legacyAddress.GetConstructors()
                .Select(ctor => ctor.GetParameters())
                .Select(plist => new
                {
                    List = plist,
                    QueueParam = plist.FirstOrDefault(pi => pi.Name.Equals(nameof(LegacyAddress.Queue), ignoreCase)),
                    MachineParam = plist.FirstOrDefault(pi => pi.Name.Equals(nameof(LegacyAddress.Machine), ignoreCase))
                })
                .Select(x => new
                {
                    x.QueueParam,
                    x.MachineParam,
                    Rest = x.List.Where(pi => pi != x.QueueParam && pi != x.MachineParam).ToArray()
                })
                .ToList();

            var valid = extractedParams
                .Where(x => x.QueueParam != null && x.MachineParam != null)
                .Where(x => x.Rest.All(pi => pi.IsOptional))
                .ToArray();

            Assert.AreEqual(1, valid.Length);
        }

        class LikeV3Subscriptions
        {
            public List<LegacyAddress> Clients { get; set; }
        }

        class LikeV4Subscriptions
        {
            public List<SubscriptionClient> Subscriptions { get; set; }
            public List<LegacyAddress> Clients { get; set; }

        }
    }
}
