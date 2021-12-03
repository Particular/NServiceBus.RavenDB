#pragma warning disable 1591

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