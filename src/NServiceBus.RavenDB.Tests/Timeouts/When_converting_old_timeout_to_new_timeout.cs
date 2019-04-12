namespace NServiceBus.RavenDB.Tests.Timeouts
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Persistence.RavenDB;
    using NServiceBus.Support;
    using NUnit.Framework;
    using Raven.Client.Documents;
    using LegacyAddress = NServiceBus.RavenDB.Tests.LegacyAddress;
    using TimeoutData = NServiceBus.Timeout.Core.TimeoutData;

    [TestFixture]
    public class When_converting_old_timeout_to_new_timeout : RavenDBPersistenceTestBase
    {
        public override void SetUp()
        {
            base.SetUp();

            FakeLegacyTimoutDataClrTypeConversionListener.Install(store);
            TimeoutDataV1toV2Converter.Register((DocumentStore)store);

            persister = new TimeoutPersister(store);
        }

        [Test]
        public async Task Should_allow_old_timeouts()
        {
            var headers = new Dictionary<string, string>
            {
                {"Bar", "34234"},
                {"Foo", "aString1"},
                {"Super", "aString2"}
            };

            var timeout = new LegacyTimeoutData
            {
                Time = DateTime.UtcNow.AddHours(-1),
                Destination = new LegacyAddress("timeouts", RuntimeEnvironment.MachineName),
                SagaId = Guid.NewGuid(),
                State = new byte[]
                {
                    1,
                    1,
                    133,
                    200
                },
                Headers = headers,
                OwningTimeoutManager = "MyTestEndpoint"
            };
            var context = new ContextBag();

            var session = store.OpenAsyncSession();
            await session.StoreAsync(timeout);
            await session.SaveChangesAsync();

            Assert.True(await persister.TryRemove(timeout.Id, context));
        }

        [Test]
        public async Task Should_allow_old_timeouts_without_machine_name()
        {
            var headers = new Dictionary<string, string>
            {
                {"Bar", "34234"},
                {"Foo", "aString1"},
                {"Super", "aString2"}
            };

            var timeout = new LegacyTimeoutData
            {
                Time = DateTime.UtcNow.AddHours(-1),
                Destination = new LegacyAddress("timeouts", null),
                SagaId = Guid.NewGuid(),
                State = new byte[]
                {
                    1,
                    1,
                    133,
                    200
                },
                Headers = headers,
                OwningTimeoutManager = "MyTestEndpoint"
            };
            var context = new ContextBag();

            var session = store.OpenAsyncSession();
            await session.StoreAsync(timeout);
            await session.SaveChangesAsync();

            Assert.True(await persister.TryRemove(timeout.Id, context));
        }

        [Test]
        // This test makes sure that the conversion listener doesn't destroy new documents
        public async Task Should_allow_new_timeouts()
        {
            var headers = new Dictionary<string, string>
            {
                {"Bar", "34234"},
                {"Foo", "aString1"},
                {"Super", "aString2"}
            };

            var timeout = new TimeoutData
            {
                Time = DateTime.UtcNow.AddHours(-1),
                Destination = "timeouts" + "@" + RuntimeEnvironment.MachineName,
                SagaId = Guid.NewGuid(),
                State = new byte[]
                {
                    1,
                    1,
                    133,
                    200
                },
                Headers = headers,
                OwningTimeoutManager = "MyTestEndpoint"
            };

            var session = store.OpenAsyncSession();
            await session.StoreAsync(timeout);
            await session.SaveChangesAsync();
            var context = new ContextBag();

            var retreievedTimeout = await persister.Peek(timeout.Id, context);

            Assert.AreEqual(timeout.Destination, retreievedTimeout.Destination);
        }

        TimeoutPersister persister;
    }
}