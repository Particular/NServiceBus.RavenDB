namespace NServiceBus.SagaPersisters.RavenDB
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Persistence;
    using NServiceBus.RavenDB.Persistence.SagaPersister;
    using NServiceBus.Sagas;
    using Raven.Abstractions.Commands;
    using Raven.Abstractions.Data;
    using Raven.Client;
    using Raven.Json.Linq;

    class SagaPersister : ISagaPersister
    {
        const string UniqueDocIdKey = "NServiceBus-UniqueDocId";

        public async Task Save(IContainSagaData sagaData, SagaCorrelationProperty correlationProperty, SynchronizedStorageSession session, ContextBag context)
        {
            var documentSession = session.RavenSession();

            if (sagaData == null)
            {
                return;
            }

            await documentSession.StoreAsync(sagaData).ConfigureAwait(false);

            if (correlationProperty == null)
            {
                return;
            }

            await CreateSagaUniqueIdentity(sagaData, correlationProperty, documentSession).ConfigureAwait(false);
        }

        static async Task CreateSagaUniqueIdentity(IContainSagaData sagaData, SagaCorrelationProperty correlationProperty, IAsyncDocumentSession documentSession)
        {
            var sagaDocId = documentSession.Advanced.DocumentStore.Conventions.FindFullDocumentKeyFromNonStringIdentifier(sagaData.Id, sagaData.GetType(), false);
            var propertyKeyValuePair = new KeyValuePair<string, object>(correlationProperty.Name, correlationProperty.Value);
            var sagaUniqueIdentityDocId = SagaUniqueIdentity.FormatId(sagaData.GetType(), propertyKeyValuePair);

            await documentSession.StoreAsync(new SagaUniqueIdentity
            {
                Id = sagaUniqueIdentityDocId,
                SagaId = sagaData.Id,
                UniqueValue = propertyKeyValuePair.Value,
                SagaDocId = sagaDocId
            }, id: sagaUniqueIdentityDocId, etag: Etag.Empty).ConfigureAwait(false);

            var metadata = await documentSession.Advanced.GetMetadataForAsync(sagaData).ConfigureAwait(false);
            metadata[UniqueDocIdKey] = sagaUniqueIdentityDocId;
        }

        public Task Update(IContainSagaData sagaData, SynchronizedStorageSession session, ContextBag context)
        {
            //no-op since the dirty tracking will handle the update for us
            return TaskEx.CompletedTask;
        }

        public async Task<T> Get<T>(Guid sagaId, SynchronizedStorageSession session, ContextBag context) where T : IContainSagaData
        {
            var documentSession = session.RavenSession();
            return await documentSession.LoadAsync<T>(sagaId).ConfigureAwait(false);
        }

        public async Task<T> Get<T>(string propertyName, object propertyValue, SynchronizedStorageSession session, ContextBag context) where T : IContainSagaData
        {
            var documentSession = session.RavenSession();

            var lookupId = SagaUniqueIdentity.FormatId(typeof(T), new KeyValuePair<string, object>(propertyName, propertyValue));

            //store it in the context to be able to optimize deletes for legacy sagas that don't have the id in metadata
            context.Set(UniqueDocIdKey, lookupId);

            var lookup = await documentSession
                .Include("SagaDocId") //tell raven to pull the saga doc as well to save us a round-trip
                .LoadAsync<SagaUniqueIdentity>(lookupId)
                .ConfigureAwait(false);

            if (lookup != null)
            {
                documentSession.Advanced.Evict(lookup);

                return lookup.SagaDocId != null
                    ? await documentSession.LoadAsync<T>(lookup.SagaDocId).ConfigureAwait(false) //if we have a saga id we can just load it
                    : await Get<T>(lookup.SagaId, session, context).ConfigureAwait(false); //if not this is a saga that was created pre 3.0.4 so we fallback to a get instead
            }

            return default(T);
        }

        public async Task Complete(IContainSagaData sagaData, SynchronizedStorageSession session, ContextBag context)
        {
            var documentSession = session.RavenSession();

            documentSession.Delete(sagaData);

            string uniqueDocumentId;
            RavenJToken uniqueDocumentIdMetadata;
            var metadata = await documentSession.Advanced.GetMetadataForAsync(sagaData).ConfigureAwait(false);
            if (metadata.TryGetValue(UniqueDocIdKey, out uniqueDocumentIdMetadata))
            {
                uniqueDocumentId = uniqueDocumentIdMetadata.Value<string>();
            }
            else
            {
                context.TryGet(UniqueDocIdKey, out uniqueDocumentId);
            }

            if (string.IsNullOrEmpty(uniqueDocumentId))
            {
                var uniqueDoc = await documentSession.Query<SagaUniqueIdentity>()
                    .SingleOrDefaultAsync(d => d.SagaId == sagaData.Id)
                    .ConfigureAwait(false);

                if (uniqueDoc != null)
                {
                    documentSession.Delete(uniqueDoc);
                }
            }
            else
            {
                documentSession.Advanced.Defer(new DeleteCommandData
                {
                    Key = uniqueDocumentId
                });
            }
        }
    }
}