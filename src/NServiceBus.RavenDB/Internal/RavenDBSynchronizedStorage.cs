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
            Func<IDocumentSession> sessionFunction;
            settings.TryGet(RavenDbSettingsExtensions.SharedSessionSettingsKey, out sessionFunction);
            if (sessionFunction != null)
            {
                return new SessionOwnership(false, sessionFunction());
            }

            IDocumentSession session;
            if (settings.TryGet(out session))
            {
                return new SessionOwnership(true, session);
            }

            var store = settings.GetOrDefault<IDocumentStore>(RavenDbSettingsExtensions.DocumentStoreSettingsKey)
                        ?? SharedDocumentStore.Get(settings);

            session = store.OpenSession();

            return new SessionOwnership(true, session);
        }

        class SessionOwnership
        {
            public SessionOwnership(bool ownsSession, IDocumentSession session)
            {
                Session = session;
                Owns = ownsSession;

            }
            public IDocumentSession Session { get; }
            public bool Owns { get; }
        }
    }

}
