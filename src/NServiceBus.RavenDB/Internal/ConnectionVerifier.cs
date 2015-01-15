namespace NServiceBus.RavenDB.Internal
{
    using System;
    using System.IO;
    using System.Net;
    using System.Text;
    using NServiceBus.Logging;
    using Raven.Client;
    using Raven.Imports.Newtonsoft.Json;

    class ConnectionVerifier
    {
        internal static void VerifyConnectionToRavenDBServer(IDocumentStore store)
        {
            RavenBuildInfo ravenBuildInfo = null;
            var connectionSuccessful = false;
            Exception exception = null;
            try
            {
                store.Initialize();

                // for embedded databases
                if (store.Url == null)
                {
                    return;
                }

                var request = WebRequest.Create(string.Format("{0}/build/version", store.Url));
                request.Timeout = 2000;
                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    if (response.StatusCode != HttpStatusCode.OK)
                        throw new InvalidOperationException("Call failed - " + response.StatusDescription);

                    using (var stream = response.GetResponseStream())
                    using (var reader = new StreamReader(stream))
                    {
                        ravenBuildInfo = JsonConvert.DeserializeObject<RavenBuildInfo>(reader.ReadToEnd());
                    }

                    connectionSuccessful = true;
                }
            }
            catch (Exception ex)
            {
                exception = ex;
            }
            if (!connectionSuccessful)
            {
                ShowUncontactableRavenWarning(store, exception);
                return;
            }

            if (!ravenBuildInfo.IsSufficientVersion())
            {
                throw new InvalidOperationException(string.Format(WrongRavenVersionMessage, ravenBuildInfo));
            }

            Logger.InfoFormat("Connection to RavenDB at {0} verified. Detected version: {1}", store.Url, ravenBuildInfo);
        }

        static void ShowUncontactableRavenWarning(IDocumentStore store, Exception exception)
        {
            var sb = new StringBuilder();
            sb.AppendFormat("RavenDB could not be contacted. We tried to access Raven using the following url: {0}.",
                            store.Url);
            sb.AppendLine();
            sb.AppendFormat("Please ensure that you can open the RavenDB Management Studio by navigating to {0}.", store.Url);
            sb.AppendLine();
            sb.AppendLine(
                @"To configure NServiceBus to use a different Raven connection string add a connection string named ""NServiceBus/Persistence"" in your config file, example:");
            sb.AppendLine(
                @"<connectionStrings>
    <add name=""NServiceBus/Persistence"" connectionString=""Url = http://localhost:9090"" />
</connectionStrings>");
            sb.AppendLine("Reason: " + exception);

            Logger.Warn(sb.ToString());
        }

        class RavenBuildInfo
        {
            public string ProductVersion { get; set; }

            public string BuildVersion { get; set; }

            public bool IsSufficientVersion()
            {
                int buildVersion;
                if (!int.TryParse(BuildVersion, out buildVersion))
                    return false;
                return !string.IsNullOrEmpty(ProductVersion) &&
                       (ProductVersion.StartsWith("2.5") && buildVersion >= 2900) || (ProductVersion.StartsWith("3.0"));
            }

            public override string ToString()
            {
                return string.Format("Product version: {0}, Build version: {1}", ProductVersion, BuildVersion);
            }
        }

        const string WrongRavenVersionMessage =
@"The RavenDB server you have specified is detected to be {0}. NServiceBus requires RavenDB version 2.5 build 2900 (or a higher build number for version 2.5) to operate correctly. Please update your RavenDB server.
Further instructions can be found at: http://particular.net/articles/using-ravendb-in-nservicebus-installing";

        static readonly ILog Logger = LogManager.GetLogger(typeof(ConnectionVerifier));
    }
}
