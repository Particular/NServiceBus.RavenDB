namespace NServiceBus.RavenDB.Internal
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.IO;
    using NServiceBus.Logging;
    using NServiceBus.Settings;
    using Raven.Client;
    using Raven.Client.Document;
    using Raven.Client.Document.DTC;

    class RavenTransactionRecoveryStorageCreator
    {
        readonly string suffixPath;

        static readonly ILog logger = LogManager.GetLogger<RavenTransactionRecoveryStorageCreator>();

        private RavenTransactionRecoveryStorageCreator(IDocumentStore store)
        {
            var docStore = store as DocumentStore;
            if (docStore == null)
            {
                throw new InvalidOperationException("Cannot create transaction recovery storage for anything but a full DocumentStore instance.");
            }

            this.suffixPath = Path.Combine("NServiceBus.RavenDB", docStore.ResourceManagerId.ToString());
        }

        public static ITransactionRecoveryStorage Create(IDocumentStore store, ReadOnlySettings settings)
        {
            var creator = new RavenTransactionRecoveryStorageCreator(store);
            foreach (var location in creator.TryToCreateFromConfiguredLocations(settings))
            {
                if (location != null)
                    return location;
            }
            
            throw new InvalidOperationException($"Unable to store RavenDB transaction recovery storage information. Specify a writeable directory using the `{RavenDbPersistenceSettingsExtensions.TransactionRecoveryStorageBasePathKey}` appSetting or .SetTransactionRecoveryStorageBasePath(basePath) option. `%LOCALAPPDATA%`, `%APPDATA%`, or `%PROGRAMDATA%` are good candidates. The same path can be shared by multiple endpoints.");
        }

        private IEnumerable<LocalDirectoryTransactionRecoveryStorage> TryToCreateFromConfiguredLocations(ReadOnlySettings settings)
        {
            var settingsBasePath = settings.GetOrDefault<string>(RavenDbPersistenceSettingsExtensions.TransactionRecoveryStorageBasePathKey);
            yield return TryCreate(settingsBasePath,
                "Unable to access RavenDB transaction recovery storage specified by `.SetTransactionRecoveryStorageBasePath(string basePath)`.");

            var configBasePath = ConfigurationManager.AppSettings[RavenDbPersistenceSettingsExtensions.TransactionRecoveryStorageBasePathKey];
            yield return TryCreate(configBasePath,
                $"Unable to access RavenDB transaction recovery storage specified in appSetting `{RavenDbPersistenceSettingsExtensions.TransactionRecoveryStorageBasePathKey}`.");
        }

        private LocalDirectoryTransactionRecoveryStorage TryCreate(string basePath, string errorMessage)
        {
            if (basePath == null)
            {
                return null;
            }

            var fullPath = Path.Combine(Environment.ExpandEnvironmentVariables(basePath), this.suffixPath);

            LocalDirectoryTransactionRecoveryStorage storage;

            try
            {
                storage = new LocalDirectoryTransactionRecoveryStorage(fullPath);
            }
            catch (UnauthorizedAccessException uax)
            {
                throw new InvalidOperationException($"{errorMessage} Access to `{basePath}`is denied.", uax);
            }
            catch (Exception x)
            {
                throw new InvalidOperationException(errorMessage, x);
            }

            logger.Info($"RavenDB persistence using DTC transaction recovery storage located at `{fullPath}`.");

            return storage;
        }
    }
}
