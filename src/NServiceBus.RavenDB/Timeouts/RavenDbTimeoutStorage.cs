namespace NServiceBus.Features
{
    using System;
    using Raven.Client;
    using RavenDB;
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
            var store =
                // Try getting a document store object specific to this Feature that user may have wired in
                context.Settings.GetOrDefault<IDocumentStore>(RavenDbTimeoutSettingsExtensions.SettingsKey)
                // Init up a new DocumentStore based on a connection string specific to this feature
                ?? Helpers.CreateDocumentStoreByConnectionStringName(context.Settings, "NServiceBus/Persistence/RavenDB/Timeout")
                // Trying pulling a shared DocumentStore set by the user or other Feature
                ?? context.Settings.GetOrDefault<IDocumentStore>(RavenDbSettingsExtensions.DocumentStoreSettingsKey) ?? SharedDocumentStore.Get(context.Settings);

            if (store == null)
            {
                throw new Exception("RavenDB is configured as persistence for Timeouts and no DocumentStore instance found");
            }

            new TimeoutsIndex().Execute(store);

            context.Container.ConfigureComponent<TimeoutPersister>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(x => x.DocumentStore, store)
                .ConfigureProperty(x => x.EndpointName, context.Settings.EndpointName())
                ;
        }
    }
}
