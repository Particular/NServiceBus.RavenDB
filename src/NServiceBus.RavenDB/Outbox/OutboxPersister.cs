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
        public Task<OutboxMessage> Get(string messageId, ContextBag options)
        {
            OutboxRecord result;
            using (var session = documentStore.OpenSession())
            {
                // We use Load operation and not queries to avoid stale results
                result = session.Load<OutboxRecord>(GetOutboxRecordId(messageId));
            }

            if (result == null)
            {
                return Task.FromResult(default(OutboxMessage));
            }

            var operations = result.TransportOperations.Select(t => new TransportOperation(t.MessageId, t.Options, t.Message, t.Headers)).ToList();
            var message = new OutboxMessage(result.MessageId, operations);

            return Task.FromResult(message);
        }


        public Task<OutboxTransaction> BeginTransaction(ContextBag context)
        {
            var session = documentStore.OpenSession();

            session.Advanced.UseOptimisticConcurrency = true;

            //todo: context.Set(session)
            var transaction = new RavenDBOutboxTransaction(session);
            return Task.FromResult<OutboxTransaction>(transaction);
        }

        public Task Store(OutboxMessage message, OutboxTransaction transaction, ContextBag context)
        {
            var session = ((RavenDBOutboxTransaction)transaction).Session;

            session.Store(new OutboxRecord
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
            }, GetOutboxRecordId(message.MessageId));

            return Task.FromResult(0);
        }

        public Task SetAsDispatched(string messageId, ContextBag options)
        {
            using (var session = documentStore.OpenSession())
            {
                session.Advanced.UseOptimisticConcurrency = true;
                var outboxMessage = session.Load<OutboxRecord>(GetOutboxRecordId(messageId));
                if (outboxMessage == null || outboxMessage.Dispatched)
                {
                    return Task.FromResult(0);
                }

                outboxMessage.Dispatched = true;
                outboxMessage.DispatchedAt = DateTime.UtcNow;

                session.SaveChanges();
            }
            return Task.FromResult(0);
        }

        static string GetOutboxRecordId(string messageId)
        {
            return "Outbox/" + messageId;
        }

        IDocumentStore documentStore;
    }
}