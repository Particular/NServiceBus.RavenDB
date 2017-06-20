namespace NServiceBus.RavenDB.Persistence.SubscriptionStorage
{
    using Unicast.Subscriptions;
    using System;
    using System.Security.Cryptography;
    using System.Text;

    class SubscriptionIdFormatter
    {
        bool useMessageVersionToGenerateSubscriptionId;
        public SubscriptionIdFormatter(bool useMessageVersionToGenerateSubscriptionId)
        {
            this.useMessageVersionToGenerateSubscriptionId = useMessageVersionToGenerateSubscriptionId;
        }

        public string FormatId(MessageType messageType)
        {
            // use MD5 hash to get a 16-byte hash of the string
            using (var provider = new MD5CryptoServiceProvider())
            {
                var inputString = useMessageVersionToGenerateSubscriptionId ?
                    messageType.TypeName + "/" + messageType.Version.Major
                    : messageType.TypeName;
                var inputBytes = Encoding.Default.GetBytes(inputString);
                var hashBytes = provider.ComputeHash(inputBytes);
                // generate a guid from the hash:
                var id = new Guid(hashBytes);

                return $"Subscriptions/{id}";
            }
        }
    }
}
