namespace NServiceBus.RavenDB.Tests.SubscriptionStorage
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Persistence.RavenDB;
    using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;
    using NUnit.Framework;
    using Raven.Client.Connection.Profiling;

    [TestFixture]
    public class SubscriptionCaching : RavenDBPersistenceTestBase
    {
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            requests = new List<RequestResultArgs>();

            store.JsonRequestFactory.LogRequest += (state, args) =>
            {
                var uri = new Uri(args.Url);
                if (uri.AbsolutePath.StartsWith("/changes"))
                {
                    return;
                }
                Console.WriteLine($"Observed Raven URL ({args.Status}) {uri}");
                requests.Add(args);
            };

            SubscriptionIndex.Create(store).GetAwaiter().GetResult();
        }

        [TestCase(false, RequestStatus.AggressivelyCached)]
        [TestCase(true, RequestStatus.Cached)]
        public async Task Subscription_queries_should_be_cached(bool disableAggressiveCache, RequestStatus expectedResultOnSubscriptionQueries)
        {
            persister = new SubscriptionPersister(store);
            persister.DisableAggressiveCaching = disableAggressiveCache;

            await persister.Subscribe(new Subscriber("TransportAddress1", "Endpoint1"), MessageTypes.MessageA, new ContextBag());
            await persister.Subscribe(new Subscriber("TransportAddress2", "Endpoint2"), MessageTypes.MessageA, new ContextBag());

            using (var session = store.OpenAsyncSession())
            {
                await session.StoreAsync(new RandomDoc(), "RandomDoc/test").ConfigureAwait(false);
                await session.SaveChangesAsync().ConfigureAwait(false);
            }
            WaitForIndexing(store);

            var messageTypes = new[]
            {
                MessageTypes.MessageA
            };

            Console.WriteLine("-- First subscriber query...");
            requests.Clear();
            var subscribers = await persister.GetSubscriberAddressesForMessage(messageTypes, new ContextBag()).ConfigureAwait(false);
            Assert.AreEqual(2, subscribers.Count());
            Assert.AreEqual(1, requests.Count);
            Assert.AreEqual(RequestStatus.SentToServer, requests[0].Status);

            Console.WriteLine($"-- Subsequent subscription queries, should be {expectedResultOnSubscriptionQueries}");
            for (var i = 0; i < 5; i++)
            {
                requests.Clear();
                var cachedSubs = await persister.GetSubscriberAddressesForMessage(messageTypes, new ContextBag()).ConfigureAwait(false);
                Assert.AreEqual(2, cachedSubs.Count());
                Assert.AreEqual(1, requests.Count);
                if (expectedResultOnSubscriptionQueries == RequestStatus.AggressivelyCached)
                {
                    Assert.AreEqual(RequestStatus.AggressivelyCached, requests[0].Status);
                }
            }

            Console.WriteLine("-- Random doc first query");
            using (var session = store.OpenAsyncSession())
            {
                requests.Clear();
                await session.LoadAsync<RandomDoc>("RandomDoc/test").ConfigureAwait(false);
                Assert.AreEqual(1, requests.Count);
                Assert.AreEqual(RequestStatus.SentToServer, requests[0].Status);
            }

            Console.WriteLine("-- Random doc, subsequent loads should be Cached, not AggressivelyCached");
            for (var i = 0; i < 5; i++)
            {
                using (var session = store.OpenAsyncSession())
                {
                    requests.Clear();
                    await session.LoadAsync<RandomDoc>("RandomDoc/test").ConfigureAwait(false);
                    Assert.AreEqual(1, requests.Count);
                    Assert.AreEqual(RequestStatus.Cached, requests[0].Status);
                }
            }
        }

        class RandomDoc { }

        SubscriptionPersister persister;
        List<RequestResultArgs> requests;
    }
}
