namespace NServiceBus.Persistence.RavenDB
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Features;
    using Raven.Client.Documents.Session;

    class RavenDbStorageSession : Feature
    {
        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<RavenDBSynchronizedStorageAdapter>(DependencyLifecycle.SingleInstance);
            context.Container.ConfigureComponent<RavenDBSynchronizedStorage>(DependencyLifecycle.SingleInstance);

            // Check to see if the user provided us with a shared session to work with before we go and create our own to inject into the pipeline
            var getAsyncSessionFunc = context.Settings.GetOrDefault<Func<IDictionary<string, string>, IAsyncDocumentSession>>(SharedAsyncSession);

            if (getAsyncSessionFunc != null)
            {
                IOpenTenantAwareRavenSessions sessionCreator = new OpenRavenSessionByCustomDelegate(getAsyncSessionFunc);
                context.Container.RegisterSingleton(sessionCreator);

                context.Settings.AddStartupDiagnosticsSection(
                    StartupDiagnosticsSectionName,
                    new
                    {
                        HasSharedAsyncSession = true,
                    });
            }
            else
            {
                context.Container.ConfigureComponent<IOpenTenantAwareRavenSessions>(
                    builder =>
                    {
                        var store = DocumentStoreManager.GetDocumentStore<StorageType.Sagas>(context.Settings, builder);
                        var storeWrapper = new DocumentStoreWrapper(store);
                        var dbNameConvention = context.Settings.GetOrDefault<Func<IDictionary<string, string>, string>>(MessageToDatabaseMappingConvention);
                        return new OpenRavenSessionByDatabaseName(storeWrapper, dbNameConvention);
                    },
                    DependencyLifecycle.SingleInstance);

                context.Settings.AddStartupDiagnosticsSection(
                    StartupDiagnosticsSectionName,
                    new
                    {
                        HasSharedAsyncSession = false,
                        HasMessageToDatabaseMappingConvention = context.Settings.HasSetting(MessageToDatabaseMappingConvention),
                    });
            }
        }

        internal const string SharedAsyncSession = "RavenDbSharedAsyncSession";
        internal const string MessageToDatabaseMappingConvention = "RavenDB.SetMessageToDatabaseMappingConvention";
        const string StartupDiagnosticsSectionName = "NServiceBus.Persistence.RavenDB.StorageSession";
    }
}