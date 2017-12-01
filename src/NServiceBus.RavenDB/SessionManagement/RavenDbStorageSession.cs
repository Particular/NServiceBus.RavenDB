namespace NServiceBus.Persistence.RavenDB
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Features;
    using Raven.Client;

    class RavenDbStorageSession : Feature
    {
        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<RavenDBSynchronizedStorageAdapter>(DependencyLifecycle.SingleInstance);
            context.Container.ConfigureComponent<RavenDBSynchronizedStorage>(DependencyLifecycle.SingleInstance);

            IOpenRavenSessionsInPipeline sessionCreator;

            // Check to see if the user provided us with a shared session to work with before we go and create our own to inject into the pipeline
            var getAsyncSessionFunc = context.Settings.GetOrDefault<Func<IDictionary<string, string>, IAsyncDocumentSession>>(RavenDbSettingsExtensions.SharedAsyncSessionSettingsKey);
            var getAsyncSessionFuncObsolete = context.Settings.GetOrDefault<Func<IAsyncDocumentSession>>(RavenDbSettingsExtensions.SharedAsyncSessionSettingsKey + ".Obsolete");

            if (getAsyncSessionFunc != null)
            {
                sessionCreator = new OpenRavenSessionByCustomDelegate(getAsyncSessionFunc);
            }
            else if (getAsyncSessionFuncObsolete != null)
            {
                sessionCreator = new OpenRavenSessionByCustomDelegate(getAsyncSessionFuncObsolete);
            }
            else
            {
                var store = DocumentStoreManager.GetDocumentStore<StorageType.Sagas>(context.Settings);
                var storeWrapper = new DocumentStoreWrapper(store);

                var dbNameConvention = context.Settings.GetOrDefault<Func<IDictionary<string, string>, string>>("RavenDB.SetMessageToDatabaseMappingConvention");
                sessionCreator = new OpenRavenSessionByDatabaseName(storeWrapper, dbNameConvention);
            }

            context.Container.RegisterSingleton(sessionCreator);
            context.Pipeline.Register("OpenRavenDbAsyncSession", new OpenAsyncSessionBehavior(sessionCreator), "Makes sure that there is a RavenDB IAsyncDocumentSession available on the pipeline");
        }
    }
}