namespace NServiceBus.Unicast.Subscriptions.RavenDB
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.RavenDB.Persistence.SubscriptionStorage;
    using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;
    using Raven.Abstractions.Exceptions;
    using Raven.Client;

    class SubscriptionPersister : ISubscriptionStorage
    {
        public SubscriptionPersister(IDocumentStore store)
        {
            documentStore = store;
        }

        public async Task Subscribe(string client, IEnumerable<MessageType> messageTypes, ContextBag context)
        {
            var attempts = 0;

            var msgTypes = messageTypes.ToList();

            //note: since we have a design that can run into concurrency exceptions we perform a few retries
            // we should redesign this in the future to use a separate doc per subscriber and message type
            do
            {
                try
                {
                    using (var session = OpenSession())
                    {
                        foreach (var messageType in msgTypes)
                        {
                            var subscriptionDocId = Subscription.FormatId(messageType);

                            var subscription = await session.LoadAsync<Subscription>(subscriptionDocId).ConfigureAwait(false);

                            if (subscription == null)
                            {
                                subscription = new Subscription
                                {
                                    Id = subscriptionDocId,
                                    MessageType = messageType,
                                    Clients = new List<string>()
                                };
                                await session.StoreAsync(subscription).ConfigureAwait(false);
                            }

                            if (!subscription.Clients.Contains(client))
                            {
                                subscription.Clients.Add(client);
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

        public async Task Unsubscribe(string client, IEnumerable<MessageType> messageTypes, ContextBag context)
        {
            using (var session = OpenSession())
            {
                foreach (var messageType in messageTypes)
                {
                    var subscriptionDocId = Subscription.FormatId(messageType);

                    var subscription = await session.LoadAsync<Subscription>(subscriptionDocId).ConfigureAwait(false);

                    if (subscription == null)
                    {
                        continue;
                    }

                    if (subscription.Clients.Contains(client))
                    {
                        subscription.Clients.Remove(client);
                    }
                }

                await session.SaveChangesAsync().ConfigureAwait(false);
            }
        }

        public async Task<IEnumerable<string>> GetSubscriberAddressesForMessage(IEnumerable<MessageType> messageTypes, ContextBag context)
        {
            var ids = messageTypes.Select(Subscription.FormatId)
                .ToList();

            using (var session = OpenSession())
            {
                var subscriptions = await session.LoadAsync<Subscription>(ids).ConfigureAwait(false);

                return subscriptions.Where(s => s != null)
                    .SelectMany(s => s.Clients)
                    .Distinct();
            }
        }

        IAsyncDocumentSession OpenSession()
        {
            var session = documentStore.OpenAsyncSession();
            session.Advanced.AllowNonAuthoritativeInformation = false;
            session.Advanced.UseOptimisticConcurrency = true;
            return session;
        }

        IDocumentStore documentStore;
    }
}