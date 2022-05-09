namespace NServiceBus.Persistence.RavenDB
{
    using System;
    using System.Collections.Generic;
    using Features;
    using Microsoft.Extensions.DependencyInjection;
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
                context.Services.AddSingleton(sessionCreator);

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
                context.Services.AddSingleton<IOpenTenantAwareRavenSessions>(sp =>
                {
                    var store = DocumentStoreManager.GetDocumentStore<StorageType.Sagas>(context.Settings, sp);
                    var storeWrapper = new DocumentStoreWrapper(store);
                    var dbNameConvention = context.Settings.GetOrDefault<Func<IDictionary<string, string>, string>>(MessageToDatabaseMappingConvention);
                    return new OpenRavenSessionByDatabaseName(storeWrapper, useClusterWideTransactions, dbNameConvention);
                });

                context.Settings.AddStartupDiagnosticsSection(
                    StartupDiagnosticsSectionName,
                    new
                    {
                        HasSharedAsyncSession = false,
                        ClusterWideTransactions = useClusterWideTransactions ? "Enabled" : "Disabled",
                        HasMessageToDatabaseMappingConvention = context.Settings.HasSetting(MessageToDatabaseMappingConvention),
                    });
            }

            context.Services.AddScoped<ICompletableSynchronizedStorageSession, RavenDBSynchronizedStorageSession>();
            context.Services.AddScoped(sp => sp.GetRequiredService<ICompletableSynchronizedStorageSession>().RavenSession());

        }

        internal const string SharedAsyncSession = "RavenDbSharedAsyncSession";
        internal const string MessageToDatabaseMappingConvention = "RavenDB.SetMessageToDatabaseMappingConvention";
        internal const string StartupDiagnosticsSectionName = "NServiceBus.Persistence.RavenDB.StorageSession";
        internal const string UseClusterWideTransactions = "NServiceBus.Persistence.RavenDB.EnableClusterWideTransactions";
    }
}