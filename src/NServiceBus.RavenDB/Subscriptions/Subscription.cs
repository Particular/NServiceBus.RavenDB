namespace NServiceBus.RavenDB.Persistence.SubscriptionStorage
{
    using NServiceBus.Persistence.RavenDB;
    using NServiceBus.Unicast.Subscriptions;
    using System.Collections.Generic;
    using Newtonsoft.Json;

    class Subscription
    {
        public string Id { get; set; }

        [JsonConverter(typeof(MessageTypeConverter))]
// ReSharper disable once UnusedAutoPropertyAccessor.Global
        public MessageType MessageType { get; set; }

        List<SubscriptionClient> subscribers;
        List<LegacyAddress> legacySubscriptions;

        public List<SubscriptionClient> Subscribers
        {
            get
            {
                if (subscribers == null)
                {
                    subscribers = new List<SubscriptionClient>();
                }
                return subscribers;
            }
            set { subscribers = value; }
        }

        [JsonProperty("Clients")]
        public List<LegacyAddress> LegacySubscriptions
        {
            get
            {
                if (legacySubscriptions == null)
                {
                    legacySubscriptions = new List<LegacyAddress>();
                }
                return legacySubscriptions;
            }
        }
    }
}