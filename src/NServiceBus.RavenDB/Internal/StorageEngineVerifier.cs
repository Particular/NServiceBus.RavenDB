namespace NServiceBus.RavenDB.Internal
{
    using System;
    using NServiceBus.Settings;
    using Raven.Client;

    class StorageEngineVerifier
    {
        const string StorageEngineDoesntSupportDtcMessage =
            @"The selected database is using a storage engine which doesn't support DTC. Either choose another storage engine or disable distributed transactions in the transaction settings";

        internal static void VerifyStorageEngineSupportsDtcIfRequired(IDocumentStore store, ReadOnlySettings settings)
        {
            if (settings.Get<bool>("Transactions.SuppressDistributedTransactions"))
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