namespace NServiceBus.RavenDB.SessionManagement
{
    using System;
    using NServiceBus.Features;
    using NServiceBus.RavenDB.Internal;
    using Raven.Client;

    class RavenDbStorageSession : Feature
    {
        public RavenDbStorageSession()
        {
            EnableByDefault();
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            // Check to see if the user provided us with a shared session to work with before we go and create our own to inject into the pipeline
            var getAsyncSessionFunc = context.Settings.GetOrDefault<Func<IAsyncDocumentSession>>(RavenDbSettingsExtensions.SharedAsyncSessionSettingsKey);
            if (getAsyncSessionFunc != null)
            {
                context.Container.ConfigureComponent(b => new ProvidedAsyncSessionBehavior(getAsyncSessionFunc), DependencyLifecycle.InstancePerCall);
                context.Pipeline.Register<ProvidedAsyncSessionBehavior.Registration>();
                return;
            }

            var store =
                // Try getting a document store object specific to this Feature that user may have wired in
                context.Settings.GetOrDefault<IDocumentStore>(RavenDbSagaSettingsExtensions.DocumentStoreSettingsKey)
                // Init up a new DocumentStore based on a connection string specific to this feature
                ?? Helpers.CreateDocumentStoreByConnectionStringName(context.Settings, "NServiceBus/Persistence/RavenDB/Saga")
                // Trying pulling a shared DocumentStore set by the user or other Feature
                ?? context.Settings.GetOrDefault<IDocumentStore>(RavenDbSettingsExtensions.DocumentStoreSettingsKey) ?? SharedDocumentStore.Get(context.Settings);

            if (store == null)
            {
                throw new Exception("RavenDB is configured as persistence for Sagas and no DocumentStore instance found");
            }

            // This is required for DTC fix, and this requires RavenDB 2.5 build 2900 or above
            TransactionRecoveryStorageVerifier.ReplaceStorageIfNotSetByUser(store);

            context.Container.ConfigureComponent<IDocumentStoreWrapper>(b => new DocumentStoreWrapper(store), DependencyLifecycle.SingleInstance);
            context.Pipeline.Register<OpenAsyncSessionBehavior.Registration>();
        }
    }
}