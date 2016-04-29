namespace NServiceBus.RavenDB.Tests.Persistence.DocumentIds
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Persistence.RavenDB;
    using Raven.Abstractions.Data;
    using Raven.Client;
    using Raven.Json.Linq;

    public abstract class DocumentIdConventionTestBase
    {
        protected const string EndpointName = "FakeEndpoint";

        protected async Task DirectStore(IDocumentStore store, string id, object document, string entityName)
        {
            var jsonDoc = RavenJObject.FromObject(document);
            var metadata = new RavenJObject();
            metadata["Raven-Entity-Name"] = entityName;
            var type = document.GetType();
            metadata["Raven-Clr-Type"] = $"{type.FullName}, {type.Assembly.GetName().Name}";

            Console.WriteLine($"Creating {entityName}: {id}");
            await store.AsyncDatabaseCommands.PutAsync(id, Etag.Empty, jsonDoc, metadata);
        }

        protected async Task StoreHiLo(IDocumentStore store, string entityName)
        {
            string hiloId = $"Raven/Hilo/{entityName}";
            var document = new RavenJObject();
            document["Max"] = 32;
            var metadata = new RavenJObject();

            Console.WriteLine($"Creating {hiloId}");
            await store.AsyncDatabaseCommands.PutAsync(hiloId, null, document, metadata);
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
