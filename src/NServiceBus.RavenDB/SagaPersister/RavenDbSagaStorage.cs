namespace NServiceBus.Features
{
    using System;
    using Persistence;
    using Raven.Client;
    using Raven.Client.Document;
    using RavenDB;
    using RavenDB.Internal;
    using SagaPersisters.RavenDB;

    /// <summary>
    /// RavenDB Saga Storage.
    /// </summary>
    public class RavenDbSagaStorage : Feature
    {
        /// <summary>
        /// Creates an instance of <see cref="RavenDbSagaStorage"/>.
        /// </summary>
        internal RavenDbSagaStorage()
        {
            DependsOn<Sagas>();
        }

        /// <summary>
        /// Called when the feature should perform its initialization. This call will only happen if the feature is enabled.
        /// </summary>
        protected override void Setup(FeatureConfigurationContext context)
        {
            // Try getting a document store object specific to this Feature that user may have wired in
            var store = context.Settings.GetOrDefault<IDocumentStore>(RavenDbSagaSettingsExtenstions.SettingsKey);

            // Init up a new DocumentStore based on a connection string specific to this feature
            if (store == null)
            {
                var connectionStringName = Helpers.GetFirstNonEmptyConnectionString("NServiceBus/Persistence/RavenDB/Saga");
                if (!string.IsNullOrWhiteSpace(connectionStringName))
                {
                    store = new DocumentStore { ConnectionStringName = connectionStringName }.Initialize();
                }
            }

            // Trying pulling a shared DocumentStore set by the user or other Feature
            store = store ?? context.Settings.GetOrDefault<IDocumentStore>(RavenDbSettingsExtenstions.DocumentStoreSettingsKey) ?? SharedDocumentStore.Get(context);

            if (store == null)
            {
                throw new Exception("RavenDB is configured as persistence for Sagas and no DocumentStore instance found");
            }

            // TODO here would be the place to wire up the ISagaFinder extension point

            // TODO configure ISessionProvider
            context.Container.ConfigureComponent<SagaPersister>(DependencyLifecycle.InstancePerCall)
                ;
        }
    }

    public static class RavenDbSagaSettingsExtenstions
    {
        public const string SettingsKey = "RavenDbDocumentStore/Saga";

        public static void UseDocumentStoreForSagas(this PersistenceConfiguration cfg, IDocumentStore documentStore)
        {
            cfg.Config.Settings.Set(SettingsKey, documentStore);
        }
    }
}
