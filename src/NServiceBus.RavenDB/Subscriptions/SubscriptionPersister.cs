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

                await session.StoreAsync(subscription, subscriptionDocId).ConfigureAwait(false);
            }
        }

        public async Task Unsubscribe(MessageType messageType, SubscriptionClient subscriptionClient, IAsyncDocumentSession session)
        {
            var subscriptionDocId = SubscriptionDocument.FormatId(messageType, subscriptionClient);

            var subscriptionToDelete = await session.LoadAsync<SubscriptionDocument>(subscriptionDocId).ConfigureAwait(false);

            if (subscriptionToDelete != null)
            {
                session.Delete(subscriptionToDelete);
            }
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
            await TrySubscriptionMethod(subscriber, messageTypes, subscriptionAccess.Subscribe);
        }

        private async Task TrySubscriptionMethod(
            Subscriber subscriber,
            IReadOnlyCollection<MessageType> messageTypes,
            Func<MessageType, SubscriptionClient, IAsyncDocumentSession, Task> method)
        {
            // When subscriber is running V6 and UseLegacyMessageDrivenSubscriptionMode is enabled at the subscriber the 'subscriber.Endpoint' value is null
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
                            await method(messageType, subscriptionClient, session);
                        }

                        await session.SaveChangesAsync().ConfigureAwait(false);
                    }

                    return;
                }
                catch (ConcurrencyException)
                {
                    attempts++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    throw;
                }
            } while (attempts < 5);
        }


        public async Task Unsubscribe(Subscriber subscriber, IReadOnlyCollection<MessageType> messageTypes, ContextBag context)
        {
            await TrySubscriptionMethod(subscriber, messageTypes, subscriptionAccess.Unsubscribe);
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