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

        public static string FormatId(MessageType messageType)
        {
            // use MD5 hash to get a 16-byte hash of the string
            using (var provider = new MD5CryptoServiceProvider())
            {
                var inputBytes = Encoding.Default.GetBytes(messageType.TypeName + "/" + messageType.Version.Major);
                var hashBytes = provider.ComputeHash(inputBytes);
                // generate a guid from the hash:
                var id = new Guid(hashBytes);

                return $"Subscriptions/{id}";
            }
        }
    }
}