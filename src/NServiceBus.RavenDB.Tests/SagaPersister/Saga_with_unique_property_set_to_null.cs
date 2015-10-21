using System;
using NServiceBus;
using NServiceBus.RavenDB.Tests;
using NServiceBus.SagaPersisters.RavenDB;
using NUnit.Framework;
using Raven.Client;

[TestFixture]
public class Saga_with_unique_property_set_to_null : RavenDBPersistenceTestBase
{
    [Test]
    public void should_throw_a_ArgumentNullException()
    {
        var saga1 = new SagaData
        {
            Id = Guid.NewGuid(),
            UniqueString = null
        };

        IDocumentSession session;
        var context = this.CreateContextWithSessionPresent(out session);
        var persister = new SagaPersister();

        Assert.Throws<ArgumentNullException>(() =>
        {
            persister.Save(saga1, this.CreateMetadata<SomeSaga>(saga1), context);
            session.SaveChanges();
        });
    }

    class SomeSaga : Saga<SagaData>
    {
        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
            mapper.ConfigureMapping<Message>(m => m.UniqueString).ToSaga(s => s.UniqueString);
        }

        class Message
        {
            public string UniqueString { get; set; }
        }
    }

    class SagaData : IContainSagaData
    {
        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        public string UniqueString { get; set; }
        public Guid Id { get; set; }
        public string Originator { get; set; }
        public string OriginalMessageId { get; set; }
    }
}