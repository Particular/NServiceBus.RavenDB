namespace NServiceBus.RavenDB.Tests.Timeouts
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.Persistence.RavenDB;
    using NUnit.Framework;
    using Raven.Client.Indexes;

    [TestFixture]
    public class When_using_transformers_with_timeoutdatav1tov2converter : RavenDBPersistenceTestBase
    {
        public override void SetUp()
        {
            base.SetUp();

            new DummyDataTransfomer().Execute(store);

            store.Listeners.RegisterListener(new FakeLegacyTimoutDataClrTypeConversionListener());
            store.Listeners.RegisterListener(new TimeoutDataV1toV2Converter());
        }

        [Test]
        public async Task Should_not_fail()
        {
            var session = store.OpenAsyncSession();

            await session.StoreAsync(new Dummy() { Id = "dummies/test" });
            await session.SaveChangesAsync();

            var result = await session.LoadAsync<DummyDataTransfomer.Result>("dummies/test", typeof(DummyDataTransfomer));
            Assert.IsNotNull(result);
        }

        public class Dummy
        {
            public string Id { get; set; }
        }


        public class DummyDataTransfomer : AbstractTransformerCreationTask<Dummy>
        {
            public class Result
            {
                public Guid Id { get; set; }
                public DateTime DoneAt { get; set; }
            }

            public DummyDataTransfomer()
            {
                TransformResults = results => from item in results
                                              select new
                                              {
                                                  OrderId = item.Id
                                              };
            }
        }
    }
}