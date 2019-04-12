namespace NServiceBus.RavenDB.Tests.Persistence.DocumentIds
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Persistence.RavenDB;
    using Raven.Client.Documents;

    public abstract class DocumentIdConventionTestBase
    {
        protected const string EndpointName = "FakeEndpoint";

        protected Task DirectStore(IDocumentStore store, string id, object document, string entityName)
        {
            throw new Exception("Don't know how to do low-level stores yet.");
            //var jsonDoc = JObject.FromObject(document);
            //var metadata = new JObject();
            //metadata["Raven-Entity-Name"] = entityName;
            //var type = document.GetType();
            //metadata["Raven-Clr-Type"] = $"{type.FullName}, {type.Assembly.GetName().Name}";

            //Console.WriteLine($"Creating {entityName}: {id}");
            //return store.AsyncDatabaseCommands.PutAsync(id, Etag.Empty, jsonDoc, metadata);
        }

        protected Task StoreHiLo(IDocumentStore store, string entityName)
        {
            throw new Exception("Don't know how to do low-level stores yet.");
            //var hiloId = $"Raven/Hilo/{entityName}";
            //var document = new JObject();
            //document["Max"] = 32;
            //var metadata = new JObject();

            //Console.WriteLine($"Creating {hiloId}");
            //return store.AsyncDatabaseCommands.PutAsync(hiloId, null, document, metadata);
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
            store.Conventions.FindCollectionName = conventions.FindCollectionName;
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
                        store.Conventions.FindCollectionName = LegacyFindTypeTagName;
                    }
                    break;
                case ConventionType.Customer:
                    store.Conventions.FindCollectionName = FakeCustomerFindTypeTagName;
                    break;
            }
        }

        static string LegacyFindTypeTagName(Type t)
        {
            var tagName = t.Name;

            if (IsASagaEntity(t))
            {
                tagName = tagName.Replace("Data", string.Empty);
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
