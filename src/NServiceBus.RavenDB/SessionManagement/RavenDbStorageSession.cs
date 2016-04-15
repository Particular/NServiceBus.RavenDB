namespace NServiceBus.RavenDB.SessionManagement
{
    using System;
    using NServiceBus.Features;
    using NServiceBus.Persistence;
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
            // Check to see if the user provided us with a shared session to work with before we go and create our own to inject into the pipeline
            var getSessionFunc = context.Settings.GetOrDefault<Func<IDocumentSession>>(RavenDbSettingsExtensions.SharedSessionSettingsKey);
            if (getSessionFunc != null)
            {
                context.Container.ConfigureComponent<ProvidedSessionBehavior>(DependencyLifecycle.InstancePerCall)
                    .ConfigureProperty(x => x.GetSession, getSessionFunc);
                context.Pipeline.Register<ProvidedSessionBehavior.Registration>();
                return;
            }

            var store = DocumentStoreManager.GetDocumentStore<StorageType.Sagas>(context.Settings);

            context.Container.ConfigureComponent<Installer>(DependencyLifecycle.InstancePerCall)
                .ConfigureProperty(c => c.StoreToInstall, store);

            context.Container.ConfigureComponent<RavenSessionProvider>(DependencyLifecycle.InstancePerCall);
            context.Container.RegisterSingleton<IDocumentStoreWrapper>(new DocumentStoreWrapper(store));
            context.Pipeline.Register<OpenSessionBehavior.Registration>();
        }
    }
}