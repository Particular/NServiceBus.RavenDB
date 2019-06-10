namespace NServiceBus.Persistence.RavenDB
{
    using System;

    /// <summary>
    /// This class provides advanced extension methods for RavenDB persistence configuration.
    /// </summary>
    public static class DocumentIdConventionsExtensions
    {
        /// <summary>
        /// Do not use legacy DocumentId mapping strategies from previous versions of NServiceBus.RavenDB.
        /// This is a breaking change which, if applied to an existing database, will result in lost Saga and Timeout data.
        /// Do not use this on an existing database under any circumstances.
        /// </summary>
        [ObsoleteEx(
            Message = "NServiceBus will now use whatever document conventions are configured on the DocumentStore. It this method was used before, it can safely be removed as it is now the default.",
            TreatAsErrorFromVersion = "6.0.0",
            RemoveInVersion = "7.0.0")]
        public static PersistenceExtensions<RavenDBPersistence> DoNotUseLegacyConventionsWhichIsOnlySafeForNewEndpoints(this PersistenceExtensions<RavenDBPersistence> config)
        {
            throw new NotImplementedException();
        }
    }
}
