namespace NServiceBus.Features
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.RavenDB;
    using NServiceBus.RavenDB.Internal;
    using NServiceBus.RavenDB.Timeouts;
    using NServiceBus.TimeoutPersisters.RavenDB;
    using Raven.Client;
    using Raven.Client.Document;
    using Raven.Client.Document.DTC;

    class RavenDbTimeoutStorage : Feature
    {
        RavenDbTimeoutStorage()
        {
            DependsOn<TimeoutManager>();
            DependsOn<SharedDocumentStore>();
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            var store =
                // Try getting a document store object specific to this Feature that user may have wired in
                context.Settings.GetOrDefault<IDocumentStore>(RavenDbTimeoutSettingsExtensions.SettingsKey)
                // Init up a new DocumentStore based on a connection string specific to this feature
                ?? Helpers.CreateDocumentStoreByConnectionStringName(context.Settings, "NServiceBus/Persistence/RavenDB/Timeout")
                // Trying pulling a shared DocumentStore set by the user or other Feature
                ?? context.Settings.GetOrDefault<IDocumentStore>(RavenDbSettingsExtensions.DocumentStoreSettingsKey) ?? SharedDocumentStore.Get(context.Settings);

            if(store == null)
            {
                throw new Exception("RavenDB is configured as persistence for Timeouts and no DocumentStore instance found");
            }

            StorageEngineVerifier.VerifyStorageEngineSupportsDtcIfRequired(store, context.Settings);

            BackwardsCompatibilityHelper.SupportOlderClrTypes(store);

            // This is required for DTC fix, and this requires RavenDB 2.5 build 2900 or above
            var remoteStorage = store as DocumentStore;
            if(remoteStorage != null)
            {
                remoteStorage.TransactionRecoveryStorage = new IsolatedStorageTransactionRecoveryStorage();
            }

            store.Listeners.RegisterListener(new TimeoutDataV1toV2Converter());

            Helpers.SafelyCreateIndex(store, new TimeoutsIndex());

            context.Container.ConfigureComponent(() => new TimeoutPersister(store), DependencyLifecycle.InstancePerCall);
            context.Container.ConfigureComponent(() => new QueryTimeouts(store), DependencyLifecycle.SingleInstance) // Needs to be SingleInstance because it contains cleanup state
                .ConfigureProperty(x => x.EndpointName, context.Settings.EndpointName().ToString());

            context.RegisterStartupTask(builder =>
            {
                return builder.Build<QueryTimeoutsLifecycleHandler>();
            });
        }

        class QueryTimeoutsLifecycleHandler : FeatureStartupTask
        {
            public QueryTimeouts QueryTimeouts { get; set; }

            protected override Task OnStart(IBusSession session)
            {
                //NOP
                return Task.FromResult(false);
            }

            protected override Task OnStop(IBusSession session)
            {
                return Task.Run(() => this.QueryTimeouts.Shutdown());
            }
        }
    }
}
