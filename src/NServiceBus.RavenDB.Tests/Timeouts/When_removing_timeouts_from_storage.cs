namespace NServiceBus.RavenDB.Tests.Timeouts
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Transactions;
    using NServiceBus.Extensibility;
    using NServiceBus.TimeoutPersisters.RavenDB;
    using NUnit.Framework;
    using Raven.Abstractions.Exceptions;
    using Raven.Client;
    using TimeoutData = NServiceBus.Timeout.Core.TimeoutData;

    public class When_removing_timeouts_from_storage : RavenDBPersistenceTestBase
    {
        public When_removing_timeouts_from_storage()
        {
            // in-memory store doesn't support dtc
            DocumentStoreFactory = t => t.NewDocumentStore(false, "esent");
        }

        [Test]
        public async Task Remove_WhenNoTimeoutRemoved_ShouldReturnFalse()
        {
            var persister = new TimeoutPersister(store);
            await persister.Add(new TimeoutData(), new ContextBag());

            var result = await persister.TryRemove(Guid.NewGuid().ToString(), new ContextBag());

            Assert.IsFalse(result);
        }

        [Test]
        public async Task Remove_WhenTimeoutRemoved_ShouldReturnTrue()
        {
            var persister = new TimeoutPersister(store);
            var timeoutData = new TimeoutData();
            await persister.Add(timeoutData, new ContextBag());

            var result = await persister.TryRemove(timeoutData.Id, new ContextBag());

            Assert.IsTrue(result);
        }

        [Test]
        public async Task Remove_WhenConcurrentDeletesUsingDtc_OnlyOneOperationShouldSucceed()
        {
            var persister = new TimeoutPersister(store);
            var timeoutData = new TimeoutData();
            await persister.Add(timeoutData, new ContextBag());

            var documentRemoved = new CountdownEvent(2);

            var t1 = Task.Run(async () =>
            {
                using (var tx = new TransactionScope(TransactionScopeOption.RequiresNew, TransactionScopeAsyncFlowOption.Enabled))
                {
                    var result = await persister.TryRemove(timeoutData.Id, new ContextBag());
                    documentRemoved.Signal(1);
                    documentRemoved.Wait();
                    tx.Complete();
                    return result;
                }
            });

            var t2 = Task.Run(async () =>
            {
                using (var tx = new TransactionScope(TransactionScopeOption.RequiresNew, TransactionScopeAsyncFlowOption.Enabled))
                {
                    var result = await persister.TryRemove(timeoutData.Id, new ContextBag());
                    documentRemoved.Signal(1);
                    documentRemoved.Wait();
                    tx.Complete();
                    return result;
                }
            });

            Assert.IsTrue(t1.Result | t2.Result, "the document should be deleted");
            Assert.IsFalse(t1.Result && t2.Result, "only one operation should complete successfully");
        }

        [Test]
        public void Raven_WhenConcurrentDeletesWithDtc_ShouldThrowConcurrencyException()
        {
            // Raven does not throw ConcurrencyExceptions when concurrently deleting documents without using DTC.
            // When using DTC we need to rely on Raven throwing this exception to avoid dispatching duplicate messages.
            // See issue http://issues.hibernatingrhinos.com/issue/RavenDB-4000

            var document = new DemoDocument();
            using (var session = store.OpenSession())
            {
                session.Store(document);
                session.SaveChanges();
            }

            var documentLoaded = new CountdownEvent(2);
            var documentDeleted = new CountdownEvent(2);

            var t1 = Task.Run(async () =>
            {
                using (var tx = new TransactionScope(TransactionScopeOption.RequiresNew, TransactionScopeAsyncFlowOption.Enabled))
                {
                    var result = await TryDeleteDocumentAsync(store, document.Id, documentLoaded, documentDeleted);
                    tx.Complete();
                    return result;
                }
            });

            var t2 = Task.Run(async () =>
            {
                using (var tx = new TransactionScope(TransactionScopeOption.RequiresNew, TransactionScopeAsyncFlowOption.Enabled))
                {
                    var result = await TryDeleteDocumentAsync(store, document.Id, documentLoaded, documentDeleted);
                    tx.Complete();
                    return result;
                }
            });

            Assert.IsTrue(t1.Result | t2.Result, "the document should be deleted");
            Assert.IsFalse(t1.Result && t2.Result, "only one operation should complete successfully");
        }

        static async Task<bool> TryDeleteDocumentAsync(IDocumentStore store, Guid documentId, CountdownEvent documentLoaded,
            CountdownEvent documentDeleted)
        {
            try
            {
                using (var session = store.OpenAsyncSession())
                {
                    var document = await session.LoadAsync<DemoDocument>(documentId);

                    documentLoaded.Signal(1);
                    documentLoaded.Wait();

                    session.Delete(document);

                    documentDeleted.Signal(1);
                    documentDeleted.Wait();

                    await session.SaveChangesAsync();
                    return true;
                }
            }
            catch (ConcurrencyException e)
            {
                Console.WriteLine(e);
                return false;
            }
        }

        class DemoDocument
        {
            public Guid Id { get; set; }
        }
    }
}