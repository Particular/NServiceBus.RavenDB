namespace NServiceBus.Persistence.ComponentTests
{
    using System;
    using System.Threading.Tasks;
    using NUnit.Framework;

    [TestFixture]
    public class When_concurrent_update_exceed_transaction_timeout_pessimistic : SagaPersisterTests<TestSaga,TestSagaData>
    {
        public override async Task OneTimeSetUp()
        {
            configuration = new PersistenceTestsConfiguration(TimeSpan.FromMilliseconds(100));
            await configuration.Configure();
        }

        [Test]
        public async Task Should_fail_with_timeout()
        {
            configuration.RequiresPessimisticConcurrencySupport();

            var correlationPropertyData = Guid.NewGuid().ToString();
            var saga = new TestSagaData { SomeId = correlationPropertyData, DateTimeProperty = DateTime.UtcNow };
            await SaveSaga(saga);

            var firstSessionDateTimeValue = DateTime.UtcNow.AddDays(-2);
            var secondSessionDateTimeValue = DateTime.UtcNow.AddDays(-1);

            var firstSessionGetDone = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var secondSessionGetDone = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var persister = configuration.SagaStorage;

            async Task FirstSession()
            {
                var firstContent = configuration.GetContextBagForSagaStorage();
                var firstSaveSession = await configuration.SynchronizedStorage.OpenSession(firstContent);
                try
                {
                    SetActiveSagaInstanceForGet<TestSaga, TestSagaData>(firstContent, saga);
                    var record = await persister.Get<TestSagaData>(saga.Id, firstSaveSession, firstContent);
                    firstSessionGetDone.SetResult(true);

                    await Task.Delay(200).ConfigureAwait(false);

                    SetActiveSagaInstanceForGet<TestSaga, TestSagaData>(firstContent, record);
                    record.DateTimeProperty = firstSessionDateTimeValue;
                    await persister.Update(record, firstSaveSession, firstContent);
                    await secondSessionGetDone.Task.ConfigureAwait(false);
                    await firstSaveSession.CompleteAsync();
                }
                finally
                {
                    firstSaveSession.Dispose();
                }
            }

            async Task SecondSession()
            {
                var secondContext = configuration.GetContextBagForSagaStorage();
                var secondSession = await configuration.SynchronizedStorage.OpenSession(secondContext);
                try
                {
                    SetActiveSagaInstanceForGet<TestSaga, TestSagaData>(secondContext, saga);

                    await firstSessionGetDone.Task.ConfigureAwait(false);

                    var recordTask = persister.Get<TestSagaData>(saga.Id, secondSession, secondContext);
                    secondSessionGetDone.SetResult(true);
                    var record = await recordTask.ConfigureAwait(false);
                    SetActiveSagaInstanceForGet<TestSaga, TestSagaData>(secondContext, record);
                    record.DateTimeProperty = secondSessionDateTimeValue;
                    await persister.Update(record, secondSession, secondContext);
                    await secondSession.CompleteAsync();
                }
                finally
                {
                    secondSession.Dispose();
                }
            }

            Assert.ThrowsAsync<TimeoutException>(async () => await Task.WhenAll(SecondSession(), FirstSession()));
        }
    }
}