namespace NServiceBus.Persistence.RavenDB
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.RavenDB.Persistence.SagaPersister;
    using NServiceBus.Sagas;
    using Raven.Client.Documents.Commands.Batches;
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

            var container = new SagaDataContainer
            {
                Id = DocumentIdForSagaData(documentSession, sagaData),
                Data = sagaData
            };

            if (correlationProperty == null)
            {
                return;
            }

            container.IdentityDocId = SagaUniqueIdentity.FormatId(sagaData.GetType(), correlationProperty.Name, correlationProperty.Value);

            await documentSession.StoreAsync(container, string.Empty, container.Id).ConfigureAwait(false);
            await documentSession.StoreAsync(
                    new SagaUniqueIdentity
                    {
                        Id = container.IdentityDocId,
                        SagaId = sagaData.Id,
                        UniqueValue = correlationProperty.Value,
                        SagaDocId = container.Id
                    },
                    changeVector: string.Empty,
                    id: container.IdentityDocId)
                .ConfigureAwait(false);
        }

        public Task Update(IContainSagaData sagaData, SynchronizedStorageSession session, ContextBag context)
        {
            //no-op since the dirty tracking will handle the update for us
            return Task.CompletedTask;
        }

        public async Task<T> Get<T>(Guid sagaId, SynchronizedStorageSession session, ContextBag context)
            where T : class, IContainSagaData
        {
            var documentSession = session.RavenSession();
            var docId = DocumentIdForSagaData(documentSession, typeof(T), sagaId);
            var container = await documentSession.LoadAsync<SagaDataContainer>(docId).ConfigureAwait(false);

            if (container == null)
            {
                return default(T);
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
                .Include("SagaDocId") //tell raven to pull the saga doc as well to save us a round-trip
                .LoadAsync<SagaUniqueIdentity>(lookupId)
                .ConfigureAwait(false);

            if (lookup == null)
            {
                return default(T);
            }

            documentSession.Advanced.Evict(lookup);

            // If we have a saga id we can just load it, should have been included in the round-trip already
            var container = await documentSession.LoadAsync<SagaDataContainer>(lookup.SagaDocId).ConfigureAwait(false);

            if (container == null)
            {
                return default(T);
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

        static string DocumentIdForSagaData(IAsyncDocumentSession documentSession, IContainSagaData sagaData)
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
    }
}
