using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.RavenDB.Tests;
using NServiceBus.SagaPersisters.RavenDB;
using NUnit.Framework;
using Raven.Client;

[TestFixture]
public class Saga_with_unique_property_set_to_null : RavenDBPersistenceTestBase
{
    [Test]
    public async Task should_throw_a_ArgumentNullException()
    {
        var saga1 = new SagaData
            {
                Id = Guid.NewGuid(),
                UniqueString = null
            };

        IAsyncDocumentSession session;
        var options = this.NewSagaPersistenceOptions<SomeSaga>(out session);
        var persister = new SagaPersister();

        var exception = await Catch<ArgumentNullException>(async () =>
        {
            await persister.Save(saga1, options);
            await session.SaveChangesAsync();
        });

        Assert.IsInstanceOf<ArgumentNullException>(exception);
    }

    class SomeSaga : Saga<SagaData>
    {
        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
            mapper.ConfigureMapping<Message>(m => m.UniqueString).ToSaga(s => s.UniqueString);
        }

        private class Message
        {
            public string UniqueString { get; set; }
        }
    }

    class SagaData : IContainSagaData
    {
        public Guid Id { get; set; }
        public string Originator { get; set; }
        public string OriginalMessageId { get; set; }
        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        public string UniqueString { get; set; }

    }
}