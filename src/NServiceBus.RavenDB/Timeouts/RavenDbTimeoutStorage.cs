namespace NServiceBus.Features
{
    using System;
    using Persistence;
    using Raven.Client;
    using Raven.Client.Document;
    using RavenDB.Internal;
    using TimeoutPersisters.RavenDB;

    /// <summary>
    /// RavenDB Timeout storage
    /// </summary>
    public class RavenDbTimeoutStorage : Feature
    {
        /// <summary>
        /// Creates an instance of <see cref="RavenDbTimeoutStorage"/>.
        /// </summary>
        RavenDbTimeoutStorage()
        {
            DependsOn<TimeoutManager>();
        }

        /// <summary>
        /// Called when the feature should perform its initialization. This call will only happen if the feature is enabled.
        /// </summary>
        protected override void Setup(FeatureConfigurationContext context)
        {
            // Try getting a document store object that may have been wired by the user
            var store = context.Settings.GetOrDefault<IDocumentStore>(RavenDbTimeoutSettingsExtenstions.SettingsKey);

            // Init up a new DocumentStore based on a connection string
            if (store == null)
            {
                var connectionStringName = Helpers.GetFirstNonEmptyConnectionString("NServiceBus/Persistence/RavenDB/Timeout");
                if (!string.IsNullOrWhiteSpace(connectionStringName))
                {
                    store = new DocumentStore { ConnectionStringName = connectionStringName }.Initialize();
                }                
            }

            // Trying pulling a shared DocumentStore set by the user or other Feature
            // TODO pull from a centralized place instead
            if (store == null)
            {
                store = context.Settings.GetOrDefault<IDocumentStore>(RavenDbSettingsExtenstions.DocumentStoreSettingsKey);
                if (store == null)
                {
                    var connectionStringName = Helpers.GetFirstNonEmptyConnectionString("NServiceBus/Persistence/RavenDB", "NServiceBus/Persistence");
                    if (!string.IsNullOrWhiteSpace(connectionStringName))
                    {
                        store = new DocumentStore { ConnectionStringName = connectionStringName }.Initialize();
                    }
                }
            }

            if (store == null)
            {
                throw new Exception("RavenDB is configured as persistence and no document store found");
            }

            context.Container.ConfigureComponent<TimeoutPersister>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(x => x.DocumentStore, store)
                .ConfigureProperty(x => x.EndpointName, context.Settings.EndpointName())
                ;
        }
    }

    public static class RavenDbTimeoutSettingsExtenstions
    {
        public const string SettingsKey = "RavenDbDocumentStore/Timeouts";

        public static void UseDocumentStoreForTimeouts(this PersistenceConfiguration cfg, IDocumentStore documentStore)
        {
            cfg.Config.Settings.Set(SettingsKey, documentStore);
        }
    }
}
