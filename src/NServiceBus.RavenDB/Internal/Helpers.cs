namespace NServiceBus.RavenDB.Internal
{
    using System;
    using System.Configuration;
    using System.Linq;
    using System.Text;
    using Logging;
    using NServiceBus.Persistence;
    using Raven.Client;
    using Raven.Json.Linq;

    class Helpers
    {
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

        static readonly ILog Logger = LogManager.GetLogger(typeof(RavenDB));

        public static string GetFirstNonEmptyConnectionString(params string[] connectionStringNames)
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
    }
}
