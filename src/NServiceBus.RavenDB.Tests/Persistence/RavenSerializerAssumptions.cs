namespace NServiceBus.RavenDB.Tests.Persistence
{
    using System.Collections.Generic;
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

                var sub = (RavenJObject)subscriptions[0];
                Assert.AreEqual("C", sub["Endpoint"].Value<string>());
                Assert.AreEqual("D", sub["TransportAddress"].Value<string>());
            }
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
