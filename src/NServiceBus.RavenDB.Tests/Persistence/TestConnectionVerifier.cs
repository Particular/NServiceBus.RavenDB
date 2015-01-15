namespace NServiceBus.RavenDB.Tests.Persistence
{
    using NServiceBus.RavenDB.Internal;
    using NUnit.Framework;
    using Raven.Client.Document;

    public class TestConnectionVerifier
    {
        [Test, Explicit("Should only be executed when the RavenDB instance listening on port 8080 is version 3.0")]
        public void Doesnt_throw_on_ravendb3_server()
        {
            using (var documentStore = new DocumentStore
            {
                Url = "http://localhost:8080",
                DefaultDatabase = "Test"
            })
            {
                documentStore.Initialize();

                ConnectionVerifier.VerifyConnectionToRavenDBServer(documentStore);
            }
        }

        [Test, Explicit("Should only be executed when the RavenDB instance listening on port 8080 is version 2.5")]
        public void Doesnt_throw_on_ravendb25_server()
        {
            using (var documentStore = new DocumentStore
            {
                Url = "http://localhost:8080",
                DefaultDatabase = "Test"
            })
            {
                documentStore.Initialize();

                ConnectionVerifier.VerifyConnectionToRavenDBServer(documentStore);
            }
        }
    }
}
