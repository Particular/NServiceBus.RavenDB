using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Extensibility;
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
        var persister = new SagaPersister(new SagaPersistenceConfiguration(), UseClusterWideTransactions);
        var uniqueString = Guid.NewGuid().ToString();
        var sessionOptions = new SessionOptions
        {
            TransactionMode = UseClusterWideTransactions ? TransactionMode.ClusterWide : TransactionMode.SingleNode
        };

        using (var session = store.OpenAsyncSession(sessionOptions).UsingOptimisticConcurrency().InContext(out var options))
        {
            var saga1 = new SagaData
            {
                Id = Guid.NewGuid(),
                UniqueString = uniqueString
            };

            var synchronizedSession = new RavenDBSynchronizedStorageSession(session, new ContextBag());

            await persister.Save(saga1, this.CreateMetadata<SomeSaga>(saga1), synchronizedSession, options);
            await session.SaveChangesAsync().ConfigureAwait(false);
        }

        var exception = await Catch<ConcurrencyException>(async cancellationToken =>
        {
            using (var session = store.OpenAsyncSession(sessionOptions).UsingOptimisticConcurrency().InContext(out var options))
            {
                var saga2 = new SagaData
                {
                    Id = Guid.NewGuid(),
                    UniqueString = uniqueString
                };

                var synchronizedSession = new RavenDBSynchronizedStorageSession(session, new ContextBag());

                await persister.Save(saga2, this.CreateMetadata<SomeSaga>(saga2), synchronizedSession, options, cancellationToken);
                await session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
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
        public string UniqueString { get; set; }
        public Guid Id { get; set; }
        public string Originator { get; set; }
        public string OriginalMessageId { get; set; }
    }
}