namespace NServiceBus.RavenDB.Tests.Persistence
{
    using NServiceBus.RavenDB.Internal;
    using NUnit.Framework;
    using Raven.Client.Document;

    public class TestConnectionVerifier
    {
        [Test]
        public void Doesnt_throw_on_ravendb25_server()
        {
            using (var documentStore = new DocumentStore
            {
                Url = "http://localhost:8081", // RavenDB 2.5 is running on port 8081 on the test agents
                DefaultDatabase = "Test"
            })
            {
                documentStore.Initialize();

                ConnectionVerifier.VerifyConnectionToRavenDBServer(documentStore);
            }
        }
    }
}
