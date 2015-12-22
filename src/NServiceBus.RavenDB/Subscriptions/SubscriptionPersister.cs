namespace NServiceBus.Unicast.Subscriptions.RavenDB
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.RavenDB.Persistence.SubscriptionStorage;
    using NServiceBus.Routing;
    using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;
    using Raven.Abstractions.Exceptions;
    using Raven.Client;

    class SubscriptionPersister : ISubscriptionStorage
    {
        public SubscriptionPersister(IDocumentStore store)
        {
            documentStore = store;
        }

        public async Task Subscribe(Subscriber subscriber, IReadOnlyCollection<MessageType> messageTypes, ContextBag context)
        {
            //When the subscriber is running V6 and UseLegacyMessageDrivenSubscriptionMode is enabled at the subscriber the 'subscriber.Endpoint' value is null
            var endpoint = subscriber.Endpoint?.ToString() ?? subscriber.TransportAddress.Split('@').First();
            var subscriptionClient = new SubscriptionClient { TransportAddress = subscriber.TransportAddress, Endpoint = endpoint };

            using (var session = OpenAsyncSession())
            {
                foreach (var messageType in messageTypes)
                {
                    await PersistIndividualDocument(messageType, subscriptionClient, session);
                    await TrySavingLegacySubscriptions(messageType, subscriptionClient, session);
                }
            }
        }

        private async Task TrySavingLegacySubscriptions(MessageType messageType, SubscriptionClient subscriptionClient, IAsyncDocumentSession session)
        {
            if (NoLegacyDocumentExists(messageType, session))
            {
                return;
            }

            var attempts = 0;
            do
            {
                try
                {
                    await PersistToAggregateDocument(messageType, subscriptionClient, session);
                    break;
                }
                catch (ConcurrencyException)
                {
                    attempts++;
                }
            } while (attempts < 5);
        }

        private static async Task PersistIndividualDocument(MessageType messageType, SubscriptionClient subscriptionClient, IAsyncDocumentSession session)
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

            await session.SaveChangesAsync().ConfigureAwait(false);
        }

        private async Task PersistToAggregateDocument(MessageType messageType, SubscriptionClient subscriptionClient, IAsyncDocumentSession session)
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

            await session.SaveChangesAsync().ConfigureAwait(false);
        }

        public async Task Unsubscribe(Subscriber subscriber, IReadOnlyCollection<MessageType> messageTypes, ContextBag context)
        {
            var subscriptionClient = new SubscriptionClient { TransportAddress = subscriber.TransportAddress, Endpoint = subscriber.Endpoint.ToString() };

            using (var session = OpenAsyncSession())
            {
                foreach (var messageType in messageTypes)
                {
                    await RemoveSubscriptionDocument(messageType, subscriptionClient, session);
                    await TryUnsubscribingFromLegacy(messageType, subscriptionClient, session);
                }

                await session.SaveChangesAsync().ConfigureAwait(false);
            }
        }

        private async Task TryUnsubscribingFromLegacy(MessageType messageType, SubscriptionClient subscriptionClient, IAsyncDocumentSession session)
        {
            if (NoLegacyDocumentExists(messageType, session))
            {
                return;
            }

            var attempts = 0;
            do
            {
                try
                {
                    await UnsubscribeFromAggregateDocument(messageType, subscriptionClient, session);
                    break;
                }
                catch (ConcurrencyException)
                {
                    attempts++;
                }
            } while (attempts < 5);
        }

        private static bool NoLegacyDocumentExists(MessageType messageType, IAsyncDocumentSession session)
        {
            var legacySubscriptionDocId = Subscription.FormatId(messageType);
            //We load metadata to avoid loading the entire document if we don't have to
            var legacyDocumentMetadata = session.Advanced.DocumentStore.DatabaseCommands.Head(legacySubscriptionDocId);

            if (legacyDocumentMetadata == null)
            {
                // There is no need to support the legacy format
                return true;
            }
            return false;
        }

        private static async Task RemoveSubscriptionDocument(MessageType messageType, SubscriptionClient subscriptionClient, IAsyncDocumentSession session)
        {
            var subscriptionDocId = SubscriptionDocument.FormatId(messageType, subscriptionClient);

            session.Delete(subscriptionDocId);

            await session.SaveChangesAsync().ConfigureAwait(false);
        }

        private async Task UnsubscribeFromAggregateDocument(MessageType messageType, SubscriptionClient subscriptionClient, IAsyncDocumentSession session)
        {
            var subscriptionDocId = Subscription.FormatId(messageType);

            var subscription = await session.LoadAsync<Subscription>(subscriptionDocId).ConfigureAwait(false);

            if (subscription.Subscribers.Contains(subscriptionClient))
            {
                subscription.Subscribers.Remove(subscriptionClient);
            }
            await session.SaveChangesAsync().ConfigureAwait(false);
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
    }
}