namespace NServiceBus.Persistence.RavenDB
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Extensions.DependencyInjection;
    using Features;
    using Raven.Client.Documents.Session;

    class RavenDbStorageSession : Feature
    {
        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Services.AddSingleton<ISynchronizedStorageAdapter, RavenDBSynchronizedStorageAdapter>();
            context.Services.AddSingleton<ISynchronizedStorage, RavenDBSynchronizedStorage>();

            // Check to see if the user provided us with a shared session to work with before we go and create our own to inject into the pipeline
            var getAsyncSessionFunc = context.Settings.GetOrDefault<Func<IDictionary<string, string>, IAsyncDocumentSession>>(SharedAsyncSession);
            if (getAsyncSessionFunc != null)
            {
                IOpenTenantAwareRavenSessions sessionCreator = new OpenRavenSessionByCustomDelegate(getAsyncSessionFunc);
                context.Services.AddSingleton(sessionCreator);

                context.Settings.AddStartupDiagnosticsSection(
                    StartupDiagnosticsSectionName,
                    new
                    {
                        HasSharedAsyncSession = true,
                    });
            }
            else
            {
                context.Services.AddSingleton<IOpenTenantAwareRavenSessions>(sp =>
                {
                    var store = DocumentStoreManager.GetDocumentStore<StorageType.Sagas>(context.Settings, sp);
                    var storeWrapper = new DocumentStoreWrapper(store);
                    var dbNameConvention = context.Settings.GetOrDefault<Func<IDictionary<string, string>, string>>(MessageToDatabaseMappingConvention);
                    return new OpenRavenSessionByDatabaseName(storeWrapper, dbNameConvention);
                });

                context.Settings.AddStartupDiagnosticsSection(
                    StartupDiagnosticsSectionName,
                    new
                    {
                        HasSharedAsyncSession = false,
                        HasMessageToDatabaseMappingConvention = context.Settings.HasSetting(MessageToDatabaseMappingConvention),
                    });
            }

            var sessionHolder = new CurrentSessionHolder();
            context.Services.AddScoped(_ => sessionHolder.Current);
            context.Pipeline.Register(new CurrentSessionBehavior(sessionHolder), "Manages the lifecycle of the current session holder.");
        }

        internal const string SharedAsyncSession = "RavenDbSharedAsyncSession";
        internal const string MessageToDatabaseMappingConvention = "RavenDB.SetMessageToDatabaseMappingConvention";
        const string StartupDiagnosticsSectionName = "NServiceBus.Persistence.RavenDB.StorageSession";
    }
}