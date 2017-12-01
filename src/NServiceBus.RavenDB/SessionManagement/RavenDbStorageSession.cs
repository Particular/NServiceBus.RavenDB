namespace NServiceBus.Persistence.RavenDB
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Features;
    using NServiceBus.Transport;
    using Raven.Client;

    class RavenDbStorageSession : Feature
    {
        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<RavenDBSynchronizedStorageAdapter>(DependencyLifecycle.SingleInstance);
            context.Container.ConfigureComponent<RavenDBSynchronizedStorage>(DependencyLifecycle.SingleInstance);

            IOpenRavenSessionsInPipeline sessionCreator;

            // Check to see if the user provided us with a shared session to work with before we go and create our own to inject into the pipeline
            var getAsyncSessionFunc = context.Settings.GetOrDefault<Func<IAsyncDocumentSession>>(RavenDbSettingsExtensions.SharedAsyncSessionSettingsKey);
            if (getAsyncSessionFunc != null)
            {
                sessionCreator = new OpenRavenSessionByCustomDelegate(getAsyncSessionFunc);
            }
            else
            {
                var store = DocumentStoreManager.GetDocumentStore<StorageType.Sagas>(context.Settings);
                var storeWrapper = new DocumentStoreWrapper(store);

                var dbNameConvention = context.Settings.GetOrDefault<Func<IDictionary<string, string>, string>>("RavenDB.SetMessageToDatabaseMappingConvention");
                sessionCreator = new OpenRavenSessionsByDatabaseName(storeWrapper, dbNameConvention);
            }

            context.Container.RegisterSingleton(sessionCreator);
            context.Pipeline.Register("OpenRavenDbAsyncSession", new OpenAsyncSessionBehavior(sessionCreator), "Makes sure that there is a RavenDB IAsyncDocumentSession available on the pipeline");
        }
    }

    interface IOpenRavenSessionsInPipeline
    {
        IAsyncDocumentSession OpenSession(IncomingMessage message);
    }

    class OpenRavenSessionsByDatabaseName : IOpenRavenSessionsInPipeline
    {
        IDocumentStoreWrapper documentStoreWrapper;
        Func<IDictionary<string, string>, string> getDatabaseName;

        public OpenRavenSessionsByDatabaseName(IDocumentStoreWrapper documentStoreWrapper, Func<IDictionary<string, string>, string> getDatabaseName = null)
        {
            this.documentStoreWrapper = documentStoreWrapper;
            this.getDatabaseName = getDatabaseName ?? (context => string.Empty);
        }

        public IAsyncDocumentSession OpenSession(IncomingMessage message)
        {
            var databaseName = getDatabaseName(message.Headers);
            var documentSession = string.IsNullOrEmpty(databaseName)
                ? documentStoreWrapper.DocumentStore.OpenAsyncSession()
                : documentStoreWrapper.DocumentStore.OpenAsyncSession(databaseName);

            documentSession.Advanced.AllowNonAuthoritativeInformation = false;
            documentSession.Advanced.UseOptimisticConcurrency = true;

            return documentSession;
        }
    }

    class OpenRavenSessionByCustomDelegate : IOpenRavenSessionsInPipeline
    {
        Func<IAsyncDocumentSession> getAsyncSession;

        public OpenRavenSessionByCustomDelegate(Func<IAsyncDocumentSession> getAsyncSession)
        {
            this.getAsyncSession = getAsyncSession;
        }

        public IAsyncDocumentSession OpenSession(IncomingMessage message)
        {
            return getAsyncSession();
        }
    }


}