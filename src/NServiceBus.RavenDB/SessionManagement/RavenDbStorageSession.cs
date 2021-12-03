namespace NServiceBus.Persistence.RavenDB
{
    using System;
    using System.Collections.Generic;
    using Features;
    using Raven.Client.Documents.Session;

    class RavenDbStorageSession : Feature
    {
        protected override void Setup(FeatureConfigurationContext context)
        {
            // Check to see if the user provided us with a shared session to work with before we go and create our own to inject into the pipeline
            var getAsyncSessionFunc = context.Settings.GetOrDefault<Func<IDictionary<string, string>, IAsyncDocumentSession>>(SharedAsyncSession);
            var useClusterWideTransactions = context.Settings.GetOrDefault<bool>(UseClusterWideTransactions);

            if (getAsyncSessionFunc != null)
            {
                IOpenTenantAwareRavenSessions sessionCreator = new OpenRavenSessionByCustomDelegate(getAsyncSessionFunc, useClusterWideTransactions);
                context.Container.RegisterSingleton(sessionCreator);

                context.Settings.AddStartupDiagnosticsSection(
                    StartupDiagnosticsSectionName,
                    new
                    {
                        HasSharedAsyncSession = true,
                        ClusterWideTransactions = useClusterWideTransactions ? "Enabled" : "Disabled"
                    });
            }
            else
            {
                context.Container.ConfigureComponent<IOpenTenantAwareRavenSessions>(builder =>
                {
                    var store = DocumentStoreManager.GetDocumentStore<StorageType.Sagas>(context.Settings, builder);
                    var storeWrapper = new DocumentStoreWrapper(store);
                    var dbNameConvention = context.Settings.GetOrDefault<Func<IDictionary<string, string>, string>>(MessageToDatabaseMappingConvention);
                    return new OpenRavenSessionByDatabaseName(storeWrapper, useClusterWideTransactions, dbNameConvention);
                }, DependencyLifecycle.SingleInstance);

                context.Settings.AddStartupDiagnosticsSection(
                    StartupDiagnosticsSectionName,
                    new
                    {
                        HasSharedAsyncSession = false,
                        ClusterWideTransactions = useClusterWideTransactions ? "Enabled" : "Disabled",
                        HasMessageToDatabaseMappingConvention = context.Settings.HasSetting(MessageToDatabaseMappingConvention),
                    });
            }

            var sessionHolder = new CurrentSessionHolder();
            context.Container.ConfigureComponent(_ => sessionHolder.Current, DependencyLifecycle.InstancePerUnitOfWork);
            context.Pipeline.Register(new CurrentSessionBehavior(sessionHolder), "Manages the lifecycle of the current session holder.");
            context.Container.ConfigureComponent(_ => new RavenDBSynchronizedStorageAdapter(sessionHolder), DependencyLifecycle.SingleInstance);
            context.Container.ConfigureComponent(provider => new RavenDBSynchronizedStorage(provider.Build<IOpenTenantAwareRavenSessions>(), sessionHolder), DependencyLifecycle.SingleInstance);
        }

        internal const string SharedAsyncSession = "RavenDbSharedAsyncSession";
        internal const string MessageToDatabaseMappingConvention = "RavenDB.SetMessageToDatabaseMappingConvention";
        internal const string StartupDiagnosticsSectionName = "NServiceBus.Persistence.RavenDB.StorageSession";
        internal const string UseClusterWideTransactions = "NServiceBus.Persistence.RavenDB.EnableClusterWideTransactions";
    }
}