namespace NServiceBus.RavenDB.SessionManagement
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Features;
    using NServiceBus.Persistence;
    using NServiceBus.Persistence.RavenDB;
    using NServiceBus.RavenDB.Internal;
    using NServiceBus.RavenDB.Outbox;
    using Raven.Client;

    class RavenDbStorageSession : Feature
    {
        public RavenDbStorageSession()
        {
            DependsOnAtLeastOne(typeof(RavenDbSagaStorage), typeof(RavenDbOutboxStorage));
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            var store = DocumentStoreManager.GetDocumentStore<StorageType.Sagas>(context.Settings);

            IOpenRavenSessionsInPipeline sessionCreator;

            // Check to see if the user provided us with a shared session to work with before we go and create our own to inject into the pipeline
            var getSessionFunc = context.Settings.GetOrDefault<Func<IDictionary<string, string>, IDocumentSession>>(RavenDbSettingsExtensions.SharedSessionSettingsKey);
            var getSessionFuncObsolete = context.Settings.GetOrDefault<Func<IDocumentSession>>(RavenDbSettingsExtensions.SharedSessionSettingsKey + ".Obsolete");

            if (getSessionFunc != null)
            {
                sessionCreator = new OpenRavenSessionByCustomDelegate(getSessionFunc);
            }
            else if (getSessionFuncObsolete != null)
            {
                sessionCreator = new OpenRavenSessionByCustomDelegate(getSessionFuncObsolete);
            }
            else
            {
               
                var storeWrapper = new DocumentStoreWrapper(store);

                var dbNameConvention = context.Settings.GetOrDefault<Func<IMessageContext, string>>("RavenDB.SetMessageToDatabaseMappingConvention");
                sessionCreator = new OpenRavenSessionByDatabaseName(storeWrapper, dbNameConvention);
            }

            context.Container.ConfigureComponent<Installer>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(c => c.StoreToInstall, store);

            context.Container.ConfigureComponent<RavenSessionProvider>(DependencyLifecycle.InstancePerCall);
            context.Container.RegisterSingleton<IDocumentStoreWrapper>(new DocumentStoreWrapper(store));
            context.Pipeline.Register<OpenSessionBehavior.Registration>();

            context.Container.RegisterSingleton(sessionCreator);
        }
    }
}