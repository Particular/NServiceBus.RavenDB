namespace NServiceBus.Features
{
    using System;
    using Persistence;
    using Raven.Client;
    using RavenDB;
    using RavenDB.Internal;
    using Unicast.Subscriptions.RavenDB;

    public class RavenDbSubscriptionStorage : Feature
    {
        public RavenDbSubscriptionStorage()
        {
            DependsOn<StorageDrivenPublishing>();
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            var store =
                // Try getting a document store object specific to this Feature that user may have wired in
                context.Settings.GetOrDefault<IDocumentStore>(RavenDbSubscriptionSettingsExtenstions.SettingsKey)
                // Init up a new DocumentStore based on a connection string specific to this feature
                ?? Helpers.CreateDocumentStoreByConnectionStringName(context.Settings, "NServiceBus/Persistence/RavenDB/Subscription")
                // Trying pulling a shared DocumentStore set by the user or other Feature
                ?? context.Settings.GetOrDefault<IDocumentStore>(RavenDbSettingsExtenstions.DocumentStoreSettingsKey) ?? SharedDocumentStore.Get(context.Settings);

            if (store == null)
            {
                throw new Exception("RavenDB is configured as persistence for Subscriptions and no DocumentStore instance found");
            }

            context.Container.ConfigureComponent<SubscriptionPersister>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(x => x.DocumentStore, store)
                ;
        }
    }

    public static class RavenDbSubscriptionSettingsExtenstions
    {
        public const string SettingsKey = "RavenDbDocumentStore/Subscription";

        public static PersistenceConfiguration UseDocumentStoreForSubscriptions(this PersistenceConfiguration cfg, IDocumentStore documentStore)
        {
            cfg.Config.Settings.Set(SettingsKey, documentStore);
            return cfg;
        }
    }
}
