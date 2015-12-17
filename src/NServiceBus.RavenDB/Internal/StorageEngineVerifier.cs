namespace NServiceBus.RavenDB.Internal
{
    using System;
    using NServiceBus.ConsistencyGuarantees;
    using NServiceBus.Settings;
    using Raven.Client;

    class StorageEngineVerifier
    {
        const string StorageEngineDoesntSupportDtcMessage =
            @"The selected database is using a storage engine which doesn't support DTC. Either choose another storage engine or disable distributed transactions in the transaction settings. If you are using the selected storage engine in combination with other resources which support DTC, and therefore don't want to disable distributed transactions, you can disable this failure message by using `config.UsePersistence<RavenDBPersistence>().IConfirmToUseAStorageEngineWhichDoesntSupportDtcWhilstLeavingDistributedTransactionSupportEnabled()`.";

        internal static void VerifyStorageEngineSupportsDtcIfRequired(IDocumentStore store, ReadOnlySettings settings)
        {
            var suppressDistributedTransactions = settings.GetRequiredTransactionModeForReceives() != TransportTransactionMode.TransactionScope;
            if (suppressDistributedTransactions)
            {
                return;
            }

            if (settings.GetOrDefault<bool>("RavenDB.IConfirmToUseAStorageEngineWhichDoesntSupportDtcWhilstLeavingDistributedTransactionSupportEnabled"))
            {
                return;
            }

            var stats = store.DatabaseCommands.GetStatistics();
            if (!stats.SupportsDtc)
            {
                throw new InvalidOperationException(StorageEngineDoesntSupportDtcMessage);
            }
        }
    }
}