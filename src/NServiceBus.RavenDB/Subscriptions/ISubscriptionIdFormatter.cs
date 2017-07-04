namespace NServiceBus.RavenDB.Persistence.SubscriptionStorage
{
    using NServiceBus.Unicast.Subscriptions;

    interface ISubscriptionIdFormatter
    {
        string FormatId(MessageType messageType);
    }
}