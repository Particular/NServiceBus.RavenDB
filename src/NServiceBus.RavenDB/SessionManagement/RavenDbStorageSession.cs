namespace NServiceBus.Persistence.RavenDB
{
    using System;
    using NServiceBus.Features;
    using Raven.Client;

    class RavenDbStorageSession : Feature
    {
        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<RavenDBSynchronizedStorageAdapter>(DependencyLifecycle.SingleInstance);
            context.Container.ConfigureComponent<RavenDBSynchronizedStorage>(DependencyLifecycle.SingleInstance);

            // Check to see if the user provided us with a shared session to work with before we go and create our own to inject into the pipeline
            var getAsyncSessionFunc = context.Settings.GetOrDefault<Func<IAsyncDocumentSession>>(RavenDbSettingsExtensions.SharedAsyncSessionSettingsKey);
            if (getAsyncSessionFunc != null)
            {
                context.Pipeline.Register("ProvidedRavenDbAsyncSession", new ProvidedAsyncSessionBehavior(getAsyncSessionFunc), "Makes sure that there is a RavenDB IAsyncDocumentSession available on the pipeline");
                return;
            }

            var store = DocumentStoreManager.GetDocumentStore<StorageType.Sagas>(context.Settings);
            context.Pipeline.Register("OpenRavenDbAsyncSession", new OpenAsyncSessionBehavior(new DocumentStoreWrapper(store)), "Makes sure that there is a RavenDB IAsyncDocumentSession available on the pipeline");
        }
    }
}