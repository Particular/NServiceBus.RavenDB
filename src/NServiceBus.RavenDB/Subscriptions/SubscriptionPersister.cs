namespace NServiceBus.Unicast.Subscriptions.RavenDB
{
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.RavenDB.Persistence.SubscriptionStorage;
    using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;
    using Raven.Client;

    class SubscriptionPersister : ISubscriptionStorage
    {
        readonly IDocumentStore documentStore;

        public SubscriptionPersister(IDocumentStore store)
        {
            documentStore = store;
        }

        public void Subscribe(string client, IEnumerable<MessageType> messageTypes, SubscriptionStorageOptions options)
        {
            var messageTypeLookup = messageTypes.ToDictionary(Subscription.FormatId);

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
        }

        public void Unsubscribe(string client, IEnumerable<MessageType> messageTypes, SubscriptionStorageOptions options)
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
    }
}