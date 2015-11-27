namespace NServiceBus.RavenDB.Outbox
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Outbox;
    using Raven.Client;

    class OutboxPersister : IOutboxStorage
    {
        public OutboxPersister(IDocumentStore documentStore)
        {
            this.documentStore = documentStore;
        }
        public async Task<OutboxMessage> Get(string messageId, ContextBag options)
        {
            OutboxRecord result;
            using (var session = documentStore.OpenAsyncSession())
            {
                // We use Load operation and not queries to avoid stale results
                result = await session.LoadAsync<OutboxRecord>(GetOutboxRecordId(messageId)).ConfigureAwait(false);
            }

            if (result == null)
            {
                return default(OutboxMessage);
            }

            var operations = result.TransportOperations.Select(t => new TransportOperation(t.MessageId, t.Options, t.Message, t.Headers)).ToList();
            var message = new OutboxMessage(result.MessageId, operations);

            return message;
        }


        public Task<OutboxTransaction> BeginTransaction(ContextBag context)
        {
            var session = documentStore.OpenAsyncSession();

            session.Advanced.UseOptimisticConcurrency = true;

            context.Set(session);
            var transaction = new RavenDBOutboxTransaction(session);
            return Task.FromResult<OutboxTransaction>(transaction);
        }

        public async Task Store(OutboxMessage message, OutboxTransaction transaction, ContextBag context)
        {
            var session = ((RavenDBOutboxTransaction)transaction).AsyncSession;

            await session.StoreAsync(new OutboxRecord
            {
                MessageId = message.MessageId,
                Dispatched = false,
                TransportOperations = message.TransportOperations.Select(t => new OutboxRecord.OutboxOperation
                {
                    Message = t.Body,
                    Headers = t.Headers,
                    MessageId = t.MessageId,
                    Options = t.Options
                }).ToList()
            }, GetOutboxRecordId(message.MessageId)).ConfigureAwait(false);
        }

        public async Task SetAsDispatched(string messageId, ContextBag options)
        {
            using (var session = documentStore.OpenAsyncSession())
            {
                session.Advanced.UseOptimisticConcurrency = true;
                var outboxMessage = await session.LoadAsync<OutboxRecord>(GetOutboxRecordId(messageId)).ConfigureAwait(false);
                if (outboxMessage == null || outboxMessage.Dispatched)
                {
                    return;
                }

                outboxMessage.Dispatched = true;
                outboxMessage.DispatchedAt = DateTime.UtcNow;

                await session.SaveChangesAsync().ConfigureAwait(false);
            }
        }

        static string GetOutboxRecordId(string messageId)
        {
            return "Outbox/" + messageId;
        }

        IDocumentStore documentStore;
    }
}