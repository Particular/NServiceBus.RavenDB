namespace NServiceBus.Persistence.RavenDB
{
    using System;
    using NServiceBus.Logging;
    using Raven.Client.Documents;
    using Raven.Client.Documents.Indexes;
    using Raven.Client.Documents.Operations.Indexes;

    class Helpers
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(RavenDBPersistence));

        static void LogRavenConnectionFailure(Exception exception, IDocumentStore store)
        {
            var error = $@"RavenDB could not be contacted. Check your DocumentStore configuration.
Original exception: {exception}";
            Logger.Warn(error);
        }

        public static void VerifyConnectionToRavenDb(IDocumentStore documentStore)
        {
            try
            {
                using (var session = documentStore.OpenSession())
                {
                    session.Store(new object(), null, "nsb/ravendb/testdocument");
                    session.Delete("nsb/ravendb/testdocument");
                    session.SaveChanges();
                }
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
                var getIndexOp = new GetIndexOperation(index.IndexName);

                var existingIndex = store.Maintenance.Send(getIndexOp);
                if (existingIndex == null || !index.CreateIndexDefinition().Equals(existingIndex))
                    throw;
            }
        }
    }
}
