namespace NServiceBus.RavenDB.Tests.DocumentIds
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.RavenDB.Persistence;
    using NServiceBus.RavenDB.Persistence.TimeoutPersister;
    using NUnit.Framework;
    using TimeoutData = NServiceBus.Timeout.Core.TimeoutData;

    [TestFixture]
    class DocumentIdStrategyTests : DocumentIdConventionTestBase
    {
        [Test]
        public void EnsureMappingDocumentIsUsed()
        {
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

                        var storeAccessor = new StoreAccessor(store);
                        var persister = new RavenTimeoutPersistence(storeAccessor);

                        persister.Add(new TimeoutData
                        {
                            Destination = new Address(EndpointName, "localhost"),
                            Headers = new Dictionary<string, string>(),
                            OwningTimeoutManager = EndpointName,
                            SagaId = Guid.NewGuid(),
                            Time = DateTime.UtcNow
                        });
                    }
                }
            }
        }

        [Test]
        public void EnsureMappingFailsWithoutIndex()
        {
            using (var db = new ReusableDB())
            {

                using (var store = db.NewStore())
                {
                    ApplyTestConventions(store, ConventionType.RavenDefault);
                    store.Initialize();

                    // Remove the index to make sure the conventions will throw
                    store.DatabaseCommands.DeleteIndex("Raven/DocumentsByEntityName");

                    var storeAccessor = new StoreAccessor(store);
                    var persister = new RavenTimeoutPersistence(storeAccessor);

                    var exception = Assert.Throws<InvalidOperationException>(() =>
                    {
                        persister.Add(new TimeoutData
                        {
                            Destination = new Address(EndpointName, "localhost"),
                            Headers = new Dictionary<string, string>(),
                            OwningTimeoutManager = EndpointName,
                            SagaId = Guid.NewGuid(),
                            Time = DateTime.UtcNow
                        });
                    });

                    Console.WriteLine($"Got expected Exception: {exception.Message}");
                }
            }
        }
    }
}
