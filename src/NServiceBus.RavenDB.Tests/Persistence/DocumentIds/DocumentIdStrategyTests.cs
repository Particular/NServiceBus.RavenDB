namespace NServiceBus.RavenDB.Tests.Persistence.DocumentIds
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Extensibility;
    using NServiceBus.Persistence.RavenDB;
    using NUnit.Framework;
    using TimeoutData = NServiceBus.Timeout.Core.TimeoutData;

    [TestFixture]
    class DocumentIdStrategyTests : DocumentIdConventionTestBase
    {
        [Test]
        public void EnsureMappingDocumentIsUsed()
        {
            var context = new ContextBag();

            using (var db = new ReusableDB())
            {
                for (var i = 0; i < 5; i++)
                {
                    using (var store = db.NewStore())
                    {
                        ApplyTestConventions(store, ConventionType.RavenDefault);
                        store.Initialize();

                        if (i > 0)
                        {
                            // On every iteration after the first, remove the index so that operations
                            // will throw if the mapping document does not exist.
                            store.DatabaseCommands.DeleteIndex("Raven/DocumentsByEntityName");
                        }

                        var persister = new TimeoutPersister(store);

                        persister.Add(new TimeoutData
                        {
                            Destination = EndpointName,
                            Headers = new Dictionary<string, string>(),
                            OwningTimeoutManager = EndpointName,
                            SagaId = Guid.NewGuid(),
                            Time = DateTime.UtcNow
                        }, context).Wait();
                    }
                }
            }
        }

        [Test]
        public void EnsureMappingFailsWithoutIndex()
        {
            var context = new ContextBag();

            using (var db = new ReusableDB())
            {
                using (var store = db.NewStore())
                {
                    ApplyTestConventions(store, ConventionType.RavenDefault);
                    store.Initialize();

                    // Remove the index to make sure the conventions will throw
                    store.DatabaseCommands.DeleteIndex("Raven/DocumentsByEntityName");

                    var persister = new TimeoutPersister(store);
                   
                    var exception = Assert.Throws<AggregateException>(() =>
                    {
                        persister.Add(new TimeoutData
                        {
                            Destination = EndpointName,
                            Headers = new Dictionary<string, string>(),
                            OwningTimeoutManager = EndpointName,
                            SagaId = Guid.NewGuid(),
                            Time = DateTime.UtcNow
                        }, context).Wait();
                    });

                    Assert.IsInstanceOf<InvalidOperationException>(exception.GetBaseException());
                    Console.WriteLine($"Got expected Exception: {exception.Message}");
                }
            }
        }
    }
}
