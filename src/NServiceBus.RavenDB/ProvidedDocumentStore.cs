using System;

namespace NServiceBus.RavenDB
{
    using Features;
    using Internal;
    using NServiceBus.Persistence;
    using Raven.Client;
    using Raven.Client.Document;

    public class ProvidedDocumentStore : Feature
    {
        public ProvidedDocumentStore()
        {
            DependsOnAtLeastOne(typeof(RavenDbSagaStorage), typeof(RavenDbSubscriptionStorage), typeof(RavenDbTimeoutStorage));
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            var store = context.Settings.GetOrDefault<IDocumentStore>(RavenDbSettingsExtenstions.DocumentStoreSettingsKey);
            if (store == null)
            {
                var connectionStringName = Helpers.GetFirstNonEmptyConnectionString("NServiceBus/Persistence/RavenDB", "NServiceBus/Persistence");
                if (!string.IsNullOrWhiteSpace(connectionStringName))
                {
                    store = new DocumentStore { ConnectionStringName = connectionStringName }.Initialize();
                }
            }
            // TODO expose the store to be used by other Features
        }
    }
}
