namespace NServiceBus.RavenDB.Internal
{
    using NServiceBus.Gateway.Deduplication;
    using NServiceBus.RavenDB.Persistence.SubscriptionStorage;
    using NServiceBus.TimeoutPersisters.RavenDB;
    using Raven.Abstractions.Data;
    using Raven.Client;
    using Raven.Client.Document;

    static class BackwardsCompatibilityHelper
    {
        public static void SupportOlderClrTypes(IDocumentStore documentStore)
        {
            documentStore.Conventions.FindClrType = (id, doc, metadata) =>
            {
                var clrtype = metadata.Value<string>(Constants.RavenClrType);

                if (clrtype.EndsWith(".Subscription, NServiceBus.Core"))
                {
                    clrtype = ReflectionUtil.GetFullNameWithoutVersionInformation(typeof(Subscription));
                }
                else if (clrtype.EndsWith(".GatewayMessage, NServiceBus.Core"))
                {
                    clrtype = ReflectionUtil.GetFullNameWithoutVersionInformation(typeof(GatewayMessage));
                }
                else if (clrtype.EndsWith(".Core.TimeoutData, NServiceBus.Core"))
                {
                    clrtype = ReflectionUtil.GetFullNameWithoutVersionInformation(typeof(TimeoutData));
                }

                return clrtype;
            };
        }
    }
}
