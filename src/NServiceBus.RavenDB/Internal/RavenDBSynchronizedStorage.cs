using NServiceBus.Extensibility;
using NServiceBus.Persistence;
using NServiceBus.SagaPersisters.RavenDB;
using NServiceBus.Settings;
using Raven.Client;
using System.Threading.Tasks;

namespace NServiceBus.RavenDB.Internal
{
    using System;

    class RavenDBSynchronizedStorage : ISynchronizedStorage
    {
        ReadOnlySettings settings;

        public RavenDBSynchronizedStorage(ReadOnlySettings settings)
        {
            this.settings = settings;
        }

        public Task<CompletableSynchronizedStorageSession> OpenSession(ContextBag contextBag)
        {
            var session = GetRavenSession(contextBag);

            var syncronizedStorageSession = new RavenDBSynchronizedStorageSession(session, true);

            return Task.FromResult((CompletableSynchronizedStorageSession)syncronizedStorageSession);
        }

        static IAsyncDocumentSession GetRavenSession(ReadOnlyContextBag contextBag)
        {
            Func<IAsyncDocumentSession> sessionFunction;
            contextBag.TryGet(RavenDbSettingsExtensions.SharedAsyncSessionSettingsKey, out sessionFunction);
            if (sessionFunction != null)
            {
                return sessionFunction();
            }
            IAsyncDocumentSession session;
            contextBag.TryGet(out session);
            if (session == null)
            {
                throw new InvalidOperationException("Failed to retrieve an IAsyncDocumentSession, this is usually because the Saga and Outbox features are not in use.");
            }
            return session;
        }
    }

}
