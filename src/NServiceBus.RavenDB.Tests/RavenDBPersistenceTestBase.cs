namespace NServiceBus.RavenDB.Tests
{
    using System.Collections.Generic;
    using NUnit.Framework;
    using Raven.Client;
    using Raven.Tests.Helpers;

    public class RavenDBPersistenceTestBase : RavenTestBase
    {
        List<IDocumentSession> sessions = new List<IDocumentSession>();
        protected IDocumentStore store;

        [SetUp]
        public virtual void SetUp()
        {
            store = NewDocumentStore();
        }

        [TearDown]
        public virtual void TearDown()
        {
            sessions.ForEach(s => s.Dispose());
            sessions.Clear();
            store.Dispose();
        }

        protected internal IDocumentSession OpenSession()
        {
            var documentSession = store.OpenSession();
            documentSession.Advanced.AllowNonAuthoritativeInformation = false;
            documentSession.Advanced.UseOptimisticConcurrency = true;
            sessions.Add(documentSession);
            return documentSession;
        }
    }
}
