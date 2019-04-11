namespace NServiceBus.Persistence.RavenDB
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using Newtonsoft.Json.Linq;
    using Raven.Client.Documents;
    using Raven.Client.Documents.Commands;
    using Raven.Client.Documents.Conventions;
    using Raven.Client.Documents.Operations.Indexes;
    using Sparrow.Json;
    using Sparrow.Json.Parsing;
    using Sparrow.Threading;

    class DocumentIdConventions
    {
        private readonly IDocumentStore store;
        private readonly Func<Type, string> userSuppliedConventions;
        private readonly string endpointName;
        private readonly bool sagasEnabled;
        private readonly bool timeoutsEnabled;
        private readonly string collectionNamesDocId;
        private readonly object padlock;
        private Dictionary<Type, string> mappedTypes;
        private IEnumerable<Type> types;

        public DocumentIdConventions(IDocumentStore store, IEnumerable<Type> types, string endpointName, bool sagasEnabled = true, bool timeoutsEnabled = true)
        {
            this.store = store;
            this.types = types;
            this.endpointName = endpointName;
            this.sagasEnabled = sagasEnabled;
            this.timeoutsEnabled = timeoutsEnabled;

            collectionNamesDocId = $"NServiceBus/DocumentCollectionNames/{SHA1Hash(endpointName)}";
            userSuppliedConventions = store.Conventions.FindCollectionName;
            padlock = new object();
        }

        public string FindCollectionName(Type type)
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

                var collectionData = new CollectionData();

                using (var jsonContext = new JsonOperationContext(4096, 4096, SharedMultipleUseFlag.None))
                {
                    var executor = store.GetRequestExecutor();
                    var getCommand = new GetDocumentsCommand(collectionNamesDocId, null, false);
                    executor.Execute(getCommand, jsonContext);

                    var jsonDoc = getCommand.Result.Results.FirstOrDefault() as BlittableJsonReaderObject;
                    if (jsonDoc != null)
                    {
                        var collectionNames = jsonDoc["Collections"] as BlittableJsonReaderArray;
                        foreach (string value in collectionNames.Items)
                        {
                            collectionData.Collections.Add(value);
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
                        var document = new
                        {
                            EndpointName = endpointName,
                            Collections = collectionData.Collections.ToList()
                        };

                        using (var session = store.OpenSession())
                        {
                            session.Store(document, collectionNamesDocId);
                        }
                    }

                    // Completes initialization
                    mappedTypes = collectionData.Mappings;
                }
            }
        }



        private HashSet<string> GetTerms()
        {
            const string DocsByEntityNameIndex = "Raven/DocumentsByEntityName";
            var getIndexOp = new GetIndexOperation(DocsByEntityNameIndex);

            var index = store.Maintenance.Send(getIndexOp);

            if (index == null)
            {
                throw new InvalidOperationException("The Raven/DocumentsByEntityName index must exist in order to determine the document ID strategy. This index is created by RavenDB automatically. Check in Raven Studio to make sure it exists.");
            }

            var getTermsOp = new GetTermsOperation(DocsByEntityNameIndex, "Tag", null, 1024);

            var terms = store.Maintenance.Send(getTermsOp);
            return new HashSet<string>(terms);
        }

        private void MapTypeToCollectionName(Type type, CollectionData collectionData)
        {
            var byUserConvention = userSuppliedConventions(type);
            var defaultConventions = new DocumentConventions();
            var ravenDefault = defaultConventions.FindCollectionName(type);
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

        private string SHA1Hash(string input)
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

        private static string LegacyFindTypeTagName(Type t)
        {
            var tagName = t.Name;

            if (IsSagaEntity(t))
            {
                tagName = tagName.Replace("Data", string.Empty);
            }

            return tagName;
        }

        private static bool IsSagaEntity(Type t)
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
