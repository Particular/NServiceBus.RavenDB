﻿namespace NServiceBus.RavenDB.SessionManagement
{
    using System;
    using NServiceBus.Features;
    using NServiceBus.Persistence;
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

            var store = DocumentStoreManager.GetDocumentStore<StorageType.Sagas>(context.Settings);

            context.Container.ConfigureComponent<IDocumentStoreWrapper>(b => new DocumentStoreWrapper(store), DependencyLifecycle.SingleInstance);
            context.Pipeline.Register<OpenAsyncSessionBehavior.Registration>();
        }
    }
}