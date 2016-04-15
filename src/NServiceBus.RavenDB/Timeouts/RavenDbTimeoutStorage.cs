namespace NServiceBus.Features
{
    using System.Threading.Tasks;
    using NServiceBus.Persistence;
    using NServiceBus.RavenDB;
    using NServiceBus.RavenDB.Internal;
    using NServiceBus.RavenDB.Timeouts;
    using NServiceBus.TimeoutPersisters.RavenDB;

    class RavenDbTimeoutStorage : Feature
    {
        RavenDbTimeoutStorage()
        {
            DependsOn<TimeoutManager>();
            DependsOn<SharedDocumentStore>();
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            var store = DocumentStoreManager.GetDocumentStore<StorageType.Timeouts>(context.Settings);

            store.Listeners.RegisterListener(new TimeoutDataV1toV2Converter());

            Helpers.SafelyCreateIndex(store, new TimeoutsIndex());

            context.Container.ConfigureComponent(() => new TimeoutPersister(store), DependencyLifecycle.InstancePerCall);
            context.Container.ConfigureComponent(() => new QueryTimeouts(store, context.Settings.EndpointName().ToString()), DependencyLifecycle.SingleInstance); // Needs to be SingleInstance because it contains cleanup state

            context.Container.ConfigureComponent<QueryCanceller>(DependencyLifecycle.InstancePerCall);
            context.RegisterStartupTask(b => b.Build<QueryCanceller>());
        }

        class QueryCanceller : FeatureStartupTask
        {
            public QueryCanceller(QueryTimeouts queryTimeouts)
            {
                this.queryTimeouts = queryTimeouts;
            }

            protected override Task OnStart(IMessageSession session)
            {
                return Task.FromResult(0);
            }

            protected override Task OnStop(IMessageSession session)
            {
                queryTimeouts.Shutdown();
                return Task.FromResult(0);
            }

            QueryTimeouts queryTimeouts;
        }
    }
}
