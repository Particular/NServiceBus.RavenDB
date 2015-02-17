namespace NServiceBus.RavenDB.Tests.Persistence
{
    using System;
    using NServiceBus.RavenDB.Internal;
    using NUnit.Framework;
    using Raven.Client.Document;

    public class TestConnectionVerifier
    {
        [Test]
        public void Throws_on_ravendb25_server()
        {
            using (var documentStore = new DocumentStore
            {
                Url = "http://localhost:8081", // RavenDB 2.5 is running on port 8081 on the test agents
                DefaultDatabase = "Test"
            })
            {
                documentStore.Initialize();

                Assert.Throws <InvalidOperationException>(() => ConnectionVerifier.VerifyConnectionToRavenDBServer(documentStore));
            }
        }

        [Test]
        public void Doesnt_throw_on_ravendb3_server()
        {
            using (var documentStore = new DocumentStore
            {
                Url = "http://localhost:8083", // RavenDB 3.0 is running on port 8083 on the test agents
                DefaultDatabase = "Test"
            })
            {
                documentStore.Initialize();

                ConnectionVerifier.VerifyConnectionToRavenDBServer(documentStore);
            }
        }
    }
}
