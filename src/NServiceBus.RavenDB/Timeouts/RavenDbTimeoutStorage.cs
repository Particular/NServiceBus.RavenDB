namespace NServiceBus.Persistence.RavenDB
{
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using NServiceBus.Features;
    using NServiceBus.Timeout.Core;

    class RavenDbTimeoutStorage : Feature
    {
        RavenDbTimeoutStorage()
        {
            DependsOn<TimeoutManager>();
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            //var store = DocumentStoreManager.GetDocumentStore<StorageType.Timeouts>(context.Settings);
            DocumentStoreManager.GetUninitializedDocumentStore<StorageType.Timeouts>(context.Settings)
                .CreateIndexOnInitialization(new TimeoutsIndex());

            context.Services.AddTransient<IPersistTimeouts>(sp =>
            {
                var store = DocumentStoreManager.GetDocumentStore<StorageType.Timeouts>(context.Settings, sp);
                return new TimeoutPersister(store);
            });

            context.Services.AddSingleton(sp =>
            {
                var store = DocumentStoreManager.GetDocumentStore<StorageType.Timeouts>(context.Settings, sp);
                return new QueryTimeouts(store, context.Settings.EndpointName());
            }); // Needs to be SingleInstance because it contains cleanup state
            context.Services.AddSingleton<IQueryTimeouts>(sp => sp.GetRequiredService<QueryTimeouts>());

            context.Services.AddTransient<QueryCanceller>();
            context.RegisterStartupTask(b => b.GetRequiredService<QueryCanceller>());
        }

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