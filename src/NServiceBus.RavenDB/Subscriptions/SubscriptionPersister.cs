namespace NServiceBus.Unicast.Subscriptions.RavenDB
{
    using System;
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

            //note: since we have a design that can run into concurrency exceptions we perform a few retries
            // we should redesign this in the future to use a separate doc per subscriber and message type
            do
            {
                try
                {
                    using (var session = OpenSession())
                    {
                        session.Advanced.UseOptimisticConcurrency = true;

                        foreach (var messageType in messageTypes.ToList())
                        {
                            var subscriptionDocId = Subscription.FormatId(messageType);

                            var subcription = await session.LoadAsync<Subscription>(subscriptionDocId).ConfigureAwait(false);

                            if (subcription == null)
                            {
                                subcription = new Subscription
                                {
                                    Id = subscriptionDocId,
                                    MessageType = messageType,
                                    Clients = new List<string>()
                                };
                                await session.StoreAsync(subcription).ConfigureAwait(false);
                            }


                            if (!subcription.Clients.Contains(client))
                            {
                                subcription.Clients.Add(client);
                            }
                        }
                        await session.SaveChangesAsync().ConfigureAwait(false);
                    }

                    return;
                }
                catch (ConcurrencyException)
                {
                    Console.Out.WriteLine("boom");

                    attempts++;
                }
            } while (attempts < 5);
        }

        public async Task Unsubscribe(string client, IEnumerable<MessageType> messageTypes, ContextBag context)
        {
            using (var session = OpenSession())
            {
                session.Advanced.UseOptimisticConcurrency = true;

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
            using (var session = OpenSession())
            {
                var subscriptions = await GetSubscriptions(messageTypes, session).ConfigureAwait(false);

                return subscriptions.SelectMany(s => s.Clients)
                     .Distinct();
            }
        }

        IAsyncDocumentSession OpenSession()
        {
            var session = documentStore.OpenAsyncSession();
            session.Advanced.AllowNonAuthoritativeInformation = false;
            return session;
        }

        static async Task<IEnumerable<Subscription>> GetSubscriptions(IEnumerable<MessageType> messageTypes, IAsyncDocumentSession session)
        {
            var ids = messageTypes
                .Select(Subscription.FormatId)
                .ToList();

            var result = await session.LoadAsync<Subscription>(ids).ConfigureAwait(false);

            return result.Where(s => s != null).ToList();
        }

        static async Task<Subscription> StoreNewSubscription(IAsyncDocumentSession session, string id, MessageType messageType)
        {
            var subscription = new Subscription
            {
                Clients = new List<string>(),
                Id = id,
                MessageType = messageType
            };
            await session.StoreAsync(subscription);

            return subscription;
        }

        readonly IDocumentStore documentStore;
    }
}