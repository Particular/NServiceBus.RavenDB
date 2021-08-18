#pragma warning disable 1591

namespace NServiceBus
{
    using System;
    using Settings;
    using Raven.Client.Documents;

    [ObsoleteEx(
        Message = "Timeout manager removed. Timeout storage configuration can be removed.",
        TreatAsErrorFromVersion = "7",
        RemoveInVersion = "8"
    )]
    public static class RavenDbTimeoutSettingsExtensions
    {
        public static PersistenceExtensions<RavenDBPersistence> UseDocumentStoreForTimeouts(this PersistenceExtensions<RavenDBPersistence> cfg, IDocumentStore documentStore)
        {
            throw new NotImplementedException();
        }
        public static PersistenceExtensions<RavenDBPersistence> UseDocumentStoreForTimeouts(this PersistenceExtensions<RavenDBPersistence> cfg, Func<IReadOnlySettings, IDocumentStore> storeCreator)
        {
            throw new NotImplementedException();
        }
        public static PersistenceExtensions<RavenDBPersistence> UseDocumentStoreForTimeouts(this PersistenceExtensions<RavenDBPersistence> cfg, Func<IReadOnlySettings, IServiceProvider, IDocumentStore> storeCreator)
        {
            throw new NotImplementedException();
        }
    }
}

namespace NServiceBus.Persistence.RavenDB
{
    using System;

    partial class SagaPersistenceConfiguration
    {
        [ObsoleteEx(
            Message = "Pessimistic locking is now the default, to enable optimistic locking, use UseOptimisticLocking().",
            TreatAsErrorFromVersion = "7",
            RemoveInVersion = "8"
        )]
        public void UsePessimisticLocking(bool value = true)
        {
            throw new InvalidOperationException();
        }
    }
}