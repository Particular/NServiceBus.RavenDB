using NServiceBus.Extensibility;
using NServiceBus.Persistence;
using NServiceBus.SagaPersisters.RavenDB;
using Raven.Client;
using System.Threading.Tasks;

namespace NServiceBus.RavenDB.Internal
{
    using System;

    class RavenDBSynchronizedStorage : ISynchronizedStorage
    {
        public Task<CompletableSynchronizedStorageSession> OpenSession(ContextBag contextBag)
        {
            var ownership = GetRavenSession(contextBag);
            var synchronizedStorageSession = new RavenDBSynchronizedStorageSession(ownership.Session, ownership.Owns);
            return Task.FromResult((CompletableSynchronizedStorageSession)synchronizedStorageSession);
        }

        SessionOwnership GetRavenSession(ContextBag contextBag)
        {
            Func<IAsyncDocumentSession> sessionFunction;
            contextBag.TryGet(out sessionFunction);
            if (sessionFunction != null)
            {
                return new SessionOwnership(false, sessionFunction());
            }

            IAsyncDocumentSession session;
            if (contextBag.TryGet(out session))
            {
                return new SessionOwnership(true, session);
            }

            throw new Exception("IAsyncDocumentSession could not be retrieved for the incoming message pipeline.");
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
