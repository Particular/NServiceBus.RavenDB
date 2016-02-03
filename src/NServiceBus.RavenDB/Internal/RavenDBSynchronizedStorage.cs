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
            var ownership = GetRavenSession();

            return Task.FromResult((CompletableSynchronizedStorageSession)new RavenDBSynchronizedStorageSession(ownership.Session, ownership.Owns));
        }

        SessionOwnership GetRavenSession()
        {
            Func<IAsyncDocumentSession> sessionFunction;
            settings.TryGet(RavenDbSettingsExtensions.SharedAsyncSessionSettingsKey, out sessionFunction);
            if (sessionFunction != null)
            {
                return new SessionOwnership(false, sessionFunction());
            }

            IAsyncDocumentSession session;
            if (settings.TryGet(out session))
            {
                return new SessionOwnership(true, session);
            }

            var store = settings.GetOrDefault<IDocumentStore>(RavenDbSettingsExtensions.DocumentStoreSettingsKey)
                        ?? SharedDocumentStore.Get(settings);

            session = store.OpenAsyncSession();

            return new SessionOwnership(true, session);
        }

        class SessionOwnership
        {
            public SessionOwnership(bool ownsSession, IAsyncDocumentSession session)
            {
                Session = session;
                Owns = ownsSession;

            }
            public IAsyncDocumentSession Session { get; }
            public bool Owns { get; }
        }
    }

}
