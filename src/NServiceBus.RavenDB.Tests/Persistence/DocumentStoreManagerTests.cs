﻿namespace NServiceBus.RavenDB.Tests.Persistence
{
    using NServiceBus.Configuration.AdvancedExtensibility;
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
                var cfg = new EndpointConfiguration("FakeEndpoint");
                cfg.UseTransport(new LearningTransport());
                cfg.SendOnly();

                var persistence = cfg.UsePersistence<RavenDBPersistence>();
                var settings = persistence.GetSettings();

                DocumentStoreManager.SetDocumentStore<StorageType.Outbox>(settings, db.NewStore("Outbox"));
                DocumentStoreManager.SetDocumentStore<StorageType.Sagas>(settings, db.NewStore("Sagas"));
                DocumentStoreManager.SetDocumentStore<StorageType.Subscriptions>(settings, db.NewStore("Subscriptions"));
                DocumentStoreManager.SetDefaultStore(settings, db.NewStore("Default"));

                var readOnly = settings as ReadOnlySettings;

                Assert.AreEqual("Outbox", DocumentStoreManager.GetDocumentStore<StorageType.Outbox>(readOnly, null).Identifier);
                Assert.AreEqual("Sagas", DocumentStoreManager.GetDocumentStore<StorageType.Sagas>(readOnly, null).Identifier);
                Assert.AreEqual("Subscriptions", DocumentStoreManager.GetDocumentStore<StorageType.Subscriptions>(readOnly, null).Identifier);
            }
        }
    }
}
