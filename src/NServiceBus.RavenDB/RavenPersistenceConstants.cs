namespace NServiceBus.RavenDB.Persistence
{
    using System;
    using System.Security.Cryptography;
    using System.Text;
    using Config;

    static class RavenPersistenceConstants
    {
        public const int DefaultPort = 8080;

        public static string GetDefaultUrl(Configure config)
        {
            var masterNode = GetMasterNode(config);

                if (string.IsNullOrEmpty(masterNode))
                {
                    masterNode = "localhost";
                }

                return string.Format("http://{0}:{1}", masterNode, DefaultPort);
        }

        static string GetMasterNode(Configure config)
        {
            var section = config.Settings.GetConfigSection<MasterNodeConfig>();
            return section != null ? section.Node : null;
        }

        public static Guid DefaultResourceManagerId(Configure config)
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