namespace NServiceBus.Unicast.Subscriptions.RavenDB
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.RavenDB.Persistence.SubscriptionStorage;
    using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;
    using Raven.Client;

    interface ISubscriptionAccess
    {
        Task Subscribe(MessageType messageType, SubscriptionClient subscriptionClient, IAsyncDocumentSession session);
        Task Unsubscribe(MessageType messageType, SubscriptionClient subscriptionClient, IAsyncDocumentSession session);
        Task<IEnumerable<Subscriber>> GetSubscriberAddressesForMessage(IReadOnlyCollection<MessageType> messageTypes, ContextBag context, IAsyncDocumentSession session);
    }
}