namespace NServiceBus.RavenDB.Tests.Persistence
{
    using NServiceBus.Persistence.RavenDB;
    using NServiceBus.Testing;
    using NUnit.Framework;

    public class RavenSessionExtensionTests
    {
        [Test]
        public void CanGetNormalSession()
        {
            using (var db = new ReusableDB())
            {
                var store = db.NewStore().Initialize();
                var session = store.OpenAsyncSession();

                var storageSession = new RavenDBSynchronizedStorageSession(session, true);

                var session2 = storageSession.RavenSession();

                Assert.AreEqual(session, session2);
            }
        }

        [Test]
        public void CanGetTestableSession()
        {
            using (var db = new ReusableDB())
            {
                var store = db.NewStore().Initialize();
                var session = store.OpenAsyncSession();

                var storageSession = new TestableRavenStorageSession(session);

                var session2 = storageSession.RavenSession();

                Assert.AreEqual(session, session2);
            }
        }
    }
}
