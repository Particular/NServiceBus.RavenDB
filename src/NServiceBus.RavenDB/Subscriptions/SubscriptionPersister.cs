namespace NServiceBus.Persistence.RavenDB
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading.Tasks;
    using System.Transactions;
    using NServiceBus.Extensibility;
    using NServiceBus.RavenDB.Persistence.SubscriptionStorage;
    using NServiceBus.Unicast.Subscriptions;
    using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;
    using Raven.Client.Documents;
    using Raven.Client.Documents.Session;
    using Raven.Client.Exceptions;

    class SubscriptionPersister : ISubscriptionStorage
    {
        public SubscriptionPersister(IDocumentStore store)
        {
            documentStore = store;
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
                    using (var session = OpenAsyncSession())
                    {
                        var subscriptionDocId = GetDocumentIdForMessageType(messageType);

                        var subscription = await session.LoadAsync<Subscription>(subscriptionDocId).ConfigureAwait(false);

                        if (subscription == null)
                        {
                            subscription = new Subscription
                            {
                                Id = subscriptionDocId,
                                MessageType = messageType,
                                Subscribers = new List<SubscriptionClient>()
                            };

                            await session.StoreAsync(subscription).ConfigureAwait(false);
                        }

                        if (!subscription.Subscribers.Contains(subscriptionClient))
                        {
                            subscription.Subscribers.Add(subscriptionClient);
                        }
                        else
                        {
                            var savedSubscription = subscription.Subscribers.Single(s => s.Equals(subscriptionClient));
                            if (savedSubscription.Endpoint != subscriber.Endpoint)
                            {
                                savedSubscription.Endpoint = subscriber.Endpoint;
                            }
                        }

                        await session.SaveChangesAsync().ConfigureAwait(false);
                    }

                    return;
                }
                catch (ConcurrencyException)
                {
                    attempts++;
                }
            } while (attempts < 5);
        }

        public async Task Unsubscribe(Subscriber subscriber, MessageType messageType, ContextBag context)
        {
            var subscriptionClient = new SubscriptionClient { TransportAddress = subscriber.TransportAddress, Endpoint = subscriber.Endpoint };

            using (var session = OpenAsyncSession())
            {
                var subscriptionDocId = GetDocumentIdForMessageType(messageType);

                var subscription = await session.LoadAsync<Subscription>(subscriptionDocId).ConfigureAwait(false);

                if (subscription == null)
                {
                    return;
                }

                if (subscription.Subscribers.Contains(subscriptionClient))
                {
                    subscription.Subscribers.Remove(subscriptionClient);
                }

                await session.SaveChangesAsync().ConfigureAwait(false);
            }
        }

        public async Task<IEnumerable<Subscriber>> GetSubscriberAddressesForMessage(IEnumerable<MessageType> messageTypes, ContextBag context)
        {
            var ids = messageTypes.Select(GetDocumentIdForMessageType).ToList();

            using (var suppressTransaction = new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled))
            {
                Subscriber[] subscribers;
                using (var session = OpenAsyncSession())
                {
                    using (ConfigureAggressiveCaching(session))
                    {
                        var subscriptions = await session.LoadAsync<Subscription>(ids).ConfigureAwait(false);

                        subscribers = subscriptions.Values.Where(s => s != null)
                            .SelectMany(s => s.Subscribers)
                            .Distinct()
                            .Select(c => new Subscriber(c.TransportAddress, c.Endpoint))
                            .ToArray();
                    }
                }

                suppressTransaction.Complete();
                return subscribers;
            }
        }

        static string GetDocumentIdForMessageType(MessageType messageType)
        {
            using (var provider = new SHA1CryptoServiceProvider())
            {
                var inputBytes = Encoding.UTF8.GetBytes(messageType.TypeName);
                var hashBytes = provider.ComputeHash(inputBytes);

                // 54ch for perf - "Subscriptions/" (14ch) + 40ch hash
                var idBuilder = new StringBuilder(54);

                idBuilder.Append("Subscriptions/");

                for (var i = 0; i < hashBytes.Length; i++)
                {
                    idBuilder.Append(hashBytes[i].ToString("x2"));
                }

                return idBuilder.ToString();
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
            session.Advanced.UseOptimisticConcurrency = true;
            return session;
        }

        IDocumentStore documentStore;
    }
}