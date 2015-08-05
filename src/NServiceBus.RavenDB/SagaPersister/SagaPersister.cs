namespace NServiceBus.SagaPersisters.RavenDB
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using NServiceBus.RavenDB.Persistence.SagaPersister;
    using NServiceBus.Saga;
    using Raven.Abstractions.Commands;
    using Raven.Client;

    class SagaPersister : ISagaPersister
    {
        internal const string UniqueValueMetadataKey = "NServiceBus-UniqueValue";
        static readonly ConcurrentDictionary<string, bool> PropertyCache = new ConcurrentDictionary<string, bool>();

        public bool AllowUnsafeLoads { get; set; }

        public void Save(IContainSagaData saga, SagaPersistenceOptions options)
        {
            var session = options.GetSession();
            session.Store(saga);
            StoreUniqueProperty(saga, session, options.Metadata);
        }

        public void Update(IContainSagaData saga, SagaPersistenceOptions options)
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
                StoreUniqueProperty(saga, session, sagaMetadata);
                return;
            }

            var storedValue = ravenMetadata[UniqueValueMetadataKey].ToString();

            var currentValue = uniqueProperty.GetValue(saga).ToString();

            if (currentValue == storedValue)
            {
                return;
            }

            DeleteUniqueProperty(saga, session, new KeyValuePair<string, object>(uniqueProperty.Name, storedValue));
            StoreUniqueProperty(saga, session, sagaMetadata);
        }

        public T Get<T>(Guid sagaId, SagaPersistenceOptions options) where T : IContainSagaData
        {
            var session = options.GetSession();
            return session.Load<T>(sagaId);
        }

        public T Get<T>(string property, object value, SagaPersistenceOptions options) where T : IContainSagaData
        {
            var session = options.GetSession();
            if (IsUniqueProperty<T>(options.Metadata, property))
            {
                return GetByUniqueProperty<T>(property, value, session, options);
            }

            if (!AllowUnsafeLoads)
            {
                var message = string.Format("Correlating on saga properties not marked as unique is not safe due to the high risk for stale results. Please add a [Unique] attribute to the '{0}' property on your '{1}' saga data class. If you still want to allow this please add .UsePersistence<RavenDBPersistence>().AllowStaleSagaReads() to your config",
                    property,
                    typeof(T).Name);
                throw new Exception(message);
            }

            return session.Advanced.DocumentQuery<T>()
                .WhereEquals(property, value)
                .FirstOrDefault();
        }

        public void Complete(IContainSagaData saga, SagaPersistenceOptions options)
        {
            var session = options.GetSession();
            session.Delete(saga);

            // TODO: Check assumption
            var correlationProperty = options.Metadata.CorrelationProperties.SingleOrDefault();

            if (correlationProperty == null)
            {
                return;
            }

            var uniqueProperty = GetUniqueProperty(options.Metadata, correlationProperty);
            DeleteUniqueProperty(saga, session, new KeyValuePair<string, object>(uniqueProperty.Name, uniqueProperty.GetValue(saga)));
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

        T GetByUniqueProperty<T>(string property, object value, IDocumentSession session, SagaPersistenceOptions options) where T : IContainSagaData
        {
            var lookupId = SagaUniqueIdentity.FormatId(typeof(T), new KeyValuePair<string, object>(property, value));

            var lookup = session
                .Include("SagaDocId") //tell raven to pull the saga doc as well to save us a round-trip
                .Load<SagaUniqueIdentity>(lookupId);

            if (lookup != null)
            {
                return lookup.SagaDocId != null
                    ? session.Load<T>(lookup.SagaDocId) //if we have a saga id we can just load it
                    : Get<T>(lookup.SagaId, options); //if not this is a saga that was created pre 3.0.4 so we fallback to a get instead
            }

            return default(T);
        }

        static void StoreUniqueProperty(IContainSagaData saga, IDocumentSession session, SagaMetadata sagaMetadata)
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


            session.Store(new SagaUniqueIdentity
            {
                Id = id,
                SagaId = saga.Id,
                UniqueValue = value,
                SagaDocId = sagaDocId
            });

            SetUniqueValueMetadata(saga, session, new KeyValuePair<string, object>(uniqueProperty.Name, value));
        }

        static void SetUniqueValueMetadata(IContainSagaData saga, IDocumentSession session, KeyValuePair<string, object> uniqueProperty)
        {
            session.Advanced.GetMetadataFor(saga)[UniqueValueMetadataKey] = uniqueProperty.Value.ToString();
        }

        static void DeleteUniqueProperty(IContainSagaData saga, IDocumentSession session, KeyValuePair<string, object> uniqueProperty)
        {
            var id = SagaUniqueIdentity.FormatId(saga.GetType(), uniqueProperty);

            session.Advanced.Defer(new DeleteCommandData
            {
                Key = id
            });
        }
    }
}