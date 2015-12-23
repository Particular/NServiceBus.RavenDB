namespace NServiceBus.Unicast.Subscriptions.RavenDB
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.RavenDB.Persistence.SubscriptionStorage;
    using NServiceBus.Routing;
    using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;
    using Raven.Client;

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

        public async Task<IEnumerable<Subscriber>> GetSubscriberAddressesForMessage(IReadOnlyCollection<MessageType> messageTypes, ContextBag context, IAsyncDocumentSession session)
        {
            var subscriptions = new List<SubscriptionDocument>();

            foreach (var messageType in messageTypes)
            {
                var startOfId = SubscriptionDocument.IdStart(messageType);
                subscriptions.AddRange(
                    await session.Advanced.LoadStartingWithAsync<SubscriptionDocument>(startOfId).ConfigureAwait(false)
                    );
            }

            return
                subscriptions
                    .Select(s => s.SubscriptionClient)
                    .Distinct()
                    .Select(c =>
                        new Subscriber(c.TransportAddress, new Endpoint(c.Endpoint))
                    );
        }
    }
}