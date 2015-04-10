namespace NServiceBus.RavenDB.Tests
{
    using NUnit.Framework;
    using Raven.Client;
    using Raven.Tests.Helpers;

    public class RavenDBPersistenceTestBase : RavenTestBase
    {
        protected IDocumentStore store;

        [SetUp]
        public void SetUp()
        {
            store = NewDocumentStore();
        }

        [TearDown]
        public void TearDown()
        {
            store.Dispose();
        }
    }
}
