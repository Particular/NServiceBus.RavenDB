namespace NServiceBus.RavenDB.Internal
{
    using System;
    using System.IO;
    using NServiceBus.Logging;
    using NServiceBus.Settings;
    using Raven.Client;
    using Raven.Client.Document;
    using Raven.Client.Document.DTC;

    static class RavenTransactionRecoveryStorageCreator
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(RavenTransactionRecoveryStorageCreator));

        public static ITransactionRecoveryStorage Create(IDocumentStore store, ReadOnlySettings settings)
        {
            var docStore = store as DocumentStore;
            if (docStore == null)
            {
                throw new InvalidOperationException("Cannot create transaction recovery storage for anything but a full DocumentStore instance.");
            }

            var settingsBasePath = settings.GetOrDefault<string>(RavenDbPersistenceSettingsExtensions.TransactionRecoveryStorageBasePathKey);

            if (settingsBasePath == null)
            {
                var programDataPathForException = Environment.ExpandEnvironmentVariables("%PROGRAMDATA%");
                throw new InvalidOperationException($"Unable to store RavenDB transaction recovery storage information. Specify a writeable directory using the .SetTransactionRecoveryStorageBasePath(basePath) option. `{programDataPathForException}` may be a good candidate. The same path can be shared by multiple endpoints.");
            }

            var suffixPath = Path.Combine("NServiceBus.RavenDB", docStore.ResourceManagerId.ToString());
            const string commonErrorMsg = "Unable to access RavenDB transaction recovery storage specified by `.SetTransactionRecoveryStorageBasePath(string basePath)`.";

            var fullPath = Path.Combine(settingsBasePath, suffixPath);

            try
            {
                var storage = new LocalDirectoryTransactionRecoveryStorage(fullPath);
                logger.Info($"RavenDB persistence using DTC transaction recovery storage located at `{fullPath}`.");
                return storage;
            }
            catch (UnauthorizedAccessException uax)
            {
                throw new InvalidOperationException($"{commonErrorMsg} Access to `{settingsBasePath}`is denied.", uax);
            }
            catch (Exception x)
            {
                throw new InvalidOperationException(commonErrorMsg, x);
            }
        }
    }
}
