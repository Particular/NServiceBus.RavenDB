namespace NServiceBus.RavenDB.Persistence.SubscriptionStorage
{
    using System;
    using System.Security.Cryptography;
    using System.Text;
    using Unicast.Subscriptions;

    class NonVersionedSubscriptionIdFormatter : ISubscriptionIdFormatter
    {
        public string FormatId(MessageType messageType)
        {
            // use MD5 hash to get a 16-byte hash of the string
            using (var provider = new MD5CryptoServiceProvider())
            {
                var inputBytes = Encoding.Default.GetBytes(messageType.TypeName);
                var hashBytes = provider.ComputeHash(inputBytes);
                // generate a guid from the hash:
                var id = new Guid(hashBytes);

                return $"Subscriptions/{id}";
            }
        }
    }
}
