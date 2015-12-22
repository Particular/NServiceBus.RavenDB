namespace NServiceBus.RavenDB.Persistence.SubscriptionStorage
{
    using NServiceBus.Unicast.Subscriptions;
    using Raven.Imports.Newtonsoft.Json;

    class SubscriptionDocument
    {
        public string Id { get; set; }

        [JsonConverter(typeof(MessageTypeConverter))]
        public MessageType MessageType { get; set; }

        public SubscriptionClient SubscriptionClient { get; set; }

        public static string FormatId(MessageType messageType, SubscriptionClient client)
        {
            var documentId = $"Subscription/{messageType.TypeName}/{client.Endpoint}/{messageType.Version.Major}";

            return documentId;
        }
    }
}