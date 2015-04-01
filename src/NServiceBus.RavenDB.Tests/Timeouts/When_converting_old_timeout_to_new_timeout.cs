namespace NServiceBus.RavenDB.Tests.Timeouts
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.RavenDB.Timeouts;
    using NServiceBus.Support;
    using NServiceBus.TimeoutPersisters.RavenDB;
    using NUnit.Framework;
    using TimeoutData = NServiceBus.Timeout.Core.TimeoutData;

    [TestFixture]
    public class When_converting_old_timeout_to_new_timeout : RavenDBPersistenceTestBase
    {
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

            var session = store.OpenSession();
            session.Store(timeout);
            session.SaveChanges();

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

            var session = store.OpenSession();
            session.Store(timeout);
            session.SaveChanges();

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
                Destination = "timouts" + "@" + RuntimeEnvironment.MachineName,
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

            var exception = await Catch(async () => { await persister.Add(timeout, context); });
            Assert.Null(exception);
        }

        TimeoutPersister persister;
    }
}