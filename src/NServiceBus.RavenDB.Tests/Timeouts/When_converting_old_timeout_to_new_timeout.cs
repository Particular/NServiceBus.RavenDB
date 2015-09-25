namespace NServiceBus.RavenDB.Tests.Timeouts
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.RavenDB.Timeouts;
    using NServiceBus.Support;
    using NServiceBus.Timeout.Core;
    using NServiceBus.TimeoutPersisters.RavenDB;
    using NUnit.Framework;

    [TestFixture]
    public class When_converting_old_timeout_to_new_timeout : RavenDBPersistenceTestBase
    {
        TimeoutPersister persister;

        public override void SetUp()
        {
            base.SetUp();

            store.Listeners.RegisterListener(new FakeLegacyTimoutDataClrTypeConversionListener());
            store.Listeners.RegisterListener(new TimeoutDataV1toV2Converter());

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
                State = new byte[] { 1, 1, 133, 200 },
                Headers = headers,
                OwningTimeoutManager = "MyTestEndpoint",
            };
            var options = new TimeoutPersistenceOptions(new ContextBag());

            var session = store.OpenAsyncSession();
            await session.StoreAsync(timeout);
            await session.SaveChangesAsync();

            Timeout.Core.TimeoutData removedTimeout = null;
            var exception = await Catch<Exception>(async () => { removedTimeout = await persister.Remove(timeout.Id, options); });
            Assert.IsNull(exception);
            Assert.AreEqual("timeouts" + "@" + RuntimeEnvironment.MachineName, removedTimeout.Destination);
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
                State = new byte[] { 1, 1, 133, 200 },
                Headers = headers,
                OwningTimeoutManager = "MyTestEndpoint",
            };
            var options = new TimeoutPersistenceOptions(new ContextBag());

            var session = store.OpenSession();
            session.Store(timeout);
            session.SaveChanges();

            Timeout.Core.TimeoutData removedTimeout = null;
            var exception = await Catch<Exception>(async () => { removedTimeout = await persister.Remove(timeout.Id, options); });
            Assert.IsNull(exception);
            Assert.AreEqual("timeouts", removedTimeout.Destination);
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

            var timeout = new Timeout.Core.TimeoutData
            {
                Time = DateTime.UtcNow.AddHours(-1),
                Destination = "timouts" + "@" + RuntimeEnvironment.MachineName,
                SagaId = Guid.NewGuid(),
                State = new byte[] { 1, 1, 133, 200 },
                Headers = headers,
                OwningTimeoutManager = "MyTestEndpoint",
            };
            var options = new TimeoutPersistenceOptions(new ContextBag());

            var exception = await Catch<Exception>(async () => { await persister.Add(timeout, options); });
            Assert.IsNull(exception);
        }
    }
}