namespace NServiceBus.Features
{
    using System;
    using Persistence;
    using Raven.Client;
    using Raven.Client.Document;
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
            // Try getting a document store object that may have been wired by the user
            var store = context.Settings.GetOrDefault<IDocumentStore>(RavenDbSubscriptionSettingsExtenstions.SettingsKey)
                ?? context.Settings.GetOrDefault<IDocumentStore>(RavenDbSettingsExtenstions.DocumentStoreSettingsKey);

            // Init up a new DocumentStore based on a connection string
            if (store == null)
            {
                var connectionStringName = Helpers.GetFirstNonEmptyConnectionString("NServiceBus/Persistence/RavenDB/Subscription", "NServiceBus/Persistence/RavenDB", "NServiceBus/Persistence");
                if (!string.IsNullOrWhiteSpace(connectionStringName))
                {
                    store = new DocumentStore { ConnectionStringName = connectionStringName }.Initialize();
                }
            }

            if (store == null)
            {
                throw new Exception("RavenDB is configured as persistence and no document store found");
            }

            context.Container.ConfigureComponent<SubscriptionPersister>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(x => x.DocumentStore, store)
                ;
        }
    }

    public static class RavenDbSubscriptionSettingsExtenstions
    {
        public const string SettingsKey = "RavenDbDocumentStore/Subscription";

        public static void UseDocumentStoreForSubscriptions(this PersistenceConfiguration cfg, IDocumentStore documentStore)
        {
            cfg.Config.Settings.Set(SettingsKey, documentStore);
        }
    }
}
