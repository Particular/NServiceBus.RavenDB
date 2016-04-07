namespace NServiceBus.RavenDB.SessionManagement
{
    using Raven.Client;

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