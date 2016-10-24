namespace NServiceBus.Persistence.SubscriptionStorage
{
    using System;
    using System.Security.Cryptography;
    using System.Text;
    using Unicast.Subscriptions;
    using Unicast.Subscriptions.MessageDrivenSubscriptions;

    class SubscriptionData
    {
        public string Id { get; set; }
        public string TransportAddress { get; set; }
        public string Endpoint { get; set; }
        //TODO: do we need more info here, e.g. MessageType or is that sufficient?

        public static string FormatId(MessageType messageType, Subscriber subscriber)
        {
            // use MD5 hash to get a 16-byte hash of the string
            using (var provider = new MD5CryptoServiceProvider())
            {
                var inputBytes = Encoding.Default.GetBytes(subscriber.TransportAddress);
                var hashBytes = provider.ComputeHash(inputBytes);
                // generate a guid from the hash:
                var id = new Guid(hashBytes);

                return $"SubscriptionDatas/{messageType.TypeName}/{id}";
            }
        }
    }
}
