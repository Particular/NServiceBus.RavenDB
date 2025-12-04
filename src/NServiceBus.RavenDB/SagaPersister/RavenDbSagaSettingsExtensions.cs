namespace NServiceBus;

using System;
using NServiceBus.Configuration.AdvancedExtensibility;
using NServiceBus.Persistence.RavenDB;
using NServiceBus.Settings;
using Raven.Client.Documents;

/// <summary>
/// Provides configuration options
/// </summary>
public static class RavenDbSagaSettingsExtensions
{
    /// <summary>
    /// Configures the given document store to be used when storing sagas
    /// </summary>
    /// <param name="cfg">Object to attach to</param>
    /// <param name="documentStore">The document store to be used</param>
    public static PersistenceExtensions<RavenDBPersistence> UseDocumentStoreForSagas(this PersistenceExtensions<RavenDBPersistence> cfg, IDocumentStore documentStore)
    {
        DocumentStoreManager.SetDocumentStore<StorageType.Sagas>(cfg.GetSettings(), documentStore);
        return cfg;
    }

    /// <summary>
    /// Configures the given document store to be used when storing sagas
    /// </summary>
    /// <param name="cfg">Object to attach to</param>
    /// <param name="storeCreator">A Func that will create the document store on NServiceBus initialization.</param>
    public static PersistenceExtensions<RavenDBPersistence> UseDocumentStoreForSagas(this PersistenceExtensions<RavenDBPersistence> cfg, Func<IReadOnlySettings, IDocumentStore> storeCreator)
    {
        DocumentStoreManager.SetDocumentStore<StorageType.Sagas>(cfg.GetSettings(), storeCreator);
        return cfg;
    }

    /// <summary>
    /// Configures the given document store to be used when storing sagas
    /// </summary>
    /// <param name="cfg">Object to attach to</param>
    /// <param name="storeCreator">A Func that will create the document store on NServiceBus initialization.</param>
    public static PersistenceExtensions<RavenDBPersistence> UseDocumentStoreForSagas(this PersistenceExtensions<RavenDBPersistence> cfg, Func<IReadOnlySettings, IServiceProvider, IDocumentStore> storeCreator)
    {
        DocumentStoreManager.SetDocumentStore<StorageType.Sagas>(cfg.GetSettings(), storeCreator);
        return cfg;
    }

    internal const string AllowStaleSagaReadsKey = "RavenDB.AllowStaleSagaReads";
}