namespace NServiceBus.Persistence.RavenDB
{
    using System;
    using NServiceBus.Logging;
    using Raven.Client.Documents;

    class Helpers
    {
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

        static readonly ILog Logger = LogManager.GetLogger(typeof(RavenDBPersistence));
    }
}