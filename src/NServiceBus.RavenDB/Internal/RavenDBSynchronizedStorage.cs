using NServiceBus.Extensibility;
using NServiceBus.Persistence;
using NServiceBus.SagaPersisters.RavenDB;
using NServiceBus.Settings;
using Raven.Client;
using System.Threading.Tasks;

namespace NServiceBus.RavenDB.Internal
{
    class RavenDBSynchronizedStorage : ISynchronizedStorage
    {
        ReadOnlySettings settings;

        public RavenDBSynchronizedStorage(ReadOnlySettings settings)
        {
            this.settings = settings;
        }

        public Task<CompletableSynchronizedStorageSession> OpenSession(ContextBag contextBag)
        {
            var store = settings.GetOrDefault<IDocumentStore>(RavenDbSettingsExtensions.DocumentStoreSettingsKey)
                ?? SharedDocumentStore.Get(settings);

            var session = store.OpenAsyncSession();

            return Task.FromResult((CompletableSynchronizedStorageSession)new RavenDBSynchronizedStorageSession(session, true));
        }
    }

}
