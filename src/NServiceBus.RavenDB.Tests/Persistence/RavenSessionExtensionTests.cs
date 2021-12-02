namespace NServiceBus.RavenDB.Tests.Persistence
{
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Persistence.RavenDB;
    using NServiceBus.Testing;
    using NUnit.Framework;
    using Raven.Client.Documents.Session;

    public class RavenSessionExtensionTests
    {
        [Test]
        public async Task CanGetNormalSession()
        {
            using (var db = new ReusableDB())
            using (var store = db.NewStore().Initialize())
            {
                await db.EnsureDatabaseExists(store);

                var sessionOptions = new SessionOptions
                {
                    TransactionMode = db.UseClusterWideTransactions ? TransactionMode.ClusterWide : TransactionMode.SingleNode
                };
                var session = store.OpenAsyncSession(sessionOptions);

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

                var sessionOptions = new SessionOptions
                {
                    TransactionMode = db.UseClusterWideTransactions ? TransactionMode.ClusterWide : TransactionMode.SingleNode
                };
                var session = store.OpenAsyncSession(sessionOptions);

                var storageSession = new TestableRavenStorageSession(session);

                var session2 = storageSession.RavenSession();

                Assert.AreEqual(session, session2);
            }
        }
    }
}
