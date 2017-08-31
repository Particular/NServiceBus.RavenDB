namespace NServiceBus.RavenDB.Persistence.SubscriptionStorage
{
    using System.Security.Cryptography;
    using System.Text;
    using Unicast.Subscriptions;

    class NonVersionedSubscriptionIdFormatter : ISubscriptionIdFormatter
    {
        public string FormatId(MessageType messageType)
        {
            using (var provider = SHA1.Create())
            {
                var inputBytes = Encoding.UTF8.GetBytes(messageType.TypeName);
                var hashBytes = provider.ComputeHash(inputBytes);

                // 54ch for perf - "Subscriptions/" (14ch) + 40ch hash
                var idBuilder = new StringBuilder(54);

                idBuilder.Append("Subscriptions/");

                for (var i = 0; i < hashBytes.Length; i++)
                {
                    idBuilder.Append(hashBytes[i].ToString("x2"));
                }

                return idBuilder.ToString();
            }
        }
    }
}
