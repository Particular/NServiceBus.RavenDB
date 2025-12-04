namespace NServiceBus.Persistence.RavenDB;

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
    public SagaPersister(SagaPersistenceConfiguration options, bool useClusterWideTransactions)
    {
        this.useClusterWideTransactions = useClusterWideTransactions;
        leaseLockTime = options.LeaseLockTime;
        enablePessimisticLocking = options.EnablePessimisticLocking;
        acquireLeaseLockRefreshMaximumDelayMilliseconds = Convert.ToInt32(options.LeaseLockAcquisitionMaximumRefreshDelay.TotalMilliseconds);
        acquireLeaseLockRefreshMinimumDelayMilliseconds = 5;
        acquireLeaseLockTimeout = options.LeaseLockAcquisitionTimeout;
    }

    public async Task Save(IContainSagaData sagaData, SagaCorrelationProperty correlationProperty, ISynchronizedStorageSession session, ContextBag context, CancellationToken cancellationToken = default)
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

        // Optimistic concurrency can be turned on for a new document by passing string.Empty as a change vector value to Store
        // method even when it is turned off for an entire session (or globally).
        // It will cause to throw ConcurrencyException if the document already exists.
        string changeVector = useClusterWideTransactions ? null : string.Empty;
        await documentSession.StoreAsync(container, changeVector, container.Id, cancellationToken).ConfigureAwait(false);
        documentSession.StoreSchemaVersionInMetadata(container);

        var sagaUniqueIdentity = new SagaUniqueIdentity
        {
            Id = container.IdentityDocId,
            SagaId = sagaData.Id,
            UniqueValue = correlationProperty.Value,
            SagaDocId = container.Id
        };

        await documentSession.StoreAsync(sagaUniqueIdentity, changeVector: changeVector, id: container.IdentityDocId, token: cancellationToken).ConfigureAwait(false);
        documentSession.StoreSchemaVersionInMetadata(sagaUniqueIdentity);
    }

    public Task Update(IContainSagaData sagaData, ISynchronizedStorageSession session, ContextBag context, CancellationToken cancellationToken = default)
    {
        // store the schema version in case it has changed
        var container = context.Get<SagaDataContainer>($"{SagaContainerContextKeyPrefix}{sagaData.Id}");
        var documentSession = session.RavenSession();
        documentSession.StoreSchemaVersionInMetadata(container);

        // dirty tracking will do the rest for us
        return Task.CompletedTask;
    }

    public async Task<T> Get<T>(Guid sagaId, ISynchronizedStorageSession session, ContextBag context, CancellationToken cancellationToken = default)
        where T : class, IContainSagaData
    {
        var documentSession = session.RavenSession();
        var docId = DocumentIdForSagaData(documentSession, typeof(T), sagaId);

        if (enablePessimisticLocking)
        {
            var index = await AcquireLease(documentSession.Advanced.DocumentStore, docId, cancellationToken).ConfigureAwait(false);
            // only true if we always have synchronized storage session around which is a valid assumption
            context.Get<SagaDataLeaseHolder>().DocumentsIdsAndIndexes.Add((docId, index));
        }

        var container = await documentSession.LoadAsync<SagaDataContainer>(docId, cancellationToken).ConfigureAwait(false);

        if (container == null)
        {
            return default;
        }

        context.Set($"{SagaContainerContextKeyPrefix}{container.Data.Id}", container);

        return container.Data as T;
    }

    public async Task<T> Get<T>(string propertyName, object propertyValue, ISynchronizedStorageSession session, ContextBag context, CancellationToken cancellationToken = default)
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
                .LoadAsync<SagaUniqueIdentity>(lookupId, cancellationToken)
                .ConfigureAwait(false);
        }
        else
        {
            lookup = await documentSession
                .Include(nameof(SagaUniqueIdentity.SagaDocId))
                .LoadAsync<SagaUniqueIdentity>(lookupId, cancellationToken)
                .ConfigureAwait(false);
        }

        if (lookup == null)
        {
            return default;
        }

        documentSession.Advanced.Evict(lookup);

        if (enablePessimisticLocking)
        {
            var index = await AcquireLease(documentSession.Advanced.DocumentStore, lookup.SagaDocId, cancellationToken).ConfigureAwait(false);
            // only true if we always have synchronized storage session around which is a valid assumption
            context.Get<SagaDataLeaseHolder>().DocumentsIdsAndIndexes.Add((lookup.SagaDocId, index));
        }

        // If we have a saga id we can just load it, should have been included in the round-trip already
        var container = await documentSession.LoadAsync<SagaDataContainer>(lookup.SagaDocId, cancellationToken).ConfigureAwait(false);

        if (container == null)
        {
            return default;
        }

        container.IdentityDocId ??= lookupId;

        context.Set($"{SagaContainerContextKeyPrefix}{container.Data.Id}", container);
        return (T)container.Data;
    }

    public Task Complete(IContainSagaData sagaData, ISynchronizedStorageSession session, ContextBag context, CancellationToken cancellationToken = default)
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

    async Task<long> AcquireLease(IDocumentStore store, string sagaDataDocId, CancellationToken cancellationToken)
    {
        using (var timedTokenSource = new CancellationTokenSource(acquireLeaseLockTimeout))
        using (var combinedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(timedTokenSource.Token, cancellationToken))
        {
            var token = combinedTokenSource.Token;

            while (!token.IsCancellationRequested)
            {
                try
                {
                    token.ThrowIfCancellationRequested();

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

                    await Task.Delay(TimeSpan.FromMilliseconds(random.Next(acquireLeaseLockRefreshMinimumDelayMilliseconds, acquireLeaseLockRefreshMaximumDelayMilliseconds)), token).ConfigureAwait(false);
                }
#pragma warning disable PS0019 // When catching System.Exception, cancellation needs to be properly accounted for - justification:
                // Cancellation is properly accounted for. In this case, we only want to catch cancellation by one of the tokens used to create the combined token.
                catch (Exception ex) when (ex.IsCausedBy(timedTokenSource.Token))
#pragma warning restore PS0019 // When catching System.Exception, cancellation needs to be properly accounted for
                {
                    // Timed token source triggering breaks and results in TimeoutException
                    // Passed in cancellationToken triggering will throw out of this method to be handled by caller
                    // log the exception in case the stack trace is ever needed for debugging
                    log.Debug("Operation canceled when time out exhausted for acquiring exclusive write lock.", ex);
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

    static readonly ILog log = LogManager.GetLogger<SagaPersister>();
    static readonly Random random = new Random();

    readonly bool enablePessimisticLocking;
    readonly int acquireLeaseLockRefreshMaximumDelayMilliseconds;
    readonly int acquireLeaseLockRefreshMinimumDelayMilliseconds;
    readonly bool useClusterWideTransactions;

    TimeSpan leaseLockTime;
    TimeSpan acquireLeaseLockTimeout;
}