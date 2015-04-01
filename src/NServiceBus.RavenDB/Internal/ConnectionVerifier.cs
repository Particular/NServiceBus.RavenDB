namespace NServiceBus.RavenDB.Internal
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Net;
    using NServiceBus.Logging;
    using Raven.Client;
    using Raven.Imports.Newtonsoft.Json;

    class ConnectionVerifier
    {

        static ILog Logger = LogManager.GetLogger(typeof(ConnectionVerifier));

        internal static void VerifyConnectionToRavenDBServer(IDocumentStore store)
        {
            Version serverVersion;
            if (!TryGetServerVersion(store, out serverVersion))
            {
                return;
            }
            var clientVersion = GetClientVersion();
            if (AreVersionsCompatible(serverVersion, clientVersion))
            {
                Logger.InfoFormat("Connection to RavenDB at {0} verified. Server version: {1}", store.Url, serverVersion);
                return;
            }
            var message = $@"Incompatible RavenDB client and server version combination detected. 
The RavenDB server version must be within the same Major+Minor range as the client version OR be greater than the client version. 
Server Version: {serverVersion}
Client Version: {clientVersion}";
            throw new Exception(message);
        }

        static Version GetClientVersion()
        {
            var clientAssembly = typeof(IDocumentStore).Assembly;
            var versionInfo = FileVersionInfo.GetVersionInfo(clientAssembly.Location);
            return Version.Parse(versionInfo.FileVersion);
        }

        internal static bool AreVersionsCompatible(Version server, Version client)
        {
            // check that server is higher OR within same major+minor as client 
            if (server.Major == client.Major && server.Minor == client.Minor)
            {
                return true;
            }
            if (server > client)
            {
                return true;
            }
            return false;
        }

        static bool TryGetServerVersion(IDocumentStore store, out Version version)
        {
            store.Initialize();

            // for embedded databases
            if (store.Url == null)
            {
                version = null;
                return false;
            }
            try
            {
                var request = WebRequest.Create($"{store.Url}/build/version");
                request.Timeout = 2000;
                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        throw new Exception("Call failed - " + response.StatusDescription);
                    }

                    using (var stream = response.GetResponseStream())
                    using (var reader = new StreamReader(stream))
                    {
                        var buildInfo = JsonConvert.DeserializeObject<RavenBuildInfo>(reader.ReadToEnd());
                        version = buildInfo.ConvertToVersion();
                        return true;
                    }
                }
            }
            catch (Exception exception)
            {
                ShowUncontactableRavenWarning(store, exception);
                version = null;
                return false;
            }
        }

        static void ShowUncontactableRavenWarning(IDocumentStore store, Exception exception)
        {
            var message = string.Format(
                @"RavenDB could not be contacted. We tried to access Raven using the following url: {0}.
Please ensure that RavenDB is running on that url and port.
If you have enabled Raven Studio you should be able to verify it by navigating to {0}/studio/index.html.
To configure NServiceBus to use a different Raven connection string add a connection string named ""NServiceBus/Persistence"" in your config file, example:
<connectionStrings>
    <add name=""NServiceBus/Persistence"" connectionString=""Url = http://localhost:9090"" />
</connectionStrings>
Reason: {1}", store.Url, exception);
            Logger.Warn(message);
        }

    }
}