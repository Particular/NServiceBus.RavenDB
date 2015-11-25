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

        public async Task<bool> DeduplicateMessage(string messageId, DateTime timeReceived, ContextBag context)
        {
            using (var session = DocumentStore.OpenAsyncSession())
            {
                session.Advanced.UseOptimisticConcurrency = true;
                session.Advanced.AllowNonAuthoritativeInformation = false;

                await session.StoreAsync(new GatewayMessage
                {
                    Id = EscapeMessageId(messageId),
                    TimeReceived = timeReceived
                }).ConfigureAwait(false);

                try
                {
                  await  session.SaveChangesAsync().ConfigureAwait(false);
                }
                catch (ConcurrencyException)
                {
                    return false;
                }

                return true;
            }
        }

        static string EscapeMessageId(string messageId)
        {
            return messageId.Replace("\\", "_");
        }
    }
}