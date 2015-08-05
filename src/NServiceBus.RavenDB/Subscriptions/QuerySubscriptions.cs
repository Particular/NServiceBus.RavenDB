namespace NServiceBus.Unicast.Subscriptions.RavenDB
{
    using System.Collections.Generic;
    using System.Linq;
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

        public IEnumerable<string> GetSubscriberAddressesForMessage(IEnumerable<MessageType> messageTypes)
        {
            using (var session = OpenSession())
            {
                return GetSubscriptions(messageTypes, session)
                    .SelectMany(s => s.Clients)
                    .Distinct();
            }
        }

        static IEnumerable<Subscription> GetSubscriptions(IEnumerable<MessageType> messageTypes, IDocumentSession session)
        {
            var ids = messageTypes
                .Select(Subscription.FormatId);

            return session.Load<Subscription>(ids).Where(s => s != null);
        }

        IDocumentSession OpenSession()
        {
            var session = documentStore.OpenSession();
            session.Advanced.AllowNonAuthoritativeInformation = false;
            return session;
        }
    }
}