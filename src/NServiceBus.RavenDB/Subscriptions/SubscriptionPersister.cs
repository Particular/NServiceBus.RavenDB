namespace NServiceBus.Unicast.Subscriptions.RavenDB
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Transactions;
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

        public Task Subscribe(string client, IEnumerable<MessageType> messageTypes, ContextBag context)
        {
            var messageTypeLookup = messageTypes.ToDictionary(Subscription.FormatId);

            var attempts = 0;

            //note: since we have a design that can run into concurrency exceptions we perform a few retries
            // we should redesign this in the future to use a separate doc per subscriber and message type
            using (new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled))
            {
                do
                {
                    try
                    {
                        using (var session = OpenSession())
                        {
                            session.Advanced.UseOptimisticConcurrency = true;

                            var existingSubscriptions = GetSubscriptions(messageTypeLookup.Values, session).ToLookup(m => m.Id);

                            var newAndExistingSubscriptions = messageTypeLookup
                                .Select(id => existingSubscriptions[id.Key].SingleOrDefault() ?? StoreNewSubscription(session, id.Key, id.Value))
                                .Where(subscription => subscription.Clients.All(c => c != client)).ToArray();

                            foreach (var subscription in newAndExistingSubscriptions)
                            {
                                subscription.Clients.Add(client);
                            }

                            session.SaveChanges();
                        }

                        return Task.FromResult(0);
                    }
                    catch (ConcurrencyException)
                    {
                        attempts++;
                    }
                }
                while (attempts < 5);
            }


            return Task.FromResult(0);
        }

        public Task Unsubscribe(string client, IEnumerable<MessageType> messageTypes, ContextBag context)
        {
            using (var session = OpenSession())
            {
                session.Advanced.UseOptimisticConcurrency = true;

                var subscriptions = GetSubscriptions(messageTypes, session);

                foreach (var subscription in subscriptions)
                {
                    subscription.Clients.Remove(client);
                }

                session.SaveChanges();
            }
            return Task.FromResult(0);
        }

        public Task<IEnumerable<string>> GetSubscriberAddressesForMessage(IEnumerable<MessageType> messageTypes, ContextBag context)
        {
            using (var session = OpenSession())
            {
                return Task.FromResult(GetSubscriptions(messageTypes, session)
                    .SelectMany(s => s.Clients)
                    .Distinct());
            }
        }

        IDocumentSession OpenSession()
        {
            var session = documentStore.OpenSession();
            session.Advanced.AllowNonAuthoritativeInformation = false;
            return session;
        }

        static IEnumerable<Subscription> GetSubscriptions(IEnumerable<MessageType> messageTypes, IDocumentSession session)
        {
            var ids = messageTypes
                .Select(Subscription.FormatId);

            return session.Load<Subscription>(ids).Where(s => s != null);
        }

        static Subscription StoreNewSubscription(IDocumentSession session, string id, MessageType messageType)
        {
            var subscription = new Subscription
            {
                Clients = new List<string>(),
                Id = id,
                MessageType = messageType
            };
            session.Store(subscription);

            return subscription;
        }

        readonly IDocumentStore documentStore;
    }
}