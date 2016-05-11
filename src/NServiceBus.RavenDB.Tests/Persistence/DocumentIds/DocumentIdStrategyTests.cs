namespace NServiceBus.RavenDB.Tests.Persistence.DocumentIds
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.TimeoutPersisters.RavenDB;
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

                        var persister = new TimeoutPersister
                        {
                            DocumentStore = store,
                            EndpointName = EndpointName
                        };

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
    }
}
