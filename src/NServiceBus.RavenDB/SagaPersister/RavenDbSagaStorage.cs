namespace NServiceBus.Features
{
    using Persistence;
    using Raven.Client;
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
            // TODO here would be the place to wire up the ISagaFinder extension point

            context.Container.ConfigureComponent<SagaPersister>(DependencyLifecycle.InstancePerCall);
        }
    }

    public static class RavenDbSagaSettingsExtenstions
    {
        public const string SettingsKey = "RavenDbDocumentStore/Saga";

        public static PersistenceConfiguration UseDocumentStoreForSagas(this PersistenceConfiguration cfg, IDocumentStore documentStore)
        {
            cfg.Config.Settings.Set(SettingsKey, documentStore);
            return cfg;
        }
    }
}
