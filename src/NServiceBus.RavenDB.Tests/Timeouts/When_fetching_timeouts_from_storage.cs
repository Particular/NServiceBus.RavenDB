namespace NServiceBus.RavenDB.Tests.Timeouts
{
    using System;
    using System.Collections.Generic;
    using NUnit.Framework;
    using Support;
    using Timeout.Core;
    using TimeoutPersisters.RavenDB;

    public class When_fetching_timeouts_from_storage
    {
        [Test]
        public void Should_return_the_complete_list_of_timeouts()
        {
            using (var store = DocumentStoreBuilder.Build())
            {
                new TimeoutsIndex().Execute(store);

                var persister = new TimeoutPersister
                                {
                                    DocumentStore = store,
                                    EndpointName = "MyTestEndpoint",
                                };

                const int numberOfTimeoutsToAdd = 10;

                for (var i = 0; i < numberOfTimeoutsToAdd; i++)
                {
                    persister.Add(new TimeoutData
                    {
                        Time = DateTime.UtcNow.AddHours(-1),
                        Destination = new Address("timeouts", RuntimeEnvironment.MachineName),
                        SagaId = Guid.NewGuid(),
                        State = new byte[] { 0, 0, 133 },
                        Headers = new Dictionary<string, string> { { "Bar", "34234" }, { "Foo", "aString1" }, { "Super", "aString2" } },
                        OwningTimeoutManager = "MyTestEndpoint",
                    });
                }
                DateTime nextTimeToRunQuery;
                Assert.AreEqual(numberOfTimeoutsToAdd, persister.GetNextChunk(DateTime.UtcNow.AddYears(-3), out nextTimeToRunQuery).Count);
            }
        }

        [Test]
        public void Should_return_the_next_time_of_retrieval()
        {
            using (var store = DocumentStoreBuilder.Build())
            {
                new TimeoutsIndex().Execute(store);

                var persister = new TimeoutPersister
                {
                    DocumentStore = store,
                    EndpointName = "MyTestEndpoint",
                };

                var nextTime = DateTime.UtcNow.AddHours(1);

                persister.Add(new TimeoutData
                {
                    Time = nextTime,
                    Destination = new Address("timeouts", RuntimeEnvironment.MachineName),
                    SagaId = Guid.NewGuid(),
                    State = new byte[] { 0, 0, 133 },
                    Headers = new Dictionary<string, string> { { "Bar", "34234" }, { "Foo", "aString1" }, { "Super", "aString2" } },
                    OwningTimeoutManager = "MyTestEndpoint",
                });



                DateTime nextTimeToRunQuery;
                persister.GetNextChunk(DateTime.UtcNow.AddYears(-3), out nextTimeToRunQuery);

                Assert.IsTrue((nextTime - nextTimeToRunQuery).TotalSeconds < 1);
            }
        }
    }
}
