namespace NServiceBus.RavenDB.Tests.Persistence
{
    using NServiceBus.Persistence.RavenDB;
    using NServiceBus.Testing;
    using NUnit.Framework;
    using Raven.Tests.Helpers;

    public class RavenSessionExtensionTests : RavenTestBase
    {
        [Test]
        public void CanGetNormalSession()
        {
            var store = NewDocumentStore().Initialize();
            var session = store.OpenAsyncSession();

            var storageSession = new RavenDBSynchronizedStorageSession(session, true);

            var session2 = storageSession.RavenSession();

            Assert.AreEqual(session, session2);
        }

        [Test]
        public void CanGetTestableSession()
        {
            var store = NewDocumentStore().Initialize();
            var session = store.OpenAsyncSession();

            var storageSession = new TestableRavenStorageSession(session);

            var session2 = storageSession.RavenSession();

            Assert.AreEqual(session, session2);
        }
    }
}
