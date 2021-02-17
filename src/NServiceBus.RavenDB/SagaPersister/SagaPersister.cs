namespace NServiceBus.Persistence.RavenDB
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.RavenDB.Persistence.SagaPersister;
    using NServiceBus.Sagas;
    using NServiceBus.Transport;
    using Raven.Client.Documents;
    using Raven.Client.Documents.Commands.Batches;
    using Raven.Client.Documents.Operations.CompareExchange;
    using Raven.Client.Documents.Session;

    class SagaPersister : ISagaPersister
    {
        public SagaPersister(SagaPersistenceConfiguration options, IOpenTenantAwareRavenSessions openTenantAwareRavenSessions)
        {
            leaseLockTime = options.LeaseLockTime;
            enablePessimisticLocking = options.EnablePessimisticLocking;
            acquireLeaseLockRefreshMaximumDelayTicks = (int)options.LeaseLockAcquisitionMaximumRefreshDelay.Ticks;
            acquireLeaseLockTimeout = options.LeaseLockAcquisitionTimeout;
            this.openTenantAwareRavenSessions = openTenantAwareRavenSessions;
        }

        public async Task Save(IContainSagaData sagaData, SagaCorrelationProperty correlationProperty, SynchronizedStorageSession session, ContextBag context)
        {
            var documentSession = session.RavenSession();
            var useClusterWideTx = ((InMemoryDocumentSessionOperations)documentSession).TransactionMode == TransactionMode.ClusterWide;

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
            documentSession.StoreSchemaVersionInMetadata(container);

            var sagaUniqueIdentity = new SagaUniqueIdentity
            {
                Id = container.IdentityDocId,
                SagaId = sagaData.Id,
                UniqueValue = correlationProperty.Value,
                SagaDocId = container.Id
            };
            documentSession.StoreSchemaVersionInMetadata(sagaUniqueIdentity);

            if (useClusterWideTx)
            {
                // CompareExchangeValue for both Id (find by Id) and IdentityDocId (find by correlation property value)
                documentSession.Advanced.ClusterTransaction.CreateCompareExchangeValue($"{SagaPersisterCompareExchangePrefix}/{container.Id}", container.Id);
                documentSession.Advanced.ClusterTransaction.CreateCompareExchangeValue($"{SagaPersisterCompareExchangePrefix}/{container.IdentityDocId}", container.Id);

                // We cannot pass any changeVector because using change vectors is incompatible with cluster-wide TXs
                await documentSession.StoreAsync(container, null, container.Id).ConfigureAwait(false);
                await documentSession.StoreAsync(sagaUniqueIdentity, null, container.IdentityDocId).ConfigureAwait(false);
            }
            else
            {
                // We cannot pass a null changeVector to signal that the document is new and we want optimistic concurrency on document creation
                await documentSession.StoreAsync(container, string.Empty, container.Id).ConfigureAwait(false);
                await documentSession.StoreAsync(sagaUniqueIdentity, string.Empty, id: container.IdentityDocId).ConfigureAwait(false);
            }
        }

        public Task Update(IContainSagaData sagaData, SynchronizedStorageSession session, ContextBag context)
        {
            var documentSession = session.RavenSession();
            var useClusterWideTx = ((InMemoryDocumentSessionOperations)documentSession).TransactionMode == TransactionMode.ClusterWide;
            var container = context.Get<SagaDataContainer>($"{SagaContainerContextKeyPrefix}{sagaData.Id}");

            if (useClusterWideTx)
            {
                var sagaIdCev = context.Get<CompareExchangeValue<string>>(SagaIdCompareExchange);
                var sagaUniqueDocIdCev = context.Get<CompareExchangeValue<string>>(SagaUniqueDocIdCompareExchange);
                // there's no possibility in the current client that one exchange value for a saga instance exists, and the other doesn't, they either both do or both don't
                if (sagaIdCev == null && sagaUniqueDocIdCev == null)
                {
                    // this is an upgrade scenario, a saga for which no exchange values have been created yet
                    documentSession.Advanced.ClusterTransaction.CreateCompareExchangeValue($"{SagaPersisterCompareExchangePrefix}/{container.Id}", container.Id);
                    documentSession.Advanced.ClusterTransaction.CreateCompareExchangeValue($"{SagaPersisterCompareExchangePrefix}/{container.IdentityDocId}", container.Id);
                }
                else
                {
                    documentSession.Advanced.ClusterTransaction.UpdateCompareExchangeValue(new CompareExchangeValue<string>(sagaIdCev.Key, sagaIdCev.Index, sagaIdCev.Value));
                    documentSession.Advanced.ClusterTransaction.UpdateCompareExchangeValue(new CompareExchangeValue<string>(sagaUniqueDocIdCev.Key, sagaUniqueDocIdCev.Index, sagaUniqueDocIdCev.Value));
                }
            }

            // store the schema version in case it has changed
            documentSession.StoreSchemaVersionInMetadata(container);

            // dirty tracking will do the rest for us
            return Task.CompletedTask;
        }

        public async Task<T> Get<T>(Guid sagaId, SynchronizedStorageSession session, ContextBag context)
            where T : class, IContainSagaData
        {
            var documentSession = session.RavenSession();
            var docId = DocumentIdForSagaData(documentSession, typeof(T), sagaId);
            var useClusterWideTx = ((InMemoryDocumentSessionOperations)documentSession).TransactionMode == TransactionMode.ClusterWide;

            if (enablePessimisticLocking)
            {
                var index = await AcquireLease(documentSession.Advanced.DocumentStore, docId).ConfigureAwait(false);
                // only true if we always have synchronized storage session around which is a valid assumption
                context.Get<SagaDataLeaseHolder>().DocumentsIdsAndIndexes.Add((docId, index));
            }

            var sagaWrapper = await documentSession.LoadAsync<SagaDataContainer>(docId).ConfigureAwait(false);
            if (sagaWrapper == null)
            {
                return default;
            }

            if (useClusterWideTx)
            {
                // if we can't find the compare exchange value, we're in an upgrade scenario

                var sagaIdCev = await documentSession.Advanced.ClusterTransaction.GetCompareExchangeValueAsync<string>($"{SagaPersisterCompareExchangePrefix}/{sagaWrapper.Id}").ConfigureAwait(false);
                var sagaUniqueDocIdCev = await documentSession.Advanced.ClusterTransaction.GetCompareExchangeValueAsync<string>($"{SagaPersisterCompareExchangePrefix}/{sagaWrapper.IdentityDocId}").ConfigureAwait(false);
                context.Set(SagaIdCompareExchange, sagaIdCev);
                context.Set(SagaUniqueDocIdCompareExchange, sagaUniqueDocIdCev);
            }

            context.Set($"{SagaContainerContextKeyPrefix}{sagaWrapper.Data.Id}", sagaWrapper);

            return sagaWrapper.Data as T;
        }

        public async Task<T> Get<T>(string propertyName, object propertyValue, SynchronizedStorageSession session, ContextBag context)
            where T : class, IContainSagaData
        {
            var documentSession = session.RavenSession();
            var lookupId = SagaUniqueIdentity.FormatId(typeof(T), propertyName, propertyValue);
            var useClusterWideTx = ((InMemoryDocumentSessionOperations)documentSession).TransactionMode == TransactionMode.ClusterWide;

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

            if (useClusterWideTx)
            {
                // if we can't find the compare exchange value, we're in an upgrade scenario.
                // We store them as null in the context bag and handle the case in Update/Delete
                var sagaIdCev = await documentSession.Advanced.ClusterTransaction.GetCompareExchangeValueAsync<string>($"{SagaPersisterCompareExchangePrefix}/{container.Id}").ConfigureAwait(false);
                var sagaUniqueDocIdCev = await documentSession.Advanced.ClusterTransaction.GetCompareExchangeValueAsync<string>($"{SagaPersisterCompareExchangePrefix}/{container.IdentityDocId}").ConfigureAwait(false);
                context.Set(SagaIdCompareExchange, sagaIdCev);
                context.Set(SagaUniqueDocIdCompareExchange, sagaUniqueDocIdCev);
            }

            context.Set($"{SagaContainerContextKeyPrefix}{container.Data.Id}", container);
            return (T)container.Data;
        }

        public async Task Complete(IContainSagaData sagaData, SynchronizedStorageSession session, ContextBag context)
        {
            var documentSession = session.RavenSession();
            var useClusterWideTx = ((InMemoryDocumentSessionOperations)documentSession).TransactionMode == TransactionMode.ClusterWide;

            var container = context.Get<SagaDataContainer>($"{SagaContainerContextKeyPrefix}{sagaData.Id}");
            documentSession.Delete(container);
            if (container.IdentityDocId != null)
            {
                documentSession.Advanced.Defer(new DeleteCommandData(container.IdentityDocId, null));
            }

            if (useClusterWideTx)
            {
                var sagaIdCev = context.Get<CompareExchangeValue<string>>(SagaIdCompareExchange);
                var sagaUniqueDocIdCev = context.Get<CompareExchangeValue<string>>(SagaUniqueDocIdCompareExchange);
                if (sagaIdCev == null || sagaUniqueDocIdCev == null)
                {
                    // this is an upgrade scenario, this is UGLY
                    // We have to create CEV out of band otherwise the delete
                    // cannot participate in a cluster wide transaction
                    var message = context.Get<IncomingMessage>();
                    using (var outOfBandSession = openTenantAwareRavenSessions.OpenSession(message.Headers))
                    {
                        outOfBandSession.Advanced.ClusterTransaction.CreateCompareExchangeValue($"{SagaPersisterCompareExchangePrefix}/{container.Id}", container.Id);
                        outOfBandSession.Advanced.ClusterTransaction.CreateCompareExchangeValue($"{SagaPersisterCompareExchangePrefix}/{container.IdentityDocId}", container.Id);

                        await outOfBandSession.SaveChangesAsync().ConfigureAwait(false);
                    }
                }

                documentSession.Advanced.ClusterTransaction.DeleteCompareExchangeValue(new CompareExchangeValue<string>(sagaIdCev.Key, sagaIdCev.Index, sagaIdCev.Value));
                documentSession.Advanced.ClusterTransaction.DeleteCompareExchangeValue(new CompareExchangeValue<string>(sagaUniqueDocIdCev.Key, sagaUniqueDocIdCev.Index, sagaUniqueDocIdCev.Value));
            }
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

                        var saveResult = await store.Operations.SendAsync(
                                new PutCompareExchangeValueOperation<SagaDataLease>(sagaDataDocId, lease, 0), token: token)
                            .ConfigureAwait(false);

                        if (saveResult.Successful)
                        {
                            // lease wasn't already present - we managed to acquire lease
                            return saveResult.Index;
                        }

                        // At this point, Put operation failed - someone else owns the lock or lock time expired
                        if (saveResult.Value.ReservedUntil < DateTime.UtcNow)
                        {
                            // Time expired - Update the existing key with the new value
                            var takeLockWithTimeoutResult = await store.Operations.SendAsync(
                                    new PutCompareExchangeValueOperation<SagaDataLease>(sagaDataDocId, lease, saveResult.Index), token: token)
                                .ConfigureAwait(false);

                            if (takeLockWithTimeoutResult.Successful)
                            {
                                return takeLockWithTimeoutResult.Index;
                            }
                        }

                        await Task.Delay(TimeSpan.FromTicks(5 + random.Next(acquireLeaseLockRefreshMaximumDelayTicks)), token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
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

        const string SagaContainerContextKeyPrefix = "SagaDataContainer:";
        static Random random = new Random();

        const string SagaIdCompareExchange = "NServiceBus.RavenDB.ClusterWideTx.SagaID";
        const string SagaUniqueDocIdCompareExchange = "NServiceBus.RavenDB.ClusterWideTx.SagaUniqueDocID";
        const string SagaPersisterCompareExchangePrefix = "sagas";
        IOpenTenantAwareRavenSessions openTenantAwareRavenSessions;
        TimeSpan leaseLockTime;
        bool enablePessimisticLocking;
        int acquireLeaseLockRefreshMaximumDelayTicks;
        TimeSpan acquireLeaseLockTimeout;
    }
}