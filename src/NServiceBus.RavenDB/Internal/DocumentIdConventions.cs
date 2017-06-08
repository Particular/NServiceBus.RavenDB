namespace NServiceBus.Persistence.RavenDB
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using Raven.Client;
    using Raven.Json.Linq;

    class DocumentIdConventions
    {
        readonly IDocumentStore store;
        readonly Func<Type, string> userSuppliedConventions;
        readonly string endpointName;
        readonly bool sagasEnabled;
        readonly bool timeoutsEnabled;
        readonly string collectionNamesDocId;
        readonly object padlock;
        Dictionary<Type, string> mappedTypes;
        IEnumerable<Type> types;

        public DocumentIdConventions(IDocumentStore store, IEnumerable<Type> types, string endpointName, bool sagasEnabled = true, bool timeoutsEnabled = true)
        {
            this.store = store;
            this.types = types;
            this.endpointName = endpointName;
            this.sagasEnabled = sagasEnabled;
            this.timeoutsEnabled = timeoutsEnabled;

            collectionNamesDocId = $"NServiceBus/DocumentCollectionNames/{SHA1Hash(endpointName)}";
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

        void Initialize()
        {
            if (mappedTypes != null)
                return;

            lock (padlock)
            {
                if (mappedTypes != null)
                    return;

                var collectionData = new CollectionData();

                var jsonDoc = store.DatabaseCommands.Get(collectionNamesDocId);
                if (jsonDoc != null)
                {
                    var collectionNames = jsonDoc.DataAsJson["Collections"] as RavenJArray;
                    foreach (RavenJValue value in collectionNames)
                    {
                        collectionData.Collections.Add(value.Value as string);
                    }
                }

                if (timeoutsEnabled)
                {
                    MapTypeToCollectionName(typeof(TimeoutPersisters.RavenDB.TimeoutData), collectionData);
                }
                if (sagasEnabled)
                {
                    foreach (var sagaType in types.Where(IsSagaEntity))
                    {
                        MapTypeToCollectionName(sagaType, collectionData);
                    }
                }

                if (collectionData.Changed)
                {
                    var newDoc = new RavenJObject();
                    var list = new RavenJArray();
                    foreach (var name in collectionData.Collections)
                    {
                        list.Add(new RavenJValue(name));
                    }
                    newDoc["EndpointName"] = endpointName;
                    newDoc["EndpointName"] = endpointName;
                    newDoc["Collections"] = list;
                    var metadata = new RavenJObject();
                    store.DatabaseCommands.Put(collectionNamesDocId, null, newDoc, metadata);
                }

                // Completes initialization
                mappedTypes = collectionData.Mappings;
            }
        }



        HashSet<string> GetTerms()
        {
            const string DocsByEntityNameIndex = "Raven/DocumentsByEntityName";
            var index = store.DatabaseCommands.GetIndex(DocsByEntityNameIndex);
            if (index == null)
            {
                throw new InvalidOperationException("The Raven/DocumentsByEntityName index must exist in order to determine the document ID strategy. This index is created by RavenDB automatically. Check in Raven Studio to make sure it exists.");
            }

            var terms = store.DatabaseCommands.GetTerms(DocsByEntityNameIndex, "Tag", null, 1024);
            return new HashSet<string>(terms);
        }

        void MapTypeToCollectionName(Type type, CollectionData collectionData)
        {
            var byUserConvention = userSuppliedConventions(type);
            var ravenDefault = Raven.Client.Document.DocumentConvention.DefaultTypeTagName(type);
            var byLegacy = LegacyFindTypeTagName(type);

            var mappingsInPriorityOrder = new[]
            {
                byUserConvention,
                ravenDefault,
                byLegacy
            };

            var configuredName = mappingsInPriorityOrder
                .Distinct()
                .SingleOrDefault(name => collectionData.Collections.Contains(name));

            if (configuredName == null)
            {
                if (collectionData.IndexResults == null)
                {
                    collectionData.IndexResults = GetTerms();
                }

                var collectionsThatExist = mappingsInPriorityOrder
                    .Distinct()
                    .Where(name => collectionData.IndexResults.Contains(name))
                    .ToArray();


                if (collectionsThatExist.Length > 1)
                {
                    var options = string.Join(", ", collectionsThatExist);
                    throw new InvalidOperationException($"Multiple RavenDB collection names ({options}) found for type `{type.FullName}`. Unable to determine DocumentId naming strategy for this type. Remove or modify the documents that were mapped incorrectly.");
                }

                configuredName = collectionsThatExist.FirstOrDefault() ?? ravenDefault;
                collectionData.Collections.Add(configuredName);
                collectionData.Changed = true;
            }

            collectionData.Mappings.Add(type, configuredName);
        }

        string SHA1Hash(string input)
        {
            using (var sha = new SHA1CryptoServiceProvider()) // Is FIPS compliant
            {
                var inBytes = Encoding.UTF8.GetBytes(input);
                var hashBytes = sha.ComputeHash(inBytes);
                var builder = new StringBuilder();
                foreach (var b in hashBytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }

        static string LegacyFindTypeTagName(Type t)
        {
            var tagName = t.Name;

            if (IsSagaEntity(t))
            {
                tagName = tagName.Replace("Data", String.Empty);
            }

            return tagName;
        }

        static bool IsSagaEntity(Type t)
        {
            return !t.IsAbstract && !t.IsInterface && !t.IsGenericType && typeof(IContainSagaData).IsAssignableFrom(t);
        }

        class CollectionData
        {
            public HashSet<string> Collections { get; } = new HashSet<string>();
            public Dictionary<Type, string> Mappings { get; } = new Dictionary<Type, string>();
            public HashSet<string> IndexResults { get; set; }
            public bool Changed { get; set; }
        }
    }
}
