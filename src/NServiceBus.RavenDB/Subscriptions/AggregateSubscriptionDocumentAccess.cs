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

    class AggregateSubscriptionDocumentAccess : ISubscriptionAccess
    {
        public async Task Subscribe(MessageType messageType, SubscriptionClient subscriptionClient, IAsyncDocumentSession session)
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
        }

        public async Task Unsubscribe(MessageType messageType, SubscriptionClient subscriptionClient, IAsyncDocumentSession session)
        {
            var subscriptionDocId = Subscription.FormatId(messageType);

            var subscription = await session.LoadAsync<Subscription>(subscriptionDocId).ConfigureAwait(false);

            if (subscription.Subscribers.Contains(subscriptionClient))
            {
                subscription.Subscribers.Remove(subscriptionClient);
            }
        }

        public async Task<IEnumerable<Subscriber>> GetSubscriberAddressesForMessage(IReadOnlyCollection<MessageType> messageTypes, ContextBag context, IAsyncDocumentSession session)
        {
            var ids = messageTypes.Select(Subscription.FormatId).ToList();

            var subscriptions = await session.LoadAsync<Subscription>(ids).ConfigureAwait(false);

            return subscriptions.Where(s => s != null)
                .SelectMany(s => s.Subscribers)
                .Distinct()
                .Select(c => new Subscriber(c.TransportAddress, new Endpoint(c.Endpoint)));
        }
    }
}