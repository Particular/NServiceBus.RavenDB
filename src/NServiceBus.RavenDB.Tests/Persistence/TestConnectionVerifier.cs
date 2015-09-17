namespace NServiceBus.RavenDB.Tests.Persistence
{
    using System;
    using NServiceBus.RavenDB.Internal;
    using NUnit.Framework;
    using Raven.Client.Document;

    public class TestConnectionVerifier
    {
        [Test]
        public void AreVersionsCompatible()
        {
            Assert.IsTrue(ConnectionVerifier.AreVersionsCompatible(
                server: new Version(1, 0, 0),
                client: new Version(1, 0, 1)),
                "client and server within same major and minor");
            Assert.IsTrue(ConnectionVerifier.AreVersionsCompatible(
                server: new Version(1, 0, 1),
                client: new Version(1, 0, 0)),
                "client and server within same major and minor");
            Assert.IsTrue(ConnectionVerifier.AreVersionsCompatible(
                server: new Version(1, 0, 1),
                client: new Version(1, 0, 0)),
                "server higher than client");
            Assert.IsTrue(ConnectionVerifier.AreVersionsCompatible(
                server: new Version(1, 1, 0),
                client: new Version(1, 0, 0)),
                "server higher than client");
            Assert.IsTrue(ConnectionVerifier.AreVersionsCompatible(
                server: new Version(2, 0, 0),
                client: new Version(1, 0, 0)),
                "server higher than client");

            Assert.IsFalse(ConnectionVerifier.AreVersionsCompatible(
                server: new Version(1, 0, 0),
                client: new Version(1, 1, 0)),
                "client higher minor than server");
            Assert.IsFalse(ConnectionVerifier.AreVersionsCompatible(
                server: new Version(1, 0, 0),
                client: new Version(2, 0, 0)),
                "client higher major than server");
        }

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

                Assert.Throws<Exception>(() => ConnectionVerifier.VerifyConnectionToRavenDBServer(documentStore));
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
