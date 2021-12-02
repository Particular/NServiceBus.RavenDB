namespace NServiceBus.RavenDB.Tests.Timeouts
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Persistence.RavenDB;
    using NUnit.Framework;
    using TimeoutData = Timeout.Core.TimeoutData;

    public class When_removing_timeouts_from_storage : RavenDBPersistenceTestBase
    {
        [Test]
        public async Task Remove_WhenNoTimeoutRemoved_ShouldReturnFalse()
        {
            var persister = new TimeoutPersister(store, UseClusterWideTransactions);
            await persister.Add(new TimeoutData(), new ContextBag());

            var result = await persister.TryRemove(Guid.NewGuid().ToString(), new ContextBag());

            Assert.IsFalse(result);
        }

        [Test]
        public async Task Remove_WhenTimeoutRemoved_ShouldReturnTrue()
        {
            var persister = new TimeoutPersister(store, UseClusterWideTransactions);
            var timeoutData = new TimeoutData();
            await persister.Add(timeoutData, new ContextBag());

            var result = await persister.TryRemove(timeoutData.Id, new ContextBag());

            Assert.IsTrue(result);
        }
    }
}