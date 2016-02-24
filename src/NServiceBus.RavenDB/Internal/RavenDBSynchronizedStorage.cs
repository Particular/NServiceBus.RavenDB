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
            var ownership = GetRavenSession(contextBag);

            return Task.FromResult((CompletableSynchronizedStorageSession)new RavenDBSynchronizedStorageSession(ownership.Session, ownership.Owns));
        }

        SessionOwnership GetRavenSession(ContextBag contextBag)
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

            session = contextBag.Get<IAsyncDocumentSession>();

            if (session == null)
            {
                throw new Exception("IAsyncDocumentSession is not configured in the container for the incoming message pipeline.");
            }

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
