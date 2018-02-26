namespace NServiceBus.RavenDB.Tests
{
    using System.Collections.Generic;
    using NServiceBus.Persistence.RavenDB;
    using NUnit.Framework;
    using Raven.Client;
    using Raven.Tests.Helpers;

    public class RavenDBPersistenceTestBase : RavenTestBase
    {
        protected IDocumentStore store;

        [SetUp]
        public virtual void SetUp()
        {
            store = NewDocumentStore();
        }

        [TearDown]
        public virtual void TearDown()
        {
            store.Dispose();
        }

        internal IOpenRavenSessionsInPipeline CreateTestSessionOpener()
        {
            return new TestOpenSessionsInPipeline(this.store);
        }

        class TestOpenSessionsInPipeline : IOpenRavenSessionsInPipeline
        {
            IDocumentStore store;

            public TestOpenSessionsInPipeline(IDocumentStore store)
            {
                this.store = store;
            }

            public IDocumentSession OpenSession(IDictionary<string, string> headers)
            {
                return store.OpenSession();
            }

        }
    }
}