namespace NServiceBus.RavenDB.Persistence.SagaPersister
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using Raven.Abstractions.Commands;
    using Saga;

    class RavenSagaPersister : ISagaPersister
    {
        internal const string UniqueValueMetadataKey = "NServiceBus-UniqueValue";

        RavenSessionFactory factory;

        public RavenSagaPersister(RavenSessionFactory factory)
        {
            this.factory = factory;
        }

        public void Save(IContainSagaData saga)
        {
            factory.Session.Store(saga);
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

            var metadata = factory.Session.Advanced.GetMetadataFor(saga);

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
            return factory.Session.Load<T>(sagaId);
        }

        public T Get<T>(string property, object value) where T : IContainSagaData
        {
            if (IsUniqueProperty<T>(property))
                return GetByUniqueProperty<T>(property, value);

            return GetByQuery<T>(property, value).FirstOrDefault();
        }

        public void Complete(IContainSagaData saga)
        {
            factory.Session.Delete(saga);

            var uniqueProperty = UniqueAttribute.GetUniqueProperty(saga);

            if (!uniqueProperty.HasValue)
                return;

            DeleteUniqueProperty(saga, uniqueProperty.Value);
        }

        bool IsUniqueProperty<T>(string property)
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

            var lookup = factory.Session
                .Include("SagaDocId") //tell raven to pull the saga doc as well to save us a round-trip
                .Load<SagaUniqueIdentity>(lookupId);

            if (lookup != null)
            {
                return lookup.SagaDocId != null
                    ? factory.Session.Load<T>(lookup.SagaDocId) //if we have a saga id we can just load it
                    : Get<T>(lookup.SagaId); //if not this is a saga that was created pre 3.0.4 so we fallback to a get instead
            }

            return default(T);
        }

        IEnumerable<T> GetByQuery<T>(string property, object value) where T : IContainSagaData
        {
            try
            {
                return factory.Session.Advanced.LuceneQuery<T>()
                    .WhereEquals(property, value)
                    .WaitForNonStaleResultsAsOfNow();
            }
            catch (InvalidCastException)
            {
                return new[]
                    {
                        default(T)
                    };
            }
        }

        void StoreUniqueProperty(IContainSagaData saga)
        {
            var uniqueProperty = UniqueAttribute.GetUniqueProperty(saga);

            if (!uniqueProperty.HasValue) return;

            var id = SagaUniqueIdentity.FormatId(saga.GetType(), uniqueProperty.Value);
            var sagaDocId = factory.Store.Conventions.FindFullDocumentKeyFromNonStringIdentifier(saga.Id, saga.GetType(), false);

            factory.Session.Store(new SagaUniqueIdentity
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
            factory.Session.Advanced.GetMetadataFor(saga)[UniqueValueMetadataKey] = uniqueProperty.Value.ToString();
        }

        void DeleteUniqueProperty(IContainSagaData saga, KeyValuePair<string, object> uniqueProperty)
        {
            var id = SagaUniqueIdentity.FormatId(saga.GetType(), uniqueProperty);

            factory.Session.Advanced.Defer(new DeleteCommandData
                {
                    Key = id
                });
        }

        static readonly ConcurrentDictionary<string, bool> PropertyCache = new ConcurrentDictionary<string, bool>();
    }
}