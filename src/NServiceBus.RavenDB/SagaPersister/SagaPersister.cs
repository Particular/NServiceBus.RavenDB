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

        public Task Save(IContainSagaData sagaInstance, IDictionary<string, object> correlationProperties, ContextBag context)
        {
            var session = context.Get<IDocumentSession>();

            session.Store(sagaInstance);

            if (!correlationProperties.Any())
            {
                return Task.FromResult(0);
            }

            var correlationProperty = correlationProperties.SingleOrDefault();


            var value = correlationProperty.Value;
            var id = SagaUniqueIdentity.FormatId(sagaInstance.GetType(), new KeyValuePair<string, object>(correlationProperty.Key, value));


            var sagaDocId = session.Advanced.DocumentStore.Conventions.FindFullDocumentKeyFromNonStringIdentifier(sagaInstance.Id, sagaInstance.GetType(), false);

            session.Store(new SagaUniqueIdentity
            {
                Id = id,
                SagaId = sagaInstance.Id,
                UniqueValue = value,
                SagaDocId = sagaDocId
            });

            session.Advanced.GetMetadataFor(sagaInstance)[UniqueDocIdKey] = id;


            return Task.FromResult(0);
        }

        public Task Update(IContainSagaData saga, ContextBag context)
        {
            //np-op since the dirty tracking will handle the update for us
            return Task.FromResult(0);
        }

        public Task<T> Get<T>(Guid sagaId, ContextBag context) where T : IContainSagaData
        {
            var session = context.Get<IDocumentSession>();
            return Task.FromResult(session.Load<T>(sagaId));
        }

        public Task<T> Get<T>(string property, object value, ContextBag context) where T : IContainSagaData
        {
            var session = context.Get<IDocumentSession>();

            var lookupId = SagaUniqueIdentity.FormatId(typeof(T), new KeyValuePair<string, object>(property, value));

            //store it in the context to be able to optimize deletes for legacy sagas that don't have the id in metadata
            context.Set(UniqueDocIdKey, lookupId);

            var lookup = session
                .Include("SagaDocId") //tell raven to pull the saga doc as well to save us a round-trip
                .Load<SagaUniqueIdentity>(lookupId);

            if (lookup != null)
            {
                return lookup.SagaDocId != null
                    ? Task.FromResult(session.Load<T>(lookup.SagaDocId)) //if we have a saga id we can just load it
                    : Get<T>(lookup.SagaId, context); //if not this is a saga that was created pre 3.0.4 so we fallback to a get instead
            }

            return Task.FromResult(default(T));
        }

        public Task Complete(IContainSagaData saga, ContextBag context)
        {
            var session = context.Get<IDocumentSession>();
            session.Delete(saga);

            string uniqueDocumentId;
            RavenJToken uniqueDocumentIdMetadata;

            if (session.Advanced.GetMetadataFor(saga).TryGetValue(UniqueDocIdKey, out uniqueDocumentIdMetadata))
            {
                uniqueDocumentId = uniqueDocumentIdMetadata.Value<string>();
            }
            else
            {
                context.TryGet(UniqueDocIdKey, out uniqueDocumentId);
            }

            if (string.IsNullOrEmpty(uniqueDocumentId))
            {
                var uniqueDoc = session.Query<SagaUniqueIdentity>()
                    .SingleOrDefault(d => d.SagaId == saga.Id);

                if (uniqueDoc != null)
                {
                    session.Delete(uniqueDoc);
                }

                return Task.FromResult(0);
            }

            session.Advanced.Defer(new DeleteCommandData
            {
                Key = uniqueDocumentId
            });

            return Task.FromResult(0);
        }
    }
}