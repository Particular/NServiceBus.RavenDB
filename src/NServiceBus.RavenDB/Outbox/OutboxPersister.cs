namespace NServiceBus.RavenDB.Outbox
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.Outbox;
    using Raven.Client;

    class OutboxPersister : IOutboxStorage
    {
        public IDocumentStore DocumentStore { get; set; }

        public async Task<OutboxMessage> Get(string messageId, OutboxStorageOptions options)
        {
            OutboxRecord result;
            using (var session = DocumentStore.OpenAsyncSession())
            {
                // We use Load operation and not queries to avoid stale results
                result = await session.LoadAsync<OutboxRecord>(GetOutboxRecordId(messageId));
            }

            if (result == null)
            {
                return null;
            }

            var message = new OutboxMessage(result.MessageId);
            message.TransportOperations.AddRange(
                result.TransportOperations.Select(t => new TransportOperation(t.MessageId, t.Options, t.Message, t.Headers)));

            return message;
        }

        public Task Store(OutboxMessage outboxMessage, OutboxStorageOptions options)
        {
            var session = options.GetSession();
            session.Advanced.UseOptimisticConcurrency = true;

            return session.StoreAsync(new OutboxRecord
            {
                MessageId = outboxMessage.MessageId,
                Dispatched = false,
                TransportOperations = outboxMessage.TransportOperations.Select(t => new OutboxRecord.OutboxOperation
                {
                    Message = t.Body,
                    Headers = t.Headers,
                    MessageId = t.MessageId,
                    Options = t.Options
                }).ToList()
            }, GetOutboxRecordId(outboxMessage.MessageId));
        }

        public async Task SetAsDispatched(string messageId, OutboxStorageOptions options)
        {
            using (var session = DocumentStore.OpenAsyncSession())
            {
                session.Advanced.UseOptimisticConcurrency = true;
                var outboxMessage = await session.LoadAsync<OutboxRecord>(GetOutboxRecordId(messageId));
                if (outboxMessage == null || outboxMessage.Dispatched)
                {
                    return;
                }

                outboxMessage.Dispatched = true;
                outboxMessage.DispatchedAt = DateTime.UtcNow;

                await session.SaveChangesAsync();
            }
        }

        static string GetOutboxRecordId(string messageId)
        {
            return "Outbox/" + messageId;
        }
    }
}