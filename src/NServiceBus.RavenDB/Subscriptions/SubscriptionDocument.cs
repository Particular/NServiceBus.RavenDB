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

        public static string IdStart(MessageType messageType)
        {
            var startOfId = $"Subscription/{messageType.TypeName}/";

            return startOfId;
        }

        public static string FormatId(MessageType messageType, SubscriptionClient client)
        {
            var documentId = IdStart(messageType) + $"{client.Endpoint}/{messageType.Version.Major}";

            return documentId;
        }
    }
}