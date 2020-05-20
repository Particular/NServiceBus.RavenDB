namespace NServiceBus.Persistence.RavenDB
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.RavenDB.Persistence.SagaPersister;
    using NServiceBus.Sagas;
    using Raven.Client.Documents;
    using Raven.Client.Documents.Commands.Batches;
    using Raven.Client.Documents.Operations.CompareExchange;
    using Raven.Client.Documents.Session;

    class SagaPersister : ISagaPersister
    {
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
        }

        public Task Update(IContainSagaData sagaData, SynchronizedStorageSession session, ContextBag context)
        {
            // store the schema version in case it has changed
            var container = context.Get<SagaDataContainer>($"{SagaContainerContextKeyPrefix}{sagaData.Id}");
            var documentSession = session.RavenSession();
            documentSession.StoreSchemaVersionInMetadata(container);

            // dirty tracking will do the rest for us
            return Task.CompletedTask;
        }

        public async Task<T> Get<T>(Guid sagaId, SynchronizedStorageSession session, ContextBag context)
            where T : class, IContainSagaData
        {
            var documentSession = session.RavenSession();
            var docId = DocumentIdForSagaData(documentSession, typeof(T), sagaId);

            // TODO: currently always pessimistic
            var index = await AcquireLease(documentSession.Advanced.DocumentStore, docId).ConfigureAwait(false);
            // only true if we always have synchronized storage session around which is a valid assumption
            context.Get<SagaDataLeaseHolder>().NamesAndIndex.Add(Tuple.Create(docId, index));

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

            var lookup = await documentSession
                // when doing pessimistic locking we can include the saga data
                // TODO: This code needs to be changed so that we can opt in to loading documents
                //.Include("SagaDocId") //tell raven to pull the saga doc as well to save us a round-trip
                .LoadAsync<SagaUniqueIdentity>(lookupId)
                .ConfigureAwait(false);

            if (lookup == null)
            {
                return default;
            }

            documentSession.Advanced.Evict(lookup);

            // TODO: currently always pessimistic
            var index = await AcquireLease(documentSession.Advanced.DocumentStore, lookup.SagaDocId).ConfigureAwait(false);
            // only true if we always have synchronized storage session around which is a valid assumption
            context.Get<SagaDataLeaseHolder>().NamesAndIndex.Add(Tuple.Create(lookup.SagaDocId, index));

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

        async Task<long> AcquireLease(IDocumentStore store, string sagaDataId)
        {
            // TODO: configurable
            var transactionTimeout = TimeSpan.FromSeconds(60);
            var leaseLockTime = TimeSpan.FromSeconds(60);
            using (var cancellationTokenSource = new CancellationTokenSource(transactionTimeout))
            {
                var token = cancellationTokenSource.Token;
                while (!token.IsCancellationRequested)
                {
                    var resource = new SagaDataLease { ReservedUntil = DateTime.UtcNow.Add(leaseLockTime), LeaseId = sagaDataId};

                    // TODO: check cancellation logic and exception bubbling
                    var saveResult = await store.Operations.SendAsync(
                        new PutCompareExchangeValueOperation<SagaDataLease>(sagaDataId, resource, 0), token: CancellationToken.None)
                        .ConfigureAwait(false);

                    if (saveResult.Successful)
                    {
                        // resourceName wasn't present - we managed to reserve
                        return saveResult.Index;
                    }

                    // At this point, Put operation failed - someone else owns the lock or lock time expired
                    if (saveResult.Value.ReservedUntil < DateTime.UtcNow)
                    {
                        // Time expired - Update the existing key with the new value
                        // TODO: check cancellation logic and exception bubbling
                        var takeLockWithTimeoutResult = await store.Operations.SendAsync(
                            new PutCompareExchangeValueOperation<SagaDataLease>(sagaDataId, resource, saveResult.Index), token: CancellationToken.None)
                            .ConfigureAwait(false);

                        if (takeLockWithTimeoutResult.Successful)
                        {
                            return takeLockWithTimeoutResult.Index;
                        }
                    }

                    // TODO: This logic here has some flaws that need to be ironed out
                    try
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(random.Next(5, 20)), token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }

                throw new TimeoutException($"Unable to acquire exclusive write lock for saga with id '{sagaDataId}'.");
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
        const string SagaLeaseKeyPrefix = "SagaDataContainerLease:";
        static Random random = new Random();
    }
}