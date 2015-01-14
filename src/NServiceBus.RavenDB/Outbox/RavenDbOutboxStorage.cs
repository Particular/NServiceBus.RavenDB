using System;

namespace NServiceBus.RavenDB.Outbox
{
    using NServiceBus.Features;
    using NServiceBus.Outbox.RavenDB;
    using NServiceBus.RavenDB.Internal;
    using Raven.Client;

    /// <summary>
    /// RavenDB Outbox storage
    /// </summary>
    class RavenDbOutboxStorage : Feature
    {
        /// <summary>
        /// Creates an instance of <see cref="RavenDbOutboxStorage"/>.
        /// </summary>
        public RavenDbOutboxStorage()
        {
            DependsOn<Outbox>();
            DependsOn<SharedDocumentStore>();
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            var store =
                // Trying pulling a shared DocumentStore set by the user or other Feature
                context.Settings.GetOrDefault<IDocumentStore>(RavenDbSettingsExtensions.DocumentStoreSettingsKey) ?? SharedDocumentStore.Get(context.Settings);

            if (store == null)
            {
                throw new Exception("RavenDB is configured as persistence for Outbox and no DocumentStore instance found");
            }

            ConnectionVerifier.VerifyConnectionToRavenDBServer(store);

            context.Container.ConfigureComponent<Installer>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(c => c.StoreToInstall, store);

            context.Container.ConfigureComponent<OutboxPersister>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(x => x.DocumentStore, store);
        }
    }
}
