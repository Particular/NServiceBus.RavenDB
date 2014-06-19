namespace NServiceBus.Features
{
    using System;
    using Raven.Client;
    using RavenDB;
    using RavenDB.Internal;
    using Unicast.Subscriptions.RavenDB;

    /// <summary>
    /// Provides subscription storage using RavenDB
    /// </summary>
    public class RavenDbSubscriptionStorage : Feature
    {
        internal RavenDbSubscriptionStorage()
        {
            DependsOn<StorageDrivenPublishing>();
            DependsOn<SharedDocumentStore>();
        }
        

        /// <summary>
        /// Performs the setup of the feature
        /// </summary>
        /// <param name="context"></param>
        protected override void Setup(FeatureConfigurationContext context)
        {
            var store =
                // Try getting a document store object specific to this Feature that user may have wired in
                context.Settings.GetOrDefault<IDocumentStore>(RavenDbSubscriptionSettingsExtensions.SettingsKey)
                // Init up a new DocumentStore based on a connection string specific to this feature
                ?? Helpers.CreateDocumentStoreByConnectionStringName(context.Settings, "NServiceBus/Persistence/RavenDB/Subscription")
                // Trying pulling a shared DocumentStore set by the user or other Feature
                ?? context.Settings.GetOrDefault<IDocumentStore>(RavenDbSettingsExtensions.DocumentStoreSettingsKey) ?? SharedDocumentStore.Get(context.Settings);

            if (store == null)
            {
                throw new Exception("RavenDB is configured as persistence for Subscriptions and no DocumentStore instance found");
            }

            context.Container.ConfigureComponent<SubscriptionPersister>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(x => x.DocumentStore, store);
        }
    }
}
