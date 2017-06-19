namespace NServiceBus.RavenDB.Tests.Persistence
{
    using System;
    using NServiceBus.Persistence;
    using NServiceBus.Persistence.RavenDB;
    using NServiceBus.Settings;
    using NUnit.Framework;
    using Raven.Client;
    using Raven.Tests.Helpers;

    [TestFixture]
    public class DocumentStoreManagerTests : RavenTestBase
    {
        [Test]
        public void Specific_stores_should_mask_default()
        {
            var settings = new SettingsHolder();
            settings.Set("Transactions.SuppressDistributedTransactions", true);
            settings.Set("TypesToScan", new Type[0]);
            settings.Set("NServiceBus.Routing.EndpointName", "FakeEndpoint");
            settings.Set("NServiceBus.Transport.TransportInfrastructure", new FakeRavenDBTransportInfrastructure(TransportTransactionMode.None));
            
            DocumentStoreManager.SetDocumentStore<StorageType.GatewayDeduplication>(settings, EmbeddedStore("GatewayDeduplication"));
            DocumentStoreManager.SetDocumentStore<StorageType.Outbox>(settings, EmbeddedStore("Outbox"));
            DocumentStoreManager.SetDocumentStore<StorageType.Sagas>(settings, EmbeddedStore("Sagas"));
            DocumentStoreManager.SetDocumentStore<StorageType.Subscriptions>(settings, EmbeddedStore("Subscriptions"));
            DocumentStoreManager.SetDocumentStore<StorageType.Timeouts>(settings, EmbeddedStore("Timeouts"));
            DocumentStoreManager.SetDefaultStore(settings, EmbeddedStore("Default"));

            var readOnly = settings as ReadOnlySettings;

            Assert.AreEqual("GatewayDeduplication", DocumentStoreManager.GetDocumentStore<StorageType.GatewayDeduplication>(readOnly).Identifier);
            Assert.AreEqual("Outbox", DocumentStoreManager.GetDocumentStore<StorageType.Outbox>(readOnly).Identifier);
            Assert.AreEqual("Sagas", DocumentStoreManager.GetDocumentStore<StorageType.Sagas>(readOnly).Identifier);
            Assert.AreEqual("Subscriptions", DocumentStoreManager.GetDocumentStore<StorageType.Subscriptions>(readOnly).Identifier);
            Assert.AreEqual("Timeouts", DocumentStoreManager.GetDocumentStore<StorageType.Timeouts>(readOnly).Identifier);
        }

        [Test]
        public void Should_construct_store_based_on_connection_params()
        {
            var connectionParams = new ConnectionParameters
            {
                Url = TestConstants.RavenUrl,
                DatabaseName = "TestConnectionParams"
            };

            var settings = DefaultSettings();
            settings.Set(RavenDbSettingsExtensions.DefaultConnectionParameters, connectionParams);

            var storeInitializer = DocumentStoreManager.GetUninitializedDocumentStore<StorageType.Sagas>(settings);

            storeInitializer.EnsureDocStoreCreated(settings);
            Assert.AreEqual(TestConstants.RavenUrl, storeInitializer.Url);
            Assert.AreEqual($"{TestConstants.RavenUrl} (DB: TestConnectionParams)", storeInitializer.Identifier);
        }

        [Test]
        public void Should_create_default_connection()
        {
            var settings = DefaultSettings();

            var storeInitializer = DocumentStoreManager.GetUninitializedDocumentStore<StorageType.Timeouts>(settings);

            storeInitializer.EnsureDocStoreCreated(settings);
            Assert.AreEqual("http://localhost:8080", storeInitializer.Url);
            Assert.AreEqual("http://localhost:8080 (DB: FakeEndpoint)", storeInitializer.Identifier);
        }

        IDocumentStore EmbeddedStore(string identifier)
        {
            var store = NewDocumentStore();
            store.Identifier = identifier;
            return store;
        }

        SettingsHolder DefaultSettings()
        {
            var settings = new SettingsHolder();
            settings.Set("NServiceBus.LocalAddress", "FakeAddress");
            settings.Set("EndpointVersion", "FakeVersion");
            settings.SetDefault("Transactions.SuppressDistributedTransactions", false);
            settings.Set("NServiceBus.Routing.EndpointName", "FakeEndpoint");
            settings.Set<SingleSharedDocumentStore>(new SingleSharedDocumentStore());

            return settings;
        }
    }
}
