namespace NServiceBus.SagaPersisters.RavenDB
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using NServiceBus.RavenDB.Persistence.SagaPersister;
    using NServiceBus.Sagas;
    using Raven.Abstractions.Commands;
    using Raven.Client;

    class SagaPersister : ISagaPersister
    {
        internal const string UniqueValueMetadataKey = "NServiceBus-UniqueValue";
        static readonly ConcurrentDictionary<string, bool> PropertyCache = new ConcurrentDictionary<string, bool>();

        public bool AllowUnsafeLoads { get; set; }

        public async Task Save(IContainSagaData saga, SagaPersistenceOptions options)
        {
            var session = options.GetSession();
            await session.StoreAsync(saga).ConfigureAwait(false);
            await StoreUniqueProperty(saga, session, options.Metadata).ConfigureAwait(false);
        }

        public async Task Update(IContainSagaData saga, SagaPersistenceOptions options)
        {
            // TODO: Check assumption
            var sagaMetadata = options.Metadata;
            var correlationProperty = sagaMetadata.CorrelationProperties.SingleOrDefault();

            if (correlationProperty == null)
            {
                return;
            }

            var session = options.GetSession();
            var uniqueProperty = GetUniqueProperty(sagaMetadata, correlationProperty);

            var ravenMetadata = session.Advanced.GetMetadataFor(saga);

            //if the user just added the unique property to a saga with existing data we need to set it
            if (!ravenMetadata.ContainsKey(UniqueValueMetadataKey))
            {
                await StoreUniqueProperty(saga, session, sagaMetadata).ConfigureAwait(false);
                return;
            }

            var storedValue = ravenMetadata[UniqueValueMetadataKey].ToString();

            var currentValue = uniqueProperty.GetValue(saga).ToString();

            if (currentValue == storedValue)
            {
                return;
            }

            DeleteUniqueProperty(saga, session, new KeyValuePair<string, object>(uniqueProperty.Name, storedValue));
            await StoreUniqueProperty(saga, session, sagaMetadata).ConfigureAwait(false);
        }

        public Task<T> Get<T>(Guid sagaId, SagaPersistenceOptions options) where T : IContainSagaData
        {
            var session = options.GetSession();
            return session.LoadAsync<T>(sagaId);
        }

        public async Task<T> Get<T>(string property, object value, SagaPersistenceOptions options) where T : IContainSagaData
        {
            var session = options.GetSession();
            if (IsUniqueProperty<T>(options.Metadata, property))
            {
                return await GetByUniqueProperty<T>(property, value, session, options);
            }

            if (!AllowUnsafeLoads)
            {
                var message = $"Correlating on saga properties not marked as unique is not safe due to the high risk for stale results. Please add a [Unique] attribute to the '{property}' property on your '{typeof(T).Name}' saga data class. If you still want to allow this please add .UsePersistence<RavenDBPersistence>().AllowStaleSagaReads() to your config";
                throw new Exception(message);
            }

            var sagaData = await session.Advanced.AsyncDocumentQuery<T>()
                .WhereEquals(property, value)
                .ToListAsync();

            return sagaData.FirstOrDefault();
        }

        public Task Complete(IContainSagaData saga, SagaPersistenceOptions options)
        {
            var session = options.GetSession();
            session.Delete(saga);

            // TODO: Check assumption
            var correlationProperty = options.Metadata.CorrelationProperties.SingleOrDefault();

            if (correlationProperty == null)
            {
                return Task.FromResult(0);
            }

            var uniqueProperty = GetUniqueProperty(options.Metadata, correlationProperty);
            DeleteUniqueProperty(saga, session, new KeyValuePair<string, object>(uniqueProperty.Name, uniqueProperty.GetValue(saga)));
            return Task.FromResult(0);
        }

        static bool IsUniqueProperty<T>(SagaMetadata metadata, string property)
        {
            var key = typeof(T).FullName + property;
            bool value;

            if (!PropertyCache.TryGetValue(key, out value))
            {
                value = metadata.CorrelationProperties.Any(p => p.Name == property);
                PropertyCache[key] = value;
            }

            return value;
        }

        static PropertyInfo GetUniqueProperty(SagaMetadata metadata, CorrelationProperty correlationProperty)
        {
            // TODO: Check assumption
            return metadata.SagaEntityType.GetProperties().Single(p => p.CanRead && p.Name == correlationProperty.Name);
        }

        async Task<T> GetByUniqueProperty<T>(string property, object value, IAsyncDocumentSession session, SagaPersistenceOptions options) where T : IContainSagaData
        {
            var lookupId = SagaUniqueIdentity.FormatId(typeof(T), new KeyValuePair<string, object>(property, value));

            var lookup = await session
                .Include("SagaDocId") //tell raven to pull the saga doc as well to save us a round-trip
                .LoadAsync<SagaUniqueIdentity>(lookupId)
                .ConfigureAwait(false);

            if (lookup != null)
            {
                return lookup.SagaDocId != null
                    ? await session.LoadAsync<T>(lookup.SagaDocId).ConfigureAwait(false) //if we have a saga id we can just load it
                    : await Get<T>(lookup.SagaId, options); //if not this is a saga that was created pre 3.0.4 so we fallback to a get instead
            }

            return default(T);
        }

        static async Task StoreUniqueProperty(IContainSagaData saga, IAsyncDocumentSession session, SagaMetadata sagaMetadata)
        {
            // TODO: Check assumption
            var correlationProperty = sagaMetadata.CorrelationProperties.SingleOrDefault();

            if (correlationProperty == null)
            {
                return;
            }

            var uniqueProperty = GetUniqueProperty(sagaMetadata, correlationProperty);

            var value = uniqueProperty.GetValue(saga);
            var id = SagaUniqueIdentity.FormatId(saga.GetType(), new KeyValuePair<string, object>(uniqueProperty.Name, value));
            var sagaDocId = session.Advanced.DocumentStore.Conventions.FindFullDocumentKeyFromNonStringIdentifier(saga.Id, saga.GetType(), false);

            await session.StoreAsync(new SagaUniqueIdentity
            {
                Id = id,
                SagaId = saga.Id,
                UniqueValue = value,
                SagaDocId = sagaDocId
            });

            SetUniqueValueMetadata(saga, session, new KeyValuePair<string, object>(uniqueProperty.Name, value));
        }

        static void SetUniqueValueMetadata(IContainSagaData saga, IAsyncDocumentSession session, KeyValuePair<string, object> uniqueProperty)
        {
            session.Advanced.GetMetadataFor(saga)[UniqueValueMetadataKey] = uniqueProperty.Value.ToString();
        }

        static void DeleteUniqueProperty(IContainSagaData saga, IAsyncDocumentSession session, KeyValuePair<string, object> uniqueProperty)
        {
            var id = SagaUniqueIdentity.FormatId(saga.GetType(), uniqueProperty);

            session.Advanced.Defer(new DeleteCommandData
            {
                Key = id
            });
        }
    }
}