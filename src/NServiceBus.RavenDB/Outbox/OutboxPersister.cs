namespace NServiceBus.Persistence.RavenDB
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Outbox;
    using NServiceBus.RavenDB.Outbox;
    using Raven.Client;
    using TransportOperation = NServiceBus.Outbox.TransportOperation;

    class OutboxPersister : IOutboxStorage
    {
        public OutboxPersister(IDocumentStore documentStore, string endpointName)
        {
            this.documentStore = documentStore;
            this.endpointName = endpointName;
        }
        public async Task<OutboxMessage> Get(string messageId, ContextBag options)
        {
            OutboxRecord result;
            using (var session = documentStore.OpenAsyncSession())
            {
                session.Advanced.AllowNonAuthoritativeInformation = false;

                // We use Load operation and not queries to avoid stale results
                var possibleIds = GetPossibleOutboxDocumentIds(messageId);
                var docs = await session.LoadAsync<OutboxRecord>(possibleIds).ConfigureAwait(false);
                result = docs.FirstOrDefault(o => o != null);
            }

            if (result == null)
            {
                return default(OutboxMessage);
            }

            var operations = new TransportOperation[result.TransportOperations.Length];
            if (!result.Dispatched)
            {
                var index = 0;

                foreach (var operation in result.TransportOperations)
                {
                    operations[index] = new TransportOperation(operation.MessageId, operation.Options, operation.Message, operation.Headers);
                    index++;
                }
            }

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

        public Task Store(OutboxMessage message, OutboxTransaction transaction, ContextBag context)
        {
            var session = ((RavenDBOutboxTransaction)transaction).AsyncSession;

            var operations = new OutboxRecord.OutboxOperation[message.TransportOperations.Length];

            var index = 0;
            foreach (var transportOperation in message.TransportOperations)
            {
                operations[index] = new OutboxRecord.OutboxOperation
                {
                    Message = transportOperation.Body,
                    Headers = transportOperation.Headers,
                    MessageId = transportOperation.MessageId,
                    Options = transportOperation.Options
                };
                index++;
            }

            return session.StoreAsync(new OutboxRecord
            {
                MessageId = message.MessageId,
                Dispatched = false,
                TransportOperations = operations
            }, GetOutboxRecordId(message.MessageId));
        }

        public async Task SetAsDispatched(string messageId, ContextBag options)
        {
            using (var session = documentStore.OpenAsyncSession())
            {
                session.Advanced.UseOptimisticConcurrency = true;
                session.Advanced.AllowNonAuthoritativeInformation = false;

                var docs = await session.LoadAsync<OutboxRecord>(GetPossibleOutboxDocumentIds(messageId)).ConfigureAwait(false);
                var outboxMessage = docs.FirstOrDefault(o => o != null);
                if (outboxMessage == null || outboxMessage.Dispatched)
                {
                    return;
                }

                outboxMessage.Dispatched = true;
                outboxMessage.DispatchedAt = DateTime.UtcNow;

                await session.SaveChangesAsync().ConfigureAwait(false);
            }
        }

        string[] GetPossibleOutboxDocumentIds(string messageId)
        {
            return new[]
            {
                GetOutboxRecordId(messageId),
                $"Outbox/{messageId}"
            };
        }

        string GetOutboxRecordId(string messageId) => $"Outbox/{endpointName}/{messageId}";

        string endpointName;
        IDocumentStore documentStore;
    }
}