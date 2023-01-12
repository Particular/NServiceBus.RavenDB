namespace NServiceBus.Persistence.RavenDB
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading;
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
        public SubscriptionPersister(IDocumentStore store, bool useClusterWideTransactions)
        {
            documentStore = store;
            this.useClusterWideTransactions = useClusterWideTransactions;
        }

        public TimeSpan AggressiveCacheDuration { get; set; }

        public bool DisableAggressiveCaching { get; set; }

        public async Task Subscribe(Subscriber subscriber, MessageType messageType, ContextBag context, CancellationToken cancellationToken = default)
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
                    using var session = OpenAsyncSession();
                    var subscriptionDocId = GetDocumentIdForMessageType(messageType);

                    var subscription = await session.LoadAsync<Subscription>(subscriptionDocId, cancellationToken).ConfigureAwait(false);

                    if (subscription == null)
                    {
                        subscription = new Subscription
                        {
                            Id = subscriptionDocId,
                            MessageType = messageType,
                            Subscribers = new List<SubscriptionClient>()
                        };

                        await session.StoreAsync(subscription, cancellationToken).ConfigureAwait(false);
                        session.StoreSchemaVersionInMetadata(subscription);
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

                    await session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                    return;
                }
                catch (ConcurrencyException)
                {
                    attempts++;
                }
            }
            while (attempts < 5);
        }

        public async Task Unsubscribe(Subscriber subscriber, MessageType messageType, ContextBag context, CancellationToken cancellationToken = default)
        {
            using var session = OpenAsyncSession();
            var subscriptionDocId = GetDocumentIdForMessageType(messageType);

            var subscription = await session.LoadAsync<Subscription>(subscriptionDocId, cancellationToken).ConfigureAwait(false);

            if (subscription == null)
            {
                return;
            }

            var subscriptionClient = new SubscriptionClient { TransportAddress = subscriber.TransportAddress, Endpoint = subscriber.Endpoint };
            if (subscription.Subscribers.Contains(subscriptionClient))
            {
                subscription.Subscribers.Remove(subscriptionClient);
            }

            await session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task<IEnumerable<Subscriber>> GetSubscriberAddressesForMessage(IEnumerable<MessageType> messageTypes, ContextBag context, CancellationToken cancellationToken = default)
        {
            var ids = messageTypes.Select(GetDocumentIdForMessageType).ToList();

            using var suppressTransaction = new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled);
            Subscriber[] subscribers;
            using (var session = OpenAsyncSession())
            {
                using IDisposable aggressiveCachingScope = await CreateAggressiveCachingScope(session).ConfigureAwait(false);
                var subscriptions = await session.LoadAsync<Subscription>(ids, cancellationToken).ConfigureAwait(false);

                subscribers = subscriptions.Values.Where(s => s != null)
                    .SelectMany(s => s.Subscribers)
                    .Distinct()
                    .Select(c => new Subscriber(c.TransportAddress, c.Endpoint))
                    .ToArray();
            }

            suppressTransaction.Complete();
            return subscribers;
        }

        static string GetDocumentIdForMessageType(MessageType messageType)
        {
            using var provider = SHA1.Create();
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

#pragma warning disable PS0018
        ValueTask<IDisposable> CreateAggressiveCachingScope(IAsyncDocumentSession session) =>
#pragma warning restore PS0018
            DisableAggressiveCaching
                ? new ValueTask<IDisposable>(EmptyDisposable.Instance)
                : session.Advanced.DocumentStore.AggressivelyCacheForAsync(AggressiveCacheDuration);

        IAsyncDocumentSession OpenAsyncSession()
        {
            var sessionOptions = new SessionOptions
            {
                TransactionMode = useClusterWideTransactions ? TransactionMode.ClusterWide : TransactionMode.SingleNode
            };
            var session = documentStore.OpenAsyncSession(sessionOptions);
            if (!useClusterWideTransactions)
            {
                session.Advanced.UseOptimisticConcurrency = true;
            }

            return session;
        }

        IDocumentStore documentStore;
        bool useClusterWideTransactions;

        sealed class EmptyDisposable : IDisposable
        {
            EmptyDisposable()
            {
            }

            public void Dispose()
            {
            }

            public static readonly EmptyDisposable Instance = new EmptyDisposable();
        }
    }
}