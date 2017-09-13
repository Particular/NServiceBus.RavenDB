namespace NServiceBus.RavenDB.Persistence.SubscriptionStorage
{
    using System;
    using System.Security.Cryptography;
    using System.Text;
    using Unicast.Subscriptions;

    class VersionedSubscriptionIdFormatter : ISubscriptionIdFormatter
    {
        public string FormatId(MessageType messageType)
        {
            // use MD5 hash to get a 16-byte hash of the string
            using (var provider = MD5.Create())
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
