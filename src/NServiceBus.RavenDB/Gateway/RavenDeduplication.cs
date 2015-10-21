namespace NServiceBus.RavenDB.Gateway.Deduplication
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Gateway.Deduplication;
    using Raven.Abstractions.Exceptions;
    using Raven.Client;

    class RavenDeduplication : IDeduplicateMessages
    {
        public IDocumentStore DocumentStore { get; set; }

        public Task<bool> DeduplicateMessage(string messageId, DateTime timeReceived, ContextBag context)
        {
            using (var session = DocumentStore.OpenSession())
            {
                session.Advanced.UseOptimisticConcurrency = true;
                session.Advanced.AllowNonAuthoritativeInformation = false;

                session.Store(new GatewayMessage
                {
                    Id = EscapeMessageId(messageId),
                    TimeReceived = timeReceived
                });

                try
                {
                    session.SaveChanges();
                }
                catch (ConcurrencyException)
                {
                    return Task.FromResult(false);
                }

                return Task.FromResult(true);
            }
        }

        static string EscapeMessageId(string messageId)
        {
            return messageId.Replace("\\", "_");
        }
    }
}