namespace NServiceBus.Persistence.RavenDB
{
    using System;
    using System.Linq;
    using Newtonsoft.Json.Linq;
    using NServiceBus.Logging;
    using Raven.Client.Documents;
    using Raven.Client.Documents.Indexes;

    class Helpers
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(RavenDBPersistence));

        // TODO: Look for string "configuration" because messages like below need to be changed.

        static void LogRavenConnectionFailure(Exception exception, IDocumentStore store)
        {
            // TODO: Just using store.Urls.First() here is wrong
            var error = $@"RavenDB could not be contacted. We tried to access Raven using the following url: {store.Urls.First()}.
Ensure that you can open the Raven Studio by navigating to {store.Urls.First()}.
To configure NServiceBus to use a different Raven connection string add a connection string named ""NServiceBus.Persistence"" in the config file, example:
<connectionStrings>
    <add name=""NServiceBus.Persistence"" connectionString=""http://localhost:9090"" />
</connectionStrings>
Original exception: {exception}";
            Logger.Warn(error);
        }

        public static void VerifyConnectionToRavenDb(IDocumentStore documentStore)
        {
            try
            {
                documentStore.DatabaseCommands.Put("nsb/ravendb/testdocument", null, new JObject(), new JObject());
                documentStore.DatabaseCommands.Delete("nsb/ravendb/testdocument", null);
            }
            catch (Exception e)
            {
                LogRavenConnectionFailure(e, documentStore);
                throw;
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
