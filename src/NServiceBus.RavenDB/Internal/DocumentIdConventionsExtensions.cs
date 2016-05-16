namespace NServiceBus.Persistence.RavenDB
{
    using NServiceBus.Configuration.AdvanceExtensibility;
    using NServiceBus.Settings;

    /// <summary>
    /// This class provides advanced extension methods for RavenDB persistence configuration. 
    /// </summary>
    public static class DocumentIdConventionsExtensions
    {
        const string DoNotUseLegacyConventionsSettingsKey = "NServiceBus.RavenDB.DoNotUseLegacyConventions";

        /// <summary>
        /// Do not use legacy DocumentId mapping strategies from previous versions of NServiceBus.RavenDB.
        /// This is a breaking change which, if applied to an existing database, will result in lost Saga and Timeout data.
        /// Do not use this on an existing database under any circumstances.
        /// </summary>
        public static PersistenceExtentions<RavenDBPersistence> DoNotUseLegacyConventionsWhichIsOnlySafeForNewEndpoints(this PersistenceExtentions<RavenDBPersistence> config)
        {
            config.GetSettings().Set(DoNotUseLegacyConventionsSettingsKey, true);
            return config;
        }

        internal static bool NeedToApplyDocumentIdConventionsToDocumentStore(ReadOnlySettings settings)
        {
            return settings.GetOrDefault<bool>(DoNotUseLegacyConventionsSettingsKey) == false;
        }
    }
}
