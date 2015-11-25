namespace NServiceBus.SagaPersisters.RavenDB
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.RavenDB.Persistence.SagaPersister;
    using NServiceBus.Sagas;
    using Raven.Abstractions.Commands;
    using Raven.Client;
    using Raven.Json.Linq;

    class SagaPersister : ISagaPersister
    {
        const string UniqueDocIdKey = "NServiceBus-UniqueDocId";

        public async Task Save(IContainSagaData sagaInstance, IDictionary<string, object> correlationProperties, ContextBag context)
        {
            var session = context.Get<IAsyncDocumentSession>();

            await session.StoreAsync(sagaInstance).ConfigureAwait(false);

            if (!correlationProperties.Any())
            {
                return;
            }

            var correlationProperty = correlationProperties.SingleOrDefault();

            var value = correlationProperty.Value;
            var id = SagaUniqueIdentity.FormatId(sagaInstance.GetType(), new KeyValuePair<string, object>(correlationProperty.Key, value));

            var sagaDocId = session.Advanced.DocumentStore.Conventions.FindFullDocumentKeyFromNonStringIdentifier(sagaInstance.Id, sagaInstance.GetType(), false);

            await session.StoreAsync(new SagaUniqueIdentity
            {
                Id = id,
                SagaId = sagaInstance.Id,
                UniqueValue = value,
                SagaDocId = sagaDocId
            }).ConfigureAwait(false);

            var metadata = await session.Advanced.GetMetadataForAsync(sagaInstance).ConfigureAwait(true);
            metadata[UniqueDocIdKey] = id;
        }

        public Task Update(IContainSagaData saga, ContextBag context)
        {
            //np-op since the dirty tracking will handle the update for us
            return Task.FromResult(0);
        }

        public async Task<T> Get<T>(Guid sagaId, ContextBag context) where T : IContainSagaData
        {
            var session = context.Get<IAsyncDocumentSession>();
            return await session.LoadAsync<T>(sagaId).ConfigureAwait(false);
        }

        public async Task<T> Get<T>(string property, object value, ContextBag context) where T : IContainSagaData
        {
            var session = context.Get<IAsyncDocumentSession>();

            var lookupId = SagaUniqueIdentity.FormatId(typeof(T), new KeyValuePair<string, object>(property, value));

            //store it in the context to be able to optimize deletes for legacy sagas that don't have the id in metadata
            context.Set(UniqueDocIdKey, lookupId);

            var lookup = await session
                .Include("SagaDocId") //tell raven to pull the saga doc as well to save us a round-trip
                .LoadAsync<SagaUniqueIdentity>(lookupId)
                .ConfigureAwait(false);

            if (lookup != null)
            {
                return lookup.SagaDocId != null
                    ? await session.LoadAsync<T>(lookup.SagaDocId).ConfigureAwait(false) //if we have a saga id we can just load it
                    : await Get<T>(lookup.SagaId, context); //if not this is a saga that was created pre 3.0.4 so we fallback to a get instead
            }

            return default(T);
        }

        public async Task Complete(IContainSagaData saga, ContextBag context)
        {
            var session = context.Get<IAsyncDocumentSession>();
            session.Delete(saga);

            string uniqueDocumentId;
            RavenJToken uniqueDocumentIdMetadata;
            var metadata = await session.Advanced.GetMetadataForAsync(saga).ConfigureAwait(false);
            if(metadata.TryGetValue(UniqueDocIdKey, out uniqueDocumentIdMetadata))
            {
                uniqueDocumentId = uniqueDocumentIdMetadata.Value<string>();
            }
            else
            {
                context.TryGet(UniqueDocIdKey, out uniqueDocumentId);
            }

            if (string.IsNullOrEmpty(uniqueDocumentId))
            {
                var uniqueDoc = await session.Query<SagaUniqueIdentity>()
                    .SingleOrDefaultAsync(d => d.SagaId == saga.Id)
                    .ConfigureAwait(false);

                if (uniqueDoc != null)
                {
                    session.Delete(uniqueDoc);
                }
            }

            session.Advanced.Defer(new DeleteCommandData
            {
                Key = uniqueDocumentId
            });
        }
    }
}