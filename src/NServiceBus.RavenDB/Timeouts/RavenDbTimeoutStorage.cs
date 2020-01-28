namespace NServiceBus.Persistence.RavenDB
{
    using System.Threading.Tasks;
    using NServiceBus.Features;
    using Raven.Client.Documents;

    class RavenDbTimeoutStorage : Feature
    {
        RavenDbTimeoutStorage()
        {
            DependsOn<TimeoutManager>();
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            //var store = DocumentStoreManager.GetDocumentStore<StorageType.Timeouts>(context.Settings);

            context.Container.ConfigureComponent(b =>
            {
                var store = DocumentStoreManager.GetDocumentStore<StorageType.Timeouts>(context.Settings, b);
                return new TimeoutPersister(store);
            }, DependencyLifecycle.InstancePerCall);

            context.Container.ConfigureComponent(b =>
            {
                var store = DocumentStoreManager.GetDocumentStore<StorageType.Timeouts>(context.Settings, b);
                return new QueryTimeouts(store, context.Settings.EndpointName());
            }, DependencyLifecycle.SingleInstance); // Needs to be SingleInstance because it contains cleanup state

            context.Container.ConfigureComponent(b =>
            {
                var store = DocumentStoreManager.GetDocumentStore<StorageType.Timeouts>(context.Settings, b);
                return new QueryCanceller(b.Build<QueryTimeouts>(), store);
            },DependencyLifecycle.InstancePerCall);
            context.RegisterStartupTask(b => b.Build<QueryCanceller>());
        }

        class QueryCanceller : FeatureStartupTask
        {
            public QueryCanceller(QueryTimeouts queryTimeouts, IDocumentStore store)
            {
                this.queryTimeouts = queryTimeouts;
                this.store = store;
            }

            protected override Task OnStart(IMessageSession session)
            {
                Helpers.SafelyCreateIndex(store, new TimeoutsIndex());

                return Task.CompletedTask;
            }

            protected override Task OnStop(IMessageSession session)
            {
                queryTimeouts.Shutdown();
                return Task.CompletedTask;
            }

            QueryTimeouts queryTimeouts;
            readonly IDocumentStore store;
        }
    }
}
