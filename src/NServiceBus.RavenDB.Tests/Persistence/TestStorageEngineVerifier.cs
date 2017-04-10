﻿namespace NServiceBus.RavenDB.Tests.Persistence
{
    using System;
    using NServiceBus.RavenDB.Internal;
    using NServiceBus.Settings;
    using NUnit.Framework;
    using Raven.Client.Document;

    public class TestStorageEngineVerifier
    {
        [Test]
        public void Throws_when_voron_combined_with_dtc()
        {
            using (var documentStore = new DocumentStore { Url = TestConstants.RavenUrl })
            using (documentStore.SetupVoronTest())
            {

                var settings = new SettingsHolder();
                settings.Set("Transactions.SuppressDistributedTransactions", false);

                Assert.Throws<InvalidOperationException>(() => StorageEngineVerifier.VerifyStorageEngineSupportsDtcIfRequired(documentStore, settings));
            }
        }

        [Test]
        public void DoesntThrow_when_voron_combined_with_dtc_including_confirmation()
        {
            using (var documentStore = new DocumentStore { Url = TestConstants.RavenUrl })
            using (documentStore.SetupVoronTest())
            {

                var settings = new SettingsHolder();
                settings.Set("Transactions.SuppressDistributedTransactions", false);
                settings.Set("RavenDB.IConfirmToUseAStorageEngineWhichDoesntSupportDtcWhilstLeavingDistributedTransactionSupportEnabled", true);

                Assert.DoesNotThrow(() => StorageEngineVerifier.VerifyStorageEngineSupportsDtcIfRequired(documentStore, settings));
            }
        }

        [Test]
        public void DoesntThrow_when_voron_without_dtc()
        {
            using (var documentStore = new DocumentStore { Url = TestConstants.RavenUrl })
            using (documentStore.SetupVoronTest())
            {
                var settings = new SettingsHolder();
                settings.Set("Transactions.SuppressDistributedTransactions", true);

                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                Assert.DoesNotThrow(() => StorageEngineVerifier.VerifyStorageEngineSupportsDtcIfRequired(documentStore, settings));
            }
        }
    }
}