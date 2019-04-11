namespace NServiceBus.Persistence.RavenDB
{
    using System;
    using NServiceBus.Gateway.Deduplication;
    using NServiceBus.RavenDB.Persistence.SubscriptionStorage;
    using NServiceBus.TimeoutPersisters.RavenDB;
    using Raven.Client;
    using Raven.Client.Documents;

    static class BackwardsCompatibilityHelper
    {
        public static void SupportOlderClrTypes(IDocumentStore documentStore)
        {
            documentStore.Conventions.FindClrType = (id, doc) =>
            {
                if (!doc.TryGet(Constants.Documents.Metadata.RavenClrType, out string clrType))
                {
                    return null;
                }

                if (clrType.EndsWith(".Subscription, NServiceBus.Core"))
                {
                    clrType = $"{typeof(Subscription).FullName}, NServiceBus.RavenDB";
                }
                else if (clrType.EndsWith(".GatewayMessage, NServiceBus.Core"))
                {
                    clrType = $"{typeof(GatewayMessage).FullName}, NServiceBus.RavenDB";
                }
                else if (clrType.EndsWith(".Core.TimeoutData, NServiceBus.Core"))
                {
                    clrType = $"{typeof(TimeoutData).FullName}, NServiceBus.RavenDB";
                }

                return clrType;
            };
        }

        public static string LegacyFindTypeTagName(Type t)
        {
            var tagName = t.Name;

            if (IsASagaEntity(t))
            {
                tagName = tagName.Replace("Data", string.Empty);
            }

            return tagName;
        }

        static bool IsASagaEntity(Type t)
        {
            return t != null && typeof(IContainSagaData).IsAssignableFrom(t);
        }
    }
}
