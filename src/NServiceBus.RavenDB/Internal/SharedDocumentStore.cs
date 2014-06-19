namespace NServiceBus.RavenDB
{
    using Features;
    using Internal;
    using Persistence;
    using Raven.Client;
    using Raven.Client.Document;
    using Settings;

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
                                               DefaultDatabase = p.DatabaseName ?? settings.EndpointName(),
                                               ApiKey = p.ApiKey
                                           };
                    Helpers.ApplyRavenDBConventions(settings, holder.DocumentStore);
                    holder.DocumentStore.Initialize();
                    RavenUserInstaller.AddDocumentStore(holder.DocumentStore);
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
