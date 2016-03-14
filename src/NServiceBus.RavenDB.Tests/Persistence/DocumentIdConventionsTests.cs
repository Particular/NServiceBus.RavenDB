namespace NServiceBus.RavenDB.Tests.Persistence
{
    using System;
    using System.Threading;
    using NServiceBus.RavenDB.Internal;
    using NUnit.Framework;
    using Raven.Tests.Helpers;

    [TestFixture]
    class DocumentIdConventionsTests : RavenTestBase
    {
        [Test]
        public void Print()
        {
            var store = NewDocumentStore();

            using (var session = store.OpenSession())
            {
                session.Store(new Doof { OrderId = 42 });
                session.SaveChanges();
            }

            while (store.DatabaseCommands.GetStatistics().StaleIndexes.Length != 0)
            {
                Console.WriteLine("Waiting for indexing...");
                Thread.Sleep(10);
            }

            var conventions = new DocumentIdConventions(store, new Type[0]);

            conventions.FindTypeTagName(typeof(DocumentIdConventionsTests));
        }

        public class Doof
        {
            public int OrderId { get; set; }
        }
    }
}
