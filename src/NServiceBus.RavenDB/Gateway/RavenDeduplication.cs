namespace NServiceBus.RavenDB.Gateway.Deduplication
{
    using System;
    using NServiceBus.Gateway.Deduplication;
    using Persistence;
    using Raven.Abstractions.Exceptions;
    using Raven.Client;

    class RavenDeduplication : IDeduplicateMessages
    {
        IDocumentStore store;

        public RavenDeduplication(StoreAccessor storeAccessor)
        {
            store = storeAccessor.Store;
        }

        public bool DeduplicateMessage(string clientId, DateTime timeReceived)
        {
            using (var session = store.OpenSession())
            {
                session.Advanced.UseOptimisticConcurrency = true;
                session.Advanced.AllowNonAuthoritativeInformation = false;

                session.Store(new GatewayMessage
                    {
                        Id = EscapeClientId(clientId),
                        TimeReceived = timeReceived
                    });

                try
                {
                    session.SaveChanges();
                }
                catch (ConcurrencyException)
                {
                    return false;
                }

                return true;
            }
        }

        static string EscapeClientId(string clientId)
        {
            return clientId.Replace("\\", "_");
        }
    }
}