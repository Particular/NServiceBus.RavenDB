namespace NServiceBus.Persistence.RavenDB
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Logging;
    using NServiceBus.RavenDB.Persistence.SagaPersister;
    using NServiceBus.Sagas;
    using Raven.Client.Documents;
    using Raven.Client.Documents.Commands.Batches;
    using Raven.Client.Documents.Operations.CompareExchange;
    using Raven.Client.Documents.Session;

    class SagaPersister : ISagaPersister
    {
        public SagaPersister(SagaPersistenceConfiguration options)
        {
            leaseLockTime = options.LeaseLockTime;
            enablePessimisticLocking = options.EnablePessimisticLocking;
            acquireLeaseLockRefreshMaximumDelayTicks = (int)options.LeaseLockAcquisitionMaximumRefreshDelay.Ticks;
            acquireLeaseLockTimeout = options.LeaseLockAcquisitionTimeout;
        }

        public async Task Save(IContainSagaData sagaData, SagaCorrelationProperty correlationProperty, SynchronizedStorageSession session, ContextBag context)
        {
            var documentSession = session.RavenSession();

            if (sagaData == null)
            {
                return;
            }

            if (correlationProperty == null)
            {
                return;
            }

            var container = new SagaDataContainer
            {
                Id = DocumentIdForSagaData(documentSession, sagaData),
                Data = sagaData,
                IdentityDocId = SagaUniqueIdentity.FormatId(sagaData.GetType(), correlationProperty.Name, correlationProperty.Value),
            };

            await documentSession.StoreAsync(container, string.Empty, container.Id).ConfigureAwait(false);
            documentSession.StoreSchemaVersionInMetadata(container);

            var sagaUniqueIdentity = new SagaUniqueIdentity
            {
                Id = container.IdentityDocId,
                SagaId = sagaData.Id,
                UniqueValue = correlationProperty.Value,
                SagaDocId = container.Id
            };

            await documentSession.StoreAsync(sagaUniqueIdentity, changeVector: string.Empty, id: container.IdentityDocId).ConfigureAwait(false);
            documentSession.StoreSchemaVersionInMetadata(sagaUniqueIdentity);
            logger.Warn("Save Complete");
        }

        public Task Update(IContainSagaData sagaData, SynchronizedStorageSession session, ContextBag context)
        {
            // store the schema version in case it has changed
            var container = context.Get<SagaDataContainer>($"{SagaContainerContextKeyPrefix}{sagaData.Id}");
            var documentSession = session.RavenSession();
            documentSession.StoreSchemaVersionInMetadata(container);

            logger.Warn("Update complete");
            // dirty tracking will do the rest for us
            return Task.CompletedTask;
        }

        public async Task<T> Get<T>(Guid sagaId, SynchronizedStorageSession session, ContextBag context)
            where T : class, IContainSagaData
        {
            var documentSession = session.RavenSession();
            var docId = DocumentIdForSagaData(documentSession, typeof(T), sagaId);

            if (enablePessimisticLocking)
            {
                var index = await AcquireLease(documentSession.Advanced.DocumentStore, docId).ConfigureAwait(false);
                // only true if we always have synchronized storage session around which is a valid assumption
                context.Get<SagaDataLeaseHolder>().DocumentsIdsAndIndexes.Add((docId, index));
            }

            var container = await documentSession.LoadAsync<SagaDataContainer>(docId).ConfigureAwait(false);

            if (container == null)
            {
                return default;
            }

            context.Set($"{SagaContainerContextKeyPrefix}{container.Data.Id}", container);

            return container.Data as T;
        }

        public async Task<T> Get<T>(string propertyName, object propertyValue, SynchronizedStorageSession session, ContextBag context)
            where T : class, IContainSagaData
        {
            var documentSession = session.RavenSession();

            var lookupId = SagaUniqueIdentity.FormatId(typeof(T), propertyName, propertyValue);

            SagaUniqueIdentity lookup;

            if (enablePessimisticLocking)
            {
                // Cannot include doc immediately, as this would be stale
                // if this is locked and need to acquire the lock
                // first and then document.
                lookup = await documentSession
                .LoadAsync<SagaUniqueIdentity>(lookupId)
                .ConfigureAwait(false);
            }
            else
            {
                lookup = await documentSession
                .Include(nameof(SagaUniqueIdentity.SagaDocId))
                .LoadAsync<SagaUniqueIdentity>(lookupId)
                .ConfigureAwait(false);
            }

            if (lookup == null)
            {
                return default;
            }

            documentSession.Advanced.Evict(lookup);

            if (enablePessimisticLocking)
            {
                var index = await AcquireLease(documentSession.Advanced.DocumentStore, lookup.SagaDocId).ConfigureAwait(false);
                // only true if we always have synchronized storage session around which is a valid assumption
                context.Get<SagaDataLeaseHolder>().DocumentsIdsAndIndexes.Add((lookup.SagaDocId, index));
            }

            // If we have a saga id we can just load it, should have been included in the round-trip already
            var container = await documentSession.LoadAsync<SagaDataContainer>(lookup.SagaDocId).ConfigureAwait(false);

            if (container == null)
            {
                return default;
            }

            if (container.IdentityDocId == null)
            {
                container.IdentityDocId = lookupId;
            }

            context.Set($"{SagaContainerContextKeyPrefix}{container.Data.Id}", container);
            return (T)container.Data;
        }

        public Task Complete(IContainSagaData sagaData, SynchronizedStorageSession session, ContextBag context)
        {
            var documentSession = session.RavenSession();
            var container = context.Get<SagaDataContainer>($"{SagaContainerContextKeyPrefix}{sagaData.Id}");
            documentSession.Delete(container);
            if (container.IdentityDocId != null)
            {
                documentSession.Advanced.Defer(new DeleteCommandData(container.IdentityDocId, null));
            }

            return Task.CompletedTask;
        }

        async Task<long> AcquireLease(IDocumentStore store, string sagaDataDocId)
        {
            using (var cancellationTokenSource = new CancellationTokenSource(acquireLeaseLockTimeout))
            {
                var token = cancellationTokenSource.Token;
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        var lease = new SagaDataLease(DateTime.UtcNow.Add(leaseLockTime));

                        logger.Warn($"Start acquiring lock {DateTime.Now} for {sagaDataDocId}");
                        var saveResult = await store.Operations.SendAsync(
                                new PutCompareExchangeValueOperation<SagaDataLease>(sagaDataDocId, lease, 0), token: token)
                            .ConfigureAwait(false);
                        logger.Warn($"Completed PutCompareExchangeValueOperation {DateTime.Now} for {sagaDataDocId}. Result: {saveResult.Successful}, Index: {saveResult.Index}");

                        if (saveResult.Successful)
                        {
                            // lease wasn't already present - we managed to acquire lease
                            return saveResult.Index;
                        }

                        // At this point, Put operation failed - someone else owns the lock or lock time expired
                        if (saveResult.Value.ReservedUntil < DateTime.UtcNow)
                        {
                            logger.Warn($"Trying to override existing lock for {sagaDataDocId}");
                            // Time expired - Update the existing key with the new value
                            var takeLockWithTimeoutResult = await store.Operations.SendAsync(
                                    new PutCompareExchangeValueOperation<SagaDataLease>(sagaDataDocId, lease, saveResult.Index), token: token)
                                .ConfigureAwait(false);

                            if (takeLockWithTimeoutResult.Successful)
                            {
                                logger.Warn($"Lock override successful {sagaDataDocId}");
                                return takeLockWithTimeoutResult.Index;
                            }
                            logger.Warn($"lock override failed {sagaDataDocId}");
                        }

                        logger.Warn($"Start Task.Delay {sagaDataDocId}");
                        await Task.Delay(TimeSpan.FromTicks(5 + random.Next(acquireLeaseLockRefreshMaximumDelayTicks)), token).ConfigureAwait(false);
                        logger.Warn($"Finished Task.Delay{sagaDataDocId}");
                    }
                    catch (OperationCanceledException e)
                    {
                        logger.Warn($"Caught OperationCanceledException {sagaDataDocId}", e);
                        break;
                    }
                }

                throw new TimeoutException($"Unable to acquire exclusive write lock for saga with id '{sagaDataDocId}' within allocated time '{acquireLeaseLockTimeout}'.");
            }
        }

        internal static string DocumentIdForSagaData(IAsyncDocumentSession documentSession, IContainSagaData sagaData)
        {
            return DocumentIdForSagaData(documentSession, sagaData.GetType(), sagaData.Id);
        }

        static string DocumentIdForSagaData(IAsyncDocumentSession documentSession, Type sagaDataType, Guid sagaId)
        {
            var conventions = documentSession.Advanced.DocumentStore.Conventions;
            var collectionName = conventions.FindCollectionName(sagaDataType);
            return $"{collectionName}{conventions.IdentityPartsSeparator}{sagaId}";
        }

        static ILog logger = LogManager.GetLogger("RavenLocking");

        const string SagaContainerContextKeyPrefix = "SagaDataContainer:";
        static Random random = new Random();

        TimeSpan leaseLockTime;
        bool enablePessimisticLocking;
        int acquireLeaseLockRefreshMaximumDelayTicks;
        TimeSpan acquireLeaseLockTimeout;
    }
}