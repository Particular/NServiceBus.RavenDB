namespace NServiceBus.RavenDB
{
    using System;
    using System.Security.Cryptography;
    using System.Text;
    using NServiceBus;
    using Persistence;
    using Raven.Client.Document;

    /// <summary>
    /// Extension methods to configure RavenDB persister.
    /// </summary>
    static class ConfigureRavenPersistence
    {
        /// <summary>
        /// Apply the NServiceBus conventions to a <see cref="DocumentStore"/> .
        /// </summary>
        static Configure ApplyRavenDBConventions(this Configure config, DocumentStore documentStore)
        {
            documentStore.ResourceManagerId = DefaultResourceManagerId(config);


            if (config.Settings.Get<bool>("Transactions.SuppressDistributedTransactions"))
            {
                documentStore.EnlistInDistributedTransactions = false;
            }

            return config;
        }

        static Guid DefaultResourceManagerId(Configure config)
        {
            var resourceManagerId = Address.Local + "-" + "foo";// TODO Configure.DefineEndpointVersionRetriever();

            return DeterministicGuidBuilder(resourceManagerId);
        }

        static Guid DeterministicGuidBuilder(string input)
        {
            // use MD5 hash to get a 16-byte hash of the string
            using (var provider = new MD5CryptoServiceProvider())
            {
                var inputBytes = Encoding.Default.GetBytes(input);
                var hashBytes = provider.ComputeHash(inputBytes);
                // generate a guid from the hash:
                return new Guid(hashBytes);
            }
        }
    }
}