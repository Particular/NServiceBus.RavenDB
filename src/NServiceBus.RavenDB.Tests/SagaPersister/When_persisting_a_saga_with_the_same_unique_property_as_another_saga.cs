using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Persistence.RavenDB;
using NServiceBus.RavenDB.Tests;
using NUnit.Framework;
using Raven.Client.Documents.Session;
using Raven.Client.Exceptions;

[TestFixture]
public class When_persisting_a_saga_with_the_same_unique_property_as_another_saga : RavenDBPersistenceTestBase
{
    [Test]
    public async Task It_should_enforce_uniqueness()
    {
        IAsyncDocumentSession session;
        var options = this.CreateContextWithAsyncSessionPresent(out session);
        var persister = new SagaPersister();
        var uniqueString = Guid.NewGuid().ToString();

        var saga1 = new SagaData
        {
            Id = Guid.NewGuid(),
            UniqueString = uniqueString
        };

        var synchronizedSession = new RavenDBSynchronizedStorageSession(session);

        await persister.Save(saga1, this.CreateMetadata<SomeSaga>(saga1), synchronizedSession, options);
        await session.SaveChangesAsync().ConfigureAwait(false);
        session.Dispose();

        var exception = await Catch<ConcurrencyException>(async () =>
        {
            options = this.CreateContextWithAsyncSessionPresent(out session);
            synchronizedSession = new RavenDBSynchronizedStorageSession(session);
            var saga2 = new SagaData
            {
                Id = Guid.NewGuid(),
                UniqueString = uniqueString
            };
            await persister.Save(saga2, this.CreateMetadata<SomeSaga>(saga2), synchronizedSession, options);
            await session.SaveChangesAsync().ConfigureAwait(false);
        });

        Assert.IsNotNull(exception);
    }

    class SomeSaga : Saga<SagaData>, IAmStartedByMessages<StartSaga>
    {
        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
        {
            mapper.ConfigureMapping<StartSaga>(m => m.UniqueString).ToSaga(s => s.UniqueString);
        }

        public Task Handle(StartSaga message, IMessageHandlerContext context)
        {
            return Task.CompletedTask;
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