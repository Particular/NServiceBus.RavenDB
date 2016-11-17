namespace NServiceBus.Persistence.RavenDB
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Transactions;
    using NServiceBus.Extensibility;
    using NServiceBus.RavenDB.Persistence.SubscriptionStorage;
    using NServiceBus.Unicast.Subscriptions;
    using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;
    using Raven.Abstractions.Exceptions;
    using Raven.Client;

    class SubscriptionPersister : ISubscriptionStorage
    {
        public SubscriptionPersister(IDocumentStore store)
        {
            documentStore = store;
            subscriptionCollectionName = store.Conventions.FindTypeTagName(typeof(Subscription));
        }

        public TimeSpan AggressiveCacheDuration { get; set; } = TimeSpan.FromMinutes(1);
        public bool DisableAggressiveCaching { get; set; }

        public async Task Subscribe(Subscriber subscriber, MessageType messageType, ContextBag context)
        {
            //When the subscriber is running V6 and UseLegacyMessageDrivenSubscriptionMode is enabled at the subscriber the 'subcriber.Endpoint' value is null
            var endpoint = subscriber.Endpoint ?? subscriber.TransportAddress.Split('@').First();
            var subscriptionClient = new SubscriptionClient { TransportAddress = subscriber.TransportAddress, Endpoint = endpoint };

            var attempts = 0;

            //note: since we have a design that can run into concurrency exceptions we perform a few retries
            // we should redesign this in the future to use a separate doc per subscriber and message type
            do
            {
                try
                {
                    await TrySubscribe(subscriber, messageType, subscriptionClient).ConfigureAwait(false);
                    return;
                }
                catch (ConcurrencyException)
                {
                    attempts++;
                }
            } while (attempts < 5);
        }

        async Task TrySubscribe(Subscriber subscriber, MessageType messageType, SubscriptionClient subscriptionClient)
        {
            using (var session = OpenAsyncSession())
            {
                var subscriptionDocId = Subscription.FormatVersionlessId(messageType);

                var subscriptionDocs = await session.Advanced
                    .AsyncDocumentQuery<Subscription>($"{subscriptionCollectionName}Index")
                    .NoCaching()
                    .WaitForNonStaleResultsAsOfLastWrite()
                    .Where($"MessageType: \"{messageType.TypeName}, Version=*\"")
                    .ToListAsync()
                    .ConfigureAwait(false);

                if (subscriptionDocs.All(sub => sub.Id != subscriptionDocId))
                {
                    var subscription = new Subscription
                    {
                        Id = subscriptionDocId,
                        MessageType = messageType,
                        Subscribers = new List<SubscriptionClient>()
                    };
                    subscriptionDocs.Add(subscription);

                    await session.StoreAsync(subscription).ConfigureAwait(false);
                }

                var subscribers = subscriptionDocs.SelectMany(doc => doc.Subscribers).Distinct().ToList();

                if (!subscribers.Contains(subscriptionClient))
                {
                    subscribers.Add(subscriptionClient);
                }
                else
                {
                    var savedSubscription = subscribers.Single(s => s.Equals(subscriptionClient));
                    if (savedSubscription.Endpoint != subscriber.Endpoint)
                    {
                        savedSubscription.Endpoint = subscriber.Endpoint;
                    }
                }

                foreach (var doc in subscriptionDocs)
                {
                    doc.Subscribers = subscribers;
                }

                await session.SaveChangesAsync().ConfigureAwait(false);
            }
        }

        public async Task Unsubscribe(Subscriber subscriber, MessageType messageType, ContextBag context)
        {
            var subscriptionClient = new SubscriptionClient { TransportAddress = subscriber.TransportAddress, Endpoint = subscriber.Endpoint };

            using (var session = OpenAsyncSession())
            {
                var subscriptionDocs = await session.Advanced
                    .AsyncDocumentQuery<Subscription>($"{subscriptionCollectionName}Index")
                    .NoCaching()
                    .WaitForNonStaleResultsAsOfLastWrite()
                    .Where($"MessageType: \"{messageType.TypeName}, Version=*\"")
                    .ToListAsync()
                    .ConfigureAwait(false);

                foreach (var doc in subscriptionDocs)
                {
                    // Uses overridden Equals that evaluates based on values
                    doc.Subscribers.RemoveAll(sub => sub.Equals(subscriptionClient));
                    if (doc.Subscribers.Count == 0)
                    {
                        session.Delete(doc);
                    }
                }

                await session.SaveChangesAsync().ConfigureAwait(false);
            }
        }


        public async Task<IEnumerable<Subscriber>> GetSubscriberAddressesForMessage(IEnumerable<MessageType> messageTypes, ContextBag context)
        {
            using (var suppressTransaction = new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled))
            {
                Subscriber[] subscribers;
                using (var session = OpenAsyncSession())
                {
                    using (ConfigureAggressiveCaching(session))
                    {
                        var lazyDocuments = new List<Lazy<Task<IEnumerable<Subscription>>>>();
                        foreach (var messageType in messageTypes)
                        {
                            var ret = session.Advanced
                                .AsyncDocumentQuery<Subscription>($"{subscriptionCollectionName}Index")
                                .Where($"MessageType: \"{messageType.TypeName}, Version=*\"")
                                .LazilyAsync(null);
                            lazyDocuments.Add(ret);
                        }
                        await session.Advanced.Eagerly.ExecuteAllPendingLazyOperationsAsync().ConfigureAwait(false);
                        subscribers = lazyDocuments
                            .Select(el => el.Value.Result)
                            .SelectMany(s => s)
                            .SelectMany(x => x.Subscribers)
                            .Distinct()
                            .Select(c => new Subscriber(c.TransportAddress, c.Endpoint))
                            .ToArray();
                    }
                }

                suppressTransaction.Complete();
                return subscribers;
            }
        }

        IDisposable ConfigureAggressiveCaching(IAsyncDocumentSession session)
        {
            if (DisableAggressiveCaching)
            {
                return EmptyDisposable.Instance;
            }

            return session.Advanced.DocumentStore.AggressivelyCacheFor(AggressiveCacheDuration);
        }

        class EmptyDisposable : IDisposable
        {
            EmptyDisposable()
            {
            }

            public void Dispose()
            {
            }

            public static readonly EmptyDisposable Instance = new EmptyDisposable();
        }

        IAsyncDocumentSession OpenAsyncSession()
        {
            var session = documentStore.OpenAsyncSession();
            session.Advanced.AllowNonAuthoritativeInformation = false;
            session.Advanced.UseOptimisticConcurrency = true;
            return session;
        }

        IDocumentStore documentStore;
        string subscriptionCollectionName;
    }
}