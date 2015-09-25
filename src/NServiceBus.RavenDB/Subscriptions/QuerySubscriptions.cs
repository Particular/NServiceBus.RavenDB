namespace NServiceBus.Unicast.Subscriptions.RavenDB
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.RavenDB.Persistence.SubscriptionStorage;
    using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;
    using Raven.Client;

    class QuerySubscriptions : IQuerySubscriptions
    {
        readonly IDocumentStore documentStore;

        public QuerySubscriptions(IDocumentStore store)
        {
            documentStore = store;
        }

        public async Task<IEnumerable<string>> GetSubscriberAddressesForMessage(IEnumerable<MessageType> messageTypes)
        {
            using (var session = OpenSession())
            {
                var subscriptions = await GetSubscriptions(messageTypes, session);
                return subscriptions
                    .Where(s => s != null)
                    .SelectMany(s => s.Clients)
                    .Distinct();
            }
        }

        static Task<Subscription[]> GetSubscriptions(IEnumerable<MessageType> messageTypes, IAsyncDocumentSession session)
        {
            var ids = messageTypes
                .Select(Subscription.FormatId);

            return session.LoadAsync<Subscription>(ids);
        }

        IAsyncDocumentSession OpenSession()
        {
            var session = documentStore.OpenAsyncSession();
            session.Advanced.AllowNonAuthoritativeInformation = false;
            return session;
        }
    }
}