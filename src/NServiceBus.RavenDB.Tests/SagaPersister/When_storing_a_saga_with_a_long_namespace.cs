using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.RavenDB.Tests;
using NServiceBus.SagaPersisters.RavenDB;
using NUnit.Framework;
using Raven.Client;

[TestFixture]
public class When_storing_a_saga_with_a_long_namespace : RavenDBPersistenceTestBase
{
    [Test]
    public async Task Should_not_generate_a_to_long_unique_property_id()
    {
        IAsyncDocumentSession session;
        var options = this.CreateContextWithAsyncSessionPresent(out session);
        var persister = new SagaPersister();
        var uniqueString = Guid.NewGuid().ToString();
        var saga = new SagaWithUniquePropertyAndALongNamespace
            {
                Id = Guid.NewGuid(),
                UniqueString = uniqueString
            };
        await persister.Save(saga, this.CreateMetadata<SomeSaga>(saga), options);
        await session.SaveChangesAsync().ConfigureAwait(false);
    }

    class SomeSaga : Saga<SagaWithUniquePropertyAndALongNamespace>
    {
        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaWithUniquePropertyAndALongNamespace> mapper)
        {
        }
    }

    class SagaWithUniquePropertyAndALongNamespace : IContainSagaData
    {
        public Guid Id { get; set; }
        public string Originator { get; set; }
        public string OriginalMessageId { get; set; }

        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        public string UniqueString { get; set; }
    }
}