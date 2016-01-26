namespace NServiceBus.RavenDB.Persistence.SubscriptionStorage
{
    using System.Collections.Generic;
    using NServiceBus.Unicast.Subscriptions;
    using Raven.Imports.Newtonsoft.Json;

    class SubscriptionDocument
    {
        public string Id { get; set; }

        [JsonConverter(typeof(MessageTypeConverter))]
        public MessageType MessageType { get; set; }

        public SubscriptionClient SubscriptionClient { get; set; }

        public static List<string> PossibleIdPrefixes(MessageType messageType)
        {
            var possibleIdPrefix = new List<string>();

            return possibleIdPrefix;
        }

        public static string IdStart(MessageType messageType)
        {
            return $"Subscription/{messageType.TypeName}/{messageType.Version.Major}/";
        }

        public static string FormatId(MessageType messageType, SubscriptionClient client)
        {
            return IdStart(messageType) + $"{client.Endpoint}";
        }
    }
}