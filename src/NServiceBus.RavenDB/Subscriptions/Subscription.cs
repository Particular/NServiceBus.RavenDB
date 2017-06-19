namespace NServiceBus.RavenDB.Persistence.SubscriptionStorage
{
    using System;
    using System.Collections.Generic;
    using System.Security.Cryptography;
    using System.Text;
    using NServiceBus.Persistence.RavenDB;
    using NServiceBus.Unicast.Subscriptions;
    using Raven.Imports.Newtonsoft.Json;

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

        //setting doNotUseVersionInSubscriptionId top false as it's the default behavior
        public static string FormatId(MessageType messageType, bool doNotUseVersionInSubscriptionId = false)
        {
            // use MD5 hash to get a 16-byte hash of the string
            using (var provider = new MD5CryptoServiceProvider())
            {
                var inputString = doNotUseVersionInSubscriptionId ?
                    messageType.TypeName
                    : messageType.TypeName + "/" + messageType.Version.Major;
                var inputBytes = Encoding.Default.GetBytes(inputString);
                var hashBytes = provider.ComputeHash(inputBytes);
                // generate a guid from the hash:
                var id = new Guid(hashBytes);

                return $"Subscriptions/{id}";
            }
        }
    }
}