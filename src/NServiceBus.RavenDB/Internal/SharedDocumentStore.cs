namespace NServiceBus.RavenDB
{
    using Features;
    using Internal;
    using Raven.Client;
    using Raven.Client.Document;
    using Settings;

    class SharedDocumentStore : Feature
    {
        public SharedDocumentStore()
        {
            Defaults(_ => _.Set<DocumentStoreHolder>(new DocumentStoreHolder()));

            DependsOnAtLeastOne(typeof(RavenDbSagaStorage), typeof(RavenDbSubscriptionStorage), typeof(RavenDbTimeoutStorage));
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
                var connectionStringName = Helpers.GetFirstNonEmptyConnectionString("NServiceBus/Persistence/RavenDB", "NServiceBus/Persistence");
                if (!string.IsNullOrWhiteSpace(connectionStringName))
                {
                    holder.DocumentStore = new DocumentStore { ConnectionStringName = connectionStringName }.Initialize();
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
