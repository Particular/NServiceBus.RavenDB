namespace NServiceBus.RavenDB.Tests.Persistence
{
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Persistence.RavenDB;
    using NServiceBus.Testing;
    using NUnit.Framework;

    public class RavenSessionExtensionTests
    {
        [Test]
        public async Task CanGetNormalSession()
        {
            using (var db = new ReusableDB())
            using (var store = db.NewStore().Initialize())
            {
                await db.EnsureDatabaseExists(store);

                var session = store.OpenAsyncSession();

                var storageSession = new RavenDBSynchronizedStorageSession(session, new ContextBag());

                var session2 = storageSession.RavenSession();

                Assert.AreEqual(session, session2);
            }
        }

        [Test]
        public async Task CanGetTestableSession()
        {
            using (var db = new ReusableDB())
            using (var store = db.NewStore().Initialize())
            {
                await db.EnsureDatabaseExists(store);

                var session = store.OpenAsyncSession();

                var storageSession = new TestableRavenStorageSession(session);

                var session2 = storageSession.RavenSession();

                Assert.AreEqual(session, session2);
            }
        }
    }
}
