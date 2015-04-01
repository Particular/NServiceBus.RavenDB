namespace NServiceBus.RavenDB.Internal
{
    using System;
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

                // The CLR type cannot be assumed to be always there
                if (clrtype == null)
                {
                    return null;
                }

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
