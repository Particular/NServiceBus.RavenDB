namespace NServiceBus.RavenDB.Persistence.SubscriptionStorage
{
    using NServiceBus.Unicast.Subscriptions;

    internal interface ISubscriptionIdFormatter
    {
        string FormatId(MessageType messageType);
    }
}