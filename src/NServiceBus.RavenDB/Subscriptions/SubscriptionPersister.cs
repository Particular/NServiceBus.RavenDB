namespace NServiceBus.Unicast.Subscriptions.RavenDB
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
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

        public async Task Subscribe(string client, IEnumerable<MessageType> messageTypes, SubscriptionStorageOptions options)
        {
            var messageTypeLookup = messageTypes.ToDictionary(Subscription.FormatId);

            using (var session = OpenSession())
            {
                session.Advanced.UseOptimisticConcurrency = true;

                var existingSubscriptions = (await GetSubscriptions(messageTypeLookup.Values, session)).Where(s => s != null).ToLookup(m => m.Id);

                var newAndExistingSubscriptions = new List<Subscription>();
                foreach (var messageType in messageTypeLookup)
                {
                    var subscription = existingSubscriptions[messageType.Key].SingleOrDefault() ?? (await StoreNewSubscription(session, messageType.Key, messageType.Value));
                    if (subscription.Clients.All(c => c != client))
                    {
                        newAndExistingSubscriptions.Add(subscription);
                    }
                }

                foreach (var subscription in newAndExistingSubscriptions)
                {
                    subscription.Clients.Add(client);
                }

                await session.SaveChangesAsync();
            }
        }

        public async Task Unsubscribe(string client, IEnumerable<MessageType> messageTypes, SubscriptionStorageOptions options)
        {
            using (var session = OpenSession())
            {
                session.Advanced.UseOptimisticConcurrency = true;

                var subscriptions = await GetSubscriptions(messageTypes, session);

                foreach (var subscription in subscriptions)
                {
                    subscription.Clients.Remove(client);
                }

                await session.SaveChangesAsync();
            }
        }

        IAsyncDocumentSession OpenSession()
        {
            var session = documentStore.OpenAsyncSession();
            session.Advanced.AllowNonAuthoritativeInformation = false;
            return session;
        }

        static Task<Subscription[]> GetSubscriptions(IEnumerable<MessageType> messageTypes, IAsyncDocumentSession session)
        {
            var ids = messageTypes
                .Select(Subscription.FormatId);

            return session.LoadAsync<Subscription>(ids);
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
    }
}