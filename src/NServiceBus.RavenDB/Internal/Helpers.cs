namespace NServiceBus.RavenDB.Internal
{
    using System;
    using System.Configuration;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using NServiceBus.Logging;
    using NServiceBus.Persistence;
    using NServiceBus.Settings;
    using Raven.Client;
    using Raven.Client.Document;
    using Raven.Client.Indexes;
    using Raven.Json.Linq;

    class Helpers
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(RavenDBPersistence));

        public static IDocumentStore CreateDocumentStoreByConnectionStringName(ReadOnlySettings settings, params string[] connectionStringNames)
        {
            var connectionStringName = GetFirstNonEmptyConnectionString(connectionStringNames);
            if (!string.IsNullOrWhiteSpace(connectionStringName))
            {
                var docStore = new DocumentStore
                {
                    ConnectionStringName = connectionStringName
                };
                if (docStore.DefaultDatabase == null)
                {
                    docStore.DefaultDatabase = settings.EndpointName().ToString();
                }
                ApplyRavenDBConventions(settings, docStore);

                return docStore.Initialize();
            }
            return null;
        }

        public static IDocumentStore CreateDocumentStoreByUrl(ReadOnlySettings settings, string url)
        {
            var docStore = new DocumentStore
            {
                Url = url
            };

            if (docStore.DefaultDatabase == null)
            {
                docStore.DefaultDatabase = settings.EndpointName().ToString();
            }

            ApplyRavenDBConventions(settings, docStore);

            return docStore.Initialize();
        }

        static void LogRavenConnectionFailure(Exception exception, IDocumentStore store)
        {
            var sb = new StringBuilder();
            sb.AppendFormat("RavenDB could not be contacted. We tried to access Raven using the following url: {0}.",
                store.Url);
            sb.AppendLine();
            sb.AppendFormat("Please ensure that you can open the Raven Studio by navigating to {0}.", store.Url);
            sb.AppendLine();
            sb.AppendLine(
                @"To configure NServiceBus to use a different Raven connection string add a connection string named ""NServiceBus.Persistence"" in your config file, example:");
            sb.AppendFormat(
                @"<connectionStrings>
    <add name=""NServiceBus.Persistence"" connectionString=""http://localhost:9090"" />
</connectionStrings>");
            sb.AppendLine("Original exception: " + exception);

            Logger.Warn(sb.ToString());
        }

        public static void VerifyConnectionToRavenDb(IDocumentStore documentStore)
        {
            try
            {
                documentStore.DatabaseCommands.Put("nsb/ravendb/testdocument", null, new RavenJObject(), new RavenJObject());
                documentStore.DatabaseCommands.Delete("nsb/ravendb/testdocument", null);
            }
            catch (Exception e)
            {
                LogRavenConnectionFailure(e, documentStore);
                throw;
            }
        }

        static string GetFirstNonEmptyConnectionString(params string[] connectionStringNames)
        {
            try
            {
                return connectionStringNames.FirstOrDefault(cstr => ConfigurationManager.ConnectionStrings[cstr] != null);
            }
            catch (ConfigurationErrorsException)
            {
                return null;
            }
        }

        /// <summary>
        ///     Apply the NServiceBus conventions to a <see cref="DocumentStore" /> .
        /// </summary>
        public static void ApplyRavenDBConventions(ReadOnlySettings settings, IDocumentStore documentStore)
        {
            documentStore.Conventions.FindTypeTagName = BackwardsCompatibilityHelper.LegacyFindTypeTagName;

            var store = documentStore as DocumentStore;
            if (store == null)
            {
                return;
            }

            var resourceManagerId = settings.Get<string>("NServiceBus.LocalAddress") + "-" + settings.Get<string>("EndpointVersion");
            store.ResourceManagerId = DeterministicGuidBuilder(resourceManagerId);

            bool suppressDistributedTransactions;
            if (settings.TryGet("Transactions.SuppressDistributedTransactions", out suppressDistributedTransactions) && suppressDistributedTransactions)
            {
                store.EnlistInDistributedTransactions = false;
            }
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

        /// <summary>
        /// Safely add the index to the RavenDB database, protect against possible failures caused by documented
        /// and undocumented possibilities of failure.
        /// Will throw iff index registration failed and index doesn't exist or it exists but with a non-current definition.
        /// </summary>
        /// <param name="store"></param>
        /// <param name="index"></param>
        internal static void SafelyCreateIndex(IDocumentStore store, AbstractIndexCreationTask index)
        {
            try
            {
                index.Execute(store);
            }
            catch (Exception) // Apparently ArgumentException can be thrown as well as a WebException; not taking any chances
            {
                var existingIndex = store.DatabaseCommands.GetIndex(index.IndexName);
                if (existingIndex == null || !index.CreateIndexDefinition().Equals(existingIndex))
                    throw;
            }
        }
    }
}
