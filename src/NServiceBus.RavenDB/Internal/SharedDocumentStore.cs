namespace NServiceBus.RavenDB
{
    using NServiceBus.Features;
    using NServiceBus.RavenDB.Internal;
    using NServiceBus.Settings;
    using Raven.Client;
    using Raven.Client.Document;

    class SharedDocumentStore : Feature
    {
        public SharedDocumentStore()
        {
            Defaults(_ => _.Set<DocumentStoreHolder>(new DocumentStoreHolder()));
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            // no-op, the docstore is initialized lazily
        }

        public static IDocumentStore Get(ReadOnlySettings settings)
        {
            // The holder is known to be non-null since we set it in the ctor
            var holder = settings.Get<DocumentStoreHolder>();
            if (holder.DocumentStore == null)
            {
                var p = settings.GetOrDefault<ConnectionParameters>(RavenDbSettingsExtensions.DefaultConnectionParameters);
                if (p != null)
                {
                    holder.DocumentStore = new DocumentStore
                    {
                        Url = p.Url,
                        DefaultDatabase = p.DatabaseName ?? settings.EndpointName().ToString(),
                        ApiKey = p.ApiKey,
                        Credentials = p.Credentials
                    };
                    Helpers.ApplyRavenDBConventions(settings, holder.DocumentStore);
                    holder.DocumentStore.Initialize();
                }
                else
                {
                    holder.DocumentStore = Helpers.CreateDocumentStoreByConnectionStringName(settings, "NServiceBus/Persistence/RavenDB", "NServiceBus/Persistence");

                    if (holder.DocumentStore == null)
                    {
                        holder.DocumentStore = Helpers.CreateDocumentStoreByUrl(settings, "http://localhost:8080");
                    }
                }
            }
            return holder.DocumentStore;
        }

        class DocumentStoreHolder
        {
            public IDocumentStore DocumentStore { get; set; }
        }
    }
}