namespace NServiceBus.Persistence.RavenDB
{
    using System.Threading.Tasks;
    using NServiceBus.Features;

    class RavenDbTimeoutStorage : Feature
    {
        RavenDbTimeoutStorage()
        {
            DependsOn<TimeoutManager>();
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            DocumentStoreManager.GetUninitializedDocumentStore<StorageType.Timeouts>(context.Settings)
                .CreateIndexOnInitialization(new TimeoutsIndex());

            var useClusterWideTransactions = context.Settings.GetOrDefault<bool>(RavenDbStorageSession.UseClusterWideTransactions);

            context.Settings.AddStartupDiagnosticsSection(
                StartupDiagnosticsSectionName,
                new
                {
                    ClusterWideTransactions = useClusterWideTransactions ? "Enabled" : "Disabled"
                });

            context.Container.ConfigureComponent(b =>
            {
                var store = DocumentStoreManager.GetDocumentStore<StorageType.Timeouts>(context.Settings, b);
                return new TimeoutPersister(store, useClusterWideTransactions);
            }, DependencyLifecycle.InstancePerCall);

            context.Container.ConfigureComponent(b =>
            {
                var store = DocumentStoreManager.GetDocumentStore<StorageType.Timeouts>(context.Settings, b);
                return new QueryTimeouts(store, context.Settings.EndpointName());
            }, DependencyLifecycle.SingleInstance); // Needs to be SingleInstance because it contains cleanup state

            context.Container.ConfigureComponent(typeof(QueryCanceller), DependencyLifecycle.InstancePerCall);
            context.RegisterStartupTask(b => b.Build<QueryCanceller>());
        }

        internal const string StartupDiagnosticsSectionName = "NServiceBus.Persistence.RavenDB.Timeouts";

        class QueryCanceller : FeatureStartupTask
        {
            public QueryCanceller(QueryTimeouts queryTimeouts)
            {
                this.queryTimeouts = queryTimeouts;
            }

            protected override Task OnStart(IMessageSession session)
            {
                return Task.CompletedTask;
            }

            protected override Task OnStop(IMessageSession session)
            {
                queryTimeouts.Shutdown();
                return Task.CompletedTask;
            }

            QueryTimeouts queryTimeouts;
        }
    }
}