namespace NServiceBus.Unicast.Subscriptions.RavenDB
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Extensibility;
    using NServiceBus.RavenDB.Persistence.SubscriptionStorage;
    using NServiceBus.Routing;
    using MessageDrivenSubscriptions;
    using Raven.Abstractions.Exceptions;
    using Raven.Client;

    interface ISubscriptionAccess
    {
        Task Subscribe(MessageType messageType, SubscriptionClient subscriptionClient, IAsyncDocumentSession session);
        Task Unsubscribe(MessageType messageType, SubscriptionClient subscriptionClient, IAsyncDocumentSession session);
    }

    class IndividualSubscriptionDocumentAccess : ISubscriptionAccess
    {
        public async Task Subscribe(MessageType messageType, SubscriptionClient subscriptionClient, IAsyncDocumentSession session)
        {
            var subscriptionDocId = SubscriptionDocument.FormatId(messageType, subscriptionClient);

            var subscription = await session.LoadAsync<SubscriptionDocument>(subscriptionDocId).ConfigureAwait(false);

            if (subscription == null)
            {
                subscription = new SubscriptionDocument
                {
                    Id = subscriptionDocId,
                    MessageType = messageType,
                    SubscriptionClient = subscriptionClient
                };

                await session.StoreAsync(subscription).ConfigureAwait(false);
            }
        }

        public Task Unsubscribe(MessageType messageType, SubscriptionClient subscriptionClient, IAsyncDocumentSession session)
        {
            var subscriptionDocId = SubscriptionDocument.FormatId(messageType, subscriptionClient);

            session.Delete(subscriptionDocId);

            return Task.FromResult(0);
        }
    }

    class AggregateSubscriptionDocumentAccess : ISubscriptionAccess
    {
        public async Task Unsubscribe(MessageType messageType, SubscriptionClient subscriptionClient, IAsyncDocumentSession session)
        {
            var subscriptionDocId = Subscription.FormatId(messageType);

            var subscription = await session.LoadAsync<Subscription>(subscriptionDocId).ConfigureAwait(false);

            if (subscription.Subscribers.Contains(subscriptionClient))
            {
                subscription.Subscribers.Remove(subscriptionClient);
            }
        }

        public async Task Subscribe(MessageType messageType, SubscriptionClient subscriptionClient, IAsyncDocumentSession session)
        {
            var subscriptionDocId = Subscription.FormatId(messageType);

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
        }

    }

    class SubscriptionPersister : ISubscriptionStorage
    {
        public SubscriptionPersister(IDocumentStore store, ISubscriptionAccess access)
        {
            documentStore = store;
            subscriptionAccess = access;
        }

        public async Task Subscribe(Subscriber subscriber, IReadOnlyCollection<MessageType> messageTypes, ContextBag context)
        {
            //When the subscriber is running V6 and UseLegacyMessageDrivenSubscriptionMode is enabled at the subscriber the 'subscriber.Endpoint' value is null
            var endpoint = subscriber.Endpoint?.ToString() ?? subscriber.TransportAddress.Split('@').First();
            var subscriptionClient = new SubscriptionClient
            {
                TransportAddress = subscriber.TransportAddress,
                Endpoint = endpoint
            };

            var attempts = 0;
            // Remove this do while and try catch when we eliminate AggregateSubscriptionDocumentAccess
            do
            {
                try
                {
                    using (var session = OpenAsyncSession())
                    {
                        foreach (var messageType in messageTypes)
                        {
                            await subscriptionAccess.Subscribe(messageType, subscriptionClient, session);
                        }

                        await session.SaveChangesAsync().ConfigureAwait(false);
                    }
                }
                catch (ConcurrencyException)
                {
                    attempts++;
                }
            } while (attempts < 5);
        }


        public async Task Unsubscribe(Subscriber subscriber, IReadOnlyCollection<MessageType> messageTypes, ContextBag context)
        {
            var subscriptionClient = new SubscriptionClient
            {
                TransportAddress = subscriber.TransportAddress,
                Endpoint = subscriber.Endpoint.ToString()
            };

            using (var session = OpenAsyncSession())
            {
                foreach (var messageType in messageTypes)
                {
                    await subscriptionAccess.Unsubscribe(messageType, subscriptionClient, session);
                }
            }
        }

        public async Task<IEnumerable<Subscriber>> GetSubscriberAddressesForMessage(IReadOnlyCollection<MessageType> messageTypes, ContextBag context)
        {
            var ids = messageTypes.Select(Subscription.FormatId).ToList();

            using (var session = OpenAsyncSession())
            {
                var subscriptions = await session.LoadAsync<Subscription>(ids).ConfigureAwait(false);

                return subscriptions.Where(s => s != null)
                                    .SelectMany(s => s.Subscribers)
                                    .Distinct()
                                    .Select(c => new Subscriber(c.TransportAddress, new Endpoint(c.Endpoint)));
            }
        }

        IAsyncDocumentSession OpenAsyncSession()
        {
            var session = documentStore.OpenAsyncSession();
            session.Advanced.AllowNonAuthoritativeInformation = false;
            session.Advanced.UseOptimisticConcurrency = true;
            return session;
        }

        IDocumentStore documentStore;
        ISubscriptionAccess subscriptionAccess;
    }
}