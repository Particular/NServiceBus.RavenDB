namespace NServiceBus.Unicast.Subscriptions.RavenDB
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Extensibility;
    using NServiceBus.RavenDB.Persistence.SubscriptionStorage;
    using MessageDrivenSubscriptions;
    using Raven.Abstractions.Exceptions;
    using Raven.Client;

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

        public async Task Unsubscribe(Subscriber subscriber, IReadOnlyCollection<MessageType> messageTypes, ContextBag context)
        {
            await TrySubscriptionMethod(subscriber, messageTypes, subscriptionAccess.Unsubscribe);
        }

        public async Task<IEnumerable<Subscriber>> GetSubscriberAddressesForMessage(IReadOnlyCollection<MessageType> messageTypes, ContextBag context)
        {
            using (var session = OpenAsyncSession())
            {
                return await subscriptionAccess.GetSubscriberAddressesForMessage(messageTypes, context, session);
            }
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
            } while (attempts < 5);
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