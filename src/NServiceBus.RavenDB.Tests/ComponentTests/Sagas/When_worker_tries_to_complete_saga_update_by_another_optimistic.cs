// ReSharper disable AccessToDisposedClosure

namespace NServiceBus.Persistence.ComponentTests
{
    using System;
    using System.Threading.Tasks;
    using Extensibility;
    using NUnit.Framework;

    [TestFixture]
    public class When_worker_tries_to_complete_saga_update_by_another_optimistic : SagaPersisterTests<TestSaga, TestSagaData>
    {
        [Test]
        public async Task Should_fail()
        {
            configuration.RequiresOptimisticConcurrencySupport();

            var correlationPropertyData = Guid.NewGuid().ToString();
            var saga = new TestSagaData {SomeId = correlationPropertyData, DateTimeProperty = DateTime.UtcNow};

            await SaveSaga(saga);

            var persister = configuration.SagaStorage;

            ContextBag losingContext;
            CompletableSynchronizedStorageSession losingSaveSession;
            TestSagaData staleRecord;

            var winningContext = configuration.GetContextBagForSagaStorage();
            var winningSaveSession = await configuration.SynchronizedStorage.OpenSession(winningContext);
            try
            {
                SetActiveSagaInstanceForGet<TestSaga, TestSagaData>(winningContext, saga);
                var record = await persister.Get<TestSagaData>(saga.Id, winningSaveSession, winningContext);
                SetActiveSagaInstanceForGet<TestSaga, TestSagaData>(winningContext, record);

                losingContext = configuration.GetContextBagForSagaStorage();
                losingSaveSession = await configuration.SynchronizedStorage.OpenSession(losingContext);
                SetActiveSagaInstanceForGet<TestSaga, TestSagaData>(losingContext, saga);
                staleRecord = await persister.Get<TestSagaData>("SomeId", correlationPropertyData, losingSaveSession, losingContext);
                SetActiveSagaInstanceForGet<TestSaga, TestSagaData>(losingContext, staleRecord);

                record.DateTimeProperty = DateTime.UtcNow;
                await persister.Update(record, winningSaveSession, winningContext);
                await winningSaveSession.CompleteAsync();
            }
            finally
            {
                winningSaveSession.Dispose();
            }

            try
            {
                Assert.That(async () =>
                {
                    await persister.Complete(staleRecord, losingSaveSession, losingContext);
                    await losingSaveSession.CompleteAsync();
                }, Throws.InstanceOf<Exception>());
            }
            finally
            {
                losingSaveSession.Dispose();
            }
        }
    }
}