namespace NServiceBus.RavenDB.Gateway.Deduplication
{
    using System;
    using NServiceBus.Gateway.Deduplication;
    using Raven.Abstractions.Exceptions;
    using Raven.Client;

    class RavenDeduplication : IDeduplicateMessages
    {
        public IDocumentStore DocumentStore { get; set; }

        public bool DeduplicateMessage(string clientId, DateTime timeReceived)
        {
            using (var session = DocumentStore.OpenSession())
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