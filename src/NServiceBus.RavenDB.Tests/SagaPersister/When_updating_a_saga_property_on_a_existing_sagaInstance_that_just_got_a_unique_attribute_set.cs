using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.RavenDB.Tests;
using NServiceBus.SagaPersisters.RavenDB;
using NUnit.Framework;
using Raven.Client;

[TestFixture]
public class When_updating_a_saga_property_on_a_existing_sagaInstance_that_just_got_a_unique_attribute_set : RavenDBPersistenceTestBase
{
    [Test]
    public async Task It_should_set_the_attribute_and_allow_the_update()
    {
        IAsyncDocumentSession session;
        var options = this.NewSagaPersistenceOptions<SomeSaga>(out session);
        var persister = new SagaPersister();
        var uniqueString = Guid.NewGuid().ToString();

        var anotherUniqueString = Guid.NewGuid().ToString();

        var saga1 = new SagaData
            {
                Id = Guid.NewGuid(),
                UniqueString = uniqueString,
                NonUniqueString = "notUnique"
            };

        await persister.Save(saga1, options);
        await session.SaveChangesAsync();
        session.Dispose();

        using (session = store.OpenAsyncSession())
        {
            //fake that the attribute was just added by removing the metadata
            (await session.Advanced.GetMetadataForAsync(saga1)).Remove(SagaPersister.UniqueValueMetadataKey);
            await session.SaveChangesAsync();
        }

        options = this.NewSagaPersistenceOptions<SomeSaga>(out session);
        var saga = await persister.Get<SagaData>(saga1.Id, options);
        saga.UniqueString = anotherUniqueString;
        await persister.Update(saga, options);
        await session.SaveChangesAsync();
        session.Dispose();

        using (session = store.OpenAsyncSession())
        {
            var value = (await session.Advanced.GetMetadataForAsync(saga1))[SagaPersister.UniqueValueMetadataKey].ToString();
            Assert.AreEqual(anotherUniqueString, value);
        }
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

        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        public string NonUniqueString { get; set; }
    }
}