namespace NServiceBus.RavenDB.Tests.Persistence
{
    using System;
    using NServiceBus.Persistence.RavenDB;
    using NServiceBus.Settings;
    using NUnit.Framework;

    [TestFixture]
    public class DocumentStoreManagerTests
    {
        [Test]
        public void Specific_stores_should_mask_default()
        {
            using (var db = new ReusableDB())
            {
                var settings = new SettingsHolder();
                settings.Set("Transactions.SuppressDistributedTransactions", true);
                settings.Set("TypesToScan", new Type[0]);
                settings.Set("NServiceBus.Routing.EndpointName", "FakeEndpoint");
                settings.Set("NServiceBus.Transport.TransportInfrastructure", new FakeRavenDBTransportInfrastructure(TransportTransactionMode.None));
                settings.Set("Endpoint.SendOnly", true);

#pragma warning disable 618
                DocumentStoreManager.SetDocumentStore<StorageType.GatewayDeduplication>(settings, db.NewStore("GatewayDeduplication"));
#pragma warning restore 618
                DocumentStoreManager.SetDocumentStore<StorageType.Outbox>(settings, db.NewStore("Outbox"));
                DocumentStoreManager.SetDocumentStore<StorageType.Sagas>(settings, db.NewStore("Sagas"));
                DocumentStoreManager.SetDocumentStore<StorageType.Subscriptions>(settings, db.NewStore("Subscriptions"));
                DocumentStoreManager.SetDocumentStore<StorageType.Timeouts>(settings, db.NewStore("Timeouts"));
                DocumentStoreManager.SetDefaultStore(settings, db.NewStore("Default"));

                var readOnly = settings as ReadOnlySettings;

#pragma warning disable 618
                Assert.AreEqual("GatewayDeduplication", DocumentStoreManager.GetDocumentStore<StorageType.GatewayDeduplication>(readOnly, null).Identifier);
#pragma warning restore 618
                Assert.AreEqual("Outbox", DocumentStoreManager.GetDocumentStore<StorageType.Outbox>(readOnly, null).Identifier);
                Assert.AreEqual("Sagas", DocumentStoreManager.GetDocumentStore<StorageType.Sagas>(readOnly, null).Identifier);
                Assert.AreEqual("Subscriptions", DocumentStoreManager.GetDocumentStore<StorageType.Subscriptions>(readOnly, null).Identifier);
                Assert.AreEqual("Timeouts", DocumentStoreManager.GetDocumentStore<StorageType.Timeouts>(readOnly, null).Identifier);
            }
        }
    }
}
