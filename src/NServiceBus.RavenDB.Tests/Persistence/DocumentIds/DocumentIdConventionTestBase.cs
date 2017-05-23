namespace NServiceBus.RavenDB.Tests.Persistence.DocumentIds
{
    using System;
    using NServiceBus.RavenDB.Internal;
    using NServiceBus.Saga;
    using Raven.Abstractions.Data;
    using Raven.Client;
    using Raven.Json.Linq;

    public abstract class DocumentIdConventionTestBase
    {
        protected const string EndpointName = "FakeEndpoint";

        protected void DirectStore(IDocumentStore store, string id, object document, string entityName)
        {
            var jsonDoc = RavenJObject.FromObject(document);
            var metadata = new RavenJObject();
            metadata["Raven-Entity-Name"] = entityName;
            var type = document.GetType();
            metadata["Raven-Clr-Type"] = $"{type.FullName}, {type.Assembly.GetName().Name}";

            Console.WriteLine($"Creating {entityName}: {id}");
            store.DatabaseCommands.Put(id, Etag.Empty, jsonDoc, metadata);
        }

        protected void StoreHiLo(IDocumentStore store, string entityName, int number)
        {
            var hiloId = $"Raven/Hilo/{entityName}";
            var document = new RavenJObject();
            document["Max"] = 32;
            var metadata = new RavenJObject();

            Console.WriteLine($"Creating {hiloId}");
            store.DatabaseCommands.Put(hiloId, null, document, metadata);
        }

        public enum ConventionType
        {
            RavenDefault,
            NSBDefault,
            Customer
        }

        protected void ApplyPrefillConventions(IDocumentStore store, ConventionType type)
        {
            ApplyConventionsInternal(store, type, true);
        }

        protected void ApplyTestConventions(IDocumentStore store, ConventionType type)
        {
            ApplyConventionsInternal(store, type, false);

            var sagaTypes = new[] { typeof(TestSagaData) };
            var conventions = new DocumentIdConventions(store, sagaTypes, EndpointName);
            store.Conventions.FindTypeTagName = conventions.FindTypeTagName;
        }

        private void ApplyConventionsInternal(IDocumentStore store, ConventionType type, bool forPrefill)
        {
            switch (type)
            {
                case ConventionType.RavenDefault:
                    // This is the default
                    break;
                case ConventionType.NSBDefault:
                    if (forPrefill)
                    {
                        store.Conventions.FindTypeTagName = LegacyFindTypeTagName;
                    }
                    break;
                case ConventionType.Customer:
                    store.Conventions.FindTypeTagName = FakeCustomerFindTypeTagName;
                    break;
            }
        }

        static string LegacyFindTypeTagName(Type t)
        {
            var tagName = t.Name;

            if (IsASagaEntity(t))
            {
                tagName = tagName.Replace("Data", String.Empty);
            }

            return tagName;
        }

        static bool IsASagaEntity(Type t)
        {
            return t != null && typeof(IContainSagaData).IsAssignableFrom(t);
        }

        static string FakeCustomerFindTypeTagName(Type t)
        {
            var charArray = t.Name.ToCharArray();
            Array.Reverse(charArray);
            return new string(charArray);
        }
    }
}
