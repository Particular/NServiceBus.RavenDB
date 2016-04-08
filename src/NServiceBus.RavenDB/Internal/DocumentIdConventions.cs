﻿namespace NServiceBus.RavenDB.Internal
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.Saga;
    using Raven.Client;

    class DocumentIdConventions
    {
        private readonly IDocumentStore store;
        private readonly Func<Type, string> userSuppliedConventions;
        private readonly object padlock;
        private Dictionary<Type, string> mappedTypes;
        private IEnumerable<Type> types;

        public DocumentIdConventions(IDocumentStore store, IEnumerable<Type> types)
        {
            this.store = store;
            this.types = types;

            userSuppliedConventions = store.Conventions.FindTypeTagName;
            padlock = new object();
        }

        public string FindTypeTagName(Type type)
        {
            Initialize();

            string mappedValue;
            if (mappedTypes.TryGetValue(type, out mappedValue))
            {
                return mappedValue;
            }

            return userSuppliedConventions(type);
        }

        private void Initialize()
        {
            if (mappedTypes != null)
                return;

            lock (padlock)
            {
                if (mappedTypes != null)
                    return;

                var terms = GetTerms();
                var termsSet = new HashSet<string>(terms);

                var mappings = new Dictionary<Type, string>();
                MapTypeToCollectionName(typeof(TimeoutPersisters.RavenDB.TimeoutData), termsSet, mappings);

                foreach (var sagaType in types.Where(IsSagaEntity))
                {
                    MapTypeToCollectionName(sagaType, termsSet, mappings);
                }

                // Completes initialization
                this.mappedTypes = mappings;
            }
        }

        private IEnumerable<string> GetTerms()
        {
            const string DocsByEntityNameIndex = "Raven/DocumentsByEntityName";
            var index = store.DatabaseCommands.GetIndex(DocsByEntityNameIndex);
            if (index == null)
            {
                // If the index doesn't exist, then the database is new and 
                // it's not going to have any existing terms in it.
                return Enumerable.Empty<string>();
            }

            return store.DatabaseCommands.GetTerms(DocsByEntityNameIndex, "Tag", null, 1024);
        }

        private void MapTypeToCollectionName(Type type, HashSet<string> collectionNames, Dictionary<Type, string> mappings)
        {
            var byUserConvention = userSuppliedConventions(type);
            var ravenDefault = Raven.Client.Document.DocumentConvention.DefaultTypeTagName(type);
            var byLegacy = LegacyFindTypeTagName(type);

            var mappingsInPriorityOrder = new []
            {
                byUserConvention,
                ravenDefault,
                byLegacy
            };

            var collectionsThatExist = mappingsInPriorityOrder
                .Where(name => collectionNames.Contains(name))
                .ToArray();

            if (collectionsThatExist.Distinct().Count() > 1)
            {
                throw new InvalidOperationException("Multiple collections exist. Needs to be fixed.");
            }

            var blessedCollectionName = collectionsThatExist.FirstOrDefault() ?? byLegacy;
            mappings.Add(type, blessedCollectionName);
        }

        private static string LegacyFindTypeTagName(Type t)
        {
            var tagName = t.Name;

            if (IsSagaEntity(t))
            {
                tagName = tagName.Replace("Data", String.Empty);
            }

            return tagName;
        }

        private static bool IsSagaEntity(Type t)
        {
            return !t.IsAbstract && !t.IsInterface && !t.IsGenericType && typeof(IContainSagaData).IsAssignableFrom(t);
        }
    }
}
