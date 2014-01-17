namespace NServiceBus.RavenDB
{
    using System;
    using NServiceBus.RavenDB.Persistence;
    using Raven.Client;

    class UserControlledSessionProvider:ISessionProvider
    {
        readonly Func<IDocumentSession> sessionProvider;

        public UserControlledSessionProvider(Func<IDocumentSession> sessionProvider)
        {
            this.sessionProvider = sessionProvider;
        }

        public IDocumentSession Session { get { return sessionProvider(); } }
    }
}