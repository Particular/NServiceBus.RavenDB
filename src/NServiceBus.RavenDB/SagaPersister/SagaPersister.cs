namespace NServiceBus.SagaPersisters.RavenDB
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.RavenDB.Persistence;
    using NServiceBus.RavenDB.Persistence.SagaPersister;
    using NServiceBus.Saga;
    using Raven.Abstractions.Commands;

    class SagaPersister : ISagaPersister
    {
        internal const string UniqueValueMetadataKey = "NServiceBus-UniqueValue";
        static readonly ConcurrentDictionary<string, bool> PropertyCache = new ConcurrentDictionary<string, bool>();
        readonly ISessionProvider sessionProvider;

        public SagaPersister(ISessionProvider sessionProvider)
        {
            this.sessionProvider = sessionProvider;
        }

        public bool AllowUnsafeLoads { get; set; }

        public void Save(IContainSagaData saga)
        {
            sessionProvider.Session.Store(saga);
            StoreUniqueProperty(saga);
        }

        public void Update(IContainSagaData saga)
        {
            var p = UniqueAttribute.GetUniqueProperty(saga);

            if (!p.HasValue)
            {
                return;
            }
            
            var uniqueProperty = p.Value;

            var metadata = sessionProvider.Session.Advanced.GetMetadataFor(saga);

            //if the user just added the unique property to a saga with existing data we need to set it
            if (!metadata.ContainsKey(UniqueValueMetadataKey))
            {
                StoreUniqueProperty(saga);
                return;
            }

            var storedValue = metadata[UniqueValueMetadataKey].ToString();

            var currentValue = uniqueProperty.Value.ToString();

            if (currentValue == storedValue)
            {
                return;
            }

            DeleteUniqueProperty(saga, new KeyValuePair<string, object>(uniqueProperty.Key, storedValue));
            StoreUniqueProperty(saga);
        }

        public T Get<T>(Guid sagaId) where T : IContainSagaData
        {
            return sessionProvider.Session.Load<T>(sagaId);
        }

        public T Get<T>(string property, object value) where T : IContainSagaData
        {
            if (IsUniqueProperty<T>(property))
            {
                return GetByUniqueProperty<T>(property, value);
            }

            if (!AllowUnsafeLoads)
            {
                var message = string.Format("Correlating on saga properties not marked as unique is not safe due to the high risk for stale results. Please add a [Unique] attribute to the '{0}' property on your '{1}' saga data class. If you still want to allow this please add .UsePersistence<RavenDBPersistence>().AllowStaleSagaReads() to your config",
                    property,
                    typeof(T).Name);
                throw new Exception(message);
            }

            return sessionProvider.Session.Advanced.DocumentQuery<T>()
                .WhereEquals(property, value)
                .FirstOrDefault();
        }

        public void Complete(IContainSagaData saga)
        {
            sessionProvider.Session.Delete(saga);

            var uniqueProperty = UniqueAttribute.GetUniqueProperty(saga);

            if (!uniqueProperty.HasValue)
            {
                return;
            }

            DeleteUniqueProperty(saga, uniqueProperty.Value);
        }

        public void Initialize(SagaMetaModel model)
        {
            this.model = model;
        }

        static bool IsUniqueProperty<T>(string property)
        {
            var key = typeof(T).FullName + property;
            bool value;

            if (!PropertyCache.TryGetValue(key, out value))
            {
                value = UniqueAttribute.GetUniqueProperties(typeof(T)).Any(p => p.Name == property);
                PropertyCache[key] = value;
            }

            return value;
        }

        T GetByUniqueProperty<T>(string property, object value) where T : IContainSagaData
        {
            var lookupId = SagaUniqueIdentity.FormatId(typeof(T), new KeyValuePair<string, object>(property, value));

            var lookup = sessionProvider.Session
                .Include("SagaDocId") //tell raven to pull the saga doc as well to save us a round-trip
                .Load<SagaUniqueIdentity>(lookupId);

            if (lookup != null)
            {
                return lookup.SagaDocId != null
                    ? sessionProvider.Session.Load<T>(lookup.SagaDocId) //if we have a saga id we can just load it
                    : Get<T>(lookup.SagaId); //if not this is a saga that was created pre 3.0.4 so we fallback to a get instead
            }

            return default(T);
        }

        void StoreUniqueProperty(IContainSagaData saga)
        {
            var uniqueProperty = UniqueAttribute.GetUniqueProperty(saga);

            if (!uniqueProperty.HasValue)
            {
                return;
            }

            var id = SagaUniqueIdentity.FormatId(saga.GetType(), uniqueProperty.Value);
            var sagaDocId = sessionProvider.Session.Advanced.DocumentStore.Conventions.FindFullDocumentKeyFromNonStringIdentifier(saga.Id, saga.GetType(), false);


            sessionProvider.Session.Store(new SagaUniqueIdentity
            {
                Id = id,
                SagaId = saga.Id,
                UniqueValue = uniqueProperty.Value.Value,
                SagaDocId = sagaDocId
            });

            SetUniqueValueMetadata(saga, uniqueProperty.Value);
        }

        void SetUniqueValueMetadata(IContainSagaData saga, KeyValuePair<string, object> uniqueProperty)
        {
            sessionProvider.Session.Advanced.GetMetadataFor(saga)[UniqueValueMetadataKey] = uniqueProperty.Value.ToString();
        }

        void DeleteUniqueProperty(IContainSagaData saga, KeyValuePair<string, object> uniqueProperty)
        {
            var id = SagaUniqueIdentity.FormatId(saga.GetType(), uniqueProperty);

            sessionProvider.Session.Advanced.Defer(new DeleteCommandData
            {
                Key = id
            });
        }
        SagaMetaModel model;
    }
}