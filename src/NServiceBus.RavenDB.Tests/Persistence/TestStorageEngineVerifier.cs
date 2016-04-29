namespace NServiceBus.RavenDB.Tests.Persistence
{
    using System;
    using NServiceBus.Persistence.RavenDB;
    using NServiceBus.Settings;
    using NServiceBus.Transports;
    using NUnit.Framework;
    using Raven.Client.Document;

    public class TestStorageEngineVerifier
    {
        [Test]
        public void Throws_when_voron_combined_with_dtc()
        {
            using (var documentStore = new DocumentStore { Url = "http://localhost:8083" })
            using (documentStore.SetupVoronTest())
            {
                var settings = new SettingsHolder();
                settings.Set<TransportInfrastructure>(new FakeRavenDBTransportInfrastructure(TransportTransactionMode.TransactionScope));
                Assert.Throws<InvalidOperationException>(() => StorageEngineVerifier.VerifyStorageEngineSupportsDtcIfRequired(documentStore, settings));
            }
        }

        [Test]
        public void DoesntThrow_when_voron_combined_with_dtc_including_confirmation()
        {
            using (var documentStore = new DocumentStore { Url = "http://localhost:8083" })
            using (documentStore.SetupVoronTest())
            {

                var settings = new SettingsHolder();
                settings.Set<TransportInfrastructure>(new FakeRavenDBTransportInfrastructure(TransportTransactionMode.TransactionScope));
                settings.Set("RavenDB.IConfirmToUseAStorageEngineWhichDoesntSupportDtcWhilstLeavingDistributedTransactionSupportEnabled", true);

                Assert.DoesNotThrow(() => StorageEngineVerifier.VerifyStorageEngineSupportsDtcIfRequired(documentStore, settings));
            }
        }

        [Test]
        public void DoesntThrow_when_voron_without_dtc()
        {
            using (var documentStore = new DocumentStore { Url = "http://localhost:8083" })
            using (documentStore.SetupVoronTest())
            {
                var settings = new SettingsHolder();
                settings.Set<TransportInfrastructure>(new FakeRavenDBTransportInfrastructure(TransportTransactionMode.ReceiveOnly));

                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                Assert.DoesNotThrow(() => StorageEngineVerifier.VerifyStorageEngineSupportsDtcIfRequired(documentStore, settings));
            }
        }
    }
}